/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.GDAX
{
    public partial class GDAXBrokerage
    {
        #region Declarations

        /// <summary>
        /// Collection of partial split messages
        /// </summary>
        public ConcurrentDictionary<long, GDAXFill> FillSplit { get; set; }

        private string _passPhrase;
        private IAlgorithm _algorithm;
        private readonly CancellationTokenSource _canceller = new CancellationTokenSource();
        private readonly ConcurrentDictionary<Symbol, DefaultOrderBook> _orderBooks = new ConcurrentDictionary<Symbol, DefaultOrderBook>();
        private readonly SymbolPropertiesDatabaseSymbolMapper _symbolMapper = new SymbolPropertiesDatabaseSymbolMapper(Market.GDAX);
        private bool _isDataQueueHandler;
        private LiveNodePacket _job;

        /// <summary>
        /// Data Aggregator
        /// </summary>
        protected IDataAggregator _aggregator;

        // GDAX has different rate limits for public and private endpoints
        // https://docs.gdax.com/#rate-limits
        internal enum GdaxEndpointType { Public, Private }

        private readonly RateGate _publicEndpointRateLimiter = new RateGate(6, TimeSpan.FromSeconds(1));
        private readonly RateGate _privateEndpointRateLimiter = new RateGate(10, TimeSpan.FromSeconds(1));

        private IPriceProvider _priceProvider;

        private readonly CancellationTokenSource _ctsFillMonitor = new CancellationTokenSource();
        private Task _fillMonitorTask;
        private readonly AutoResetEvent _fillMonitorResetEvent = new AutoResetEvent(false);
        private readonly int _fillMonitorTimeout = Config.GetInt("gdax-fill-monitor-timeout", 500);
        private readonly ConcurrentDictionary<string, PendingOrder> _pendingOrders = new ConcurrentDictionary<string, PendingOrder>();

        #endregion Declarations

        /// <summary>
        /// The list of websocket channels to subscribe
        /// </summary>
        protected virtual string[] ChannelNames { get; } = { "heartbeat" };

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="name">Name of brokerage</param>
        public GDAXBrokerage(string name) : base(name)
        {
        }

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        /// <param name="wssUrl">websockets url</param>
        /// <param name="websocket">instance of websockets client</param>
        /// <param name="restClient">instance of rest client</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="passPhrase">pass phrase</param>
        /// <param name="algorithm">the algorithm instance is required to retreive account type</param>
        /// <param name="priceProvider">The price provider for missing FX conversion rates</param>
        /// <param name="aggregator">consolidate ticks</param>
        /// <param name="job">The live job packet</param>
        public GDAXBrokerage(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, string passPhrase, IAlgorithm algorithm,
            IPriceProvider priceProvider, IDataAggregator aggregator, LiveNodePacket job)
            : base("GDAX")
        {
            Initialize(
                wssUrl: wssUrl,
                websocket: websocket,
                restClient: restClient,
                apiKey: apiKey,
                apiSecret: apiSecret,
                passPhrase: passPhrase,
                algorithm: algorithm,
                priceProvider: priceProvider,
                aggregator: aggregator,
                job: job
            );
        }

        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnMessage(object sender, WebSocketMessage webSocketMessage)
        {
            var e = (WebSocketClientWrapper.TextMessage)webSocketMessage.Data;

            try
            {
                var raw = JsonConvert.DeserializeObject<Messages.BaseMessage>(e.Message, JsonSettings);

                if (raw.Type == "heartbeat")
                {
                    return;
                }
                else if (raw.Type == "snapshot")
                {
                    OnSnapshot(e.Message);
                    return;
                }
                else if (raw.Type == "l2update")
                {
                    OnL2Update(e.Message);
                    return;
                }
                else if (raw.Type == "error")
                {
                    Log.Error($"GDAXBrokerage.OnMessage.error(): Data: {Environment.NewLine}{e.Message}");

                    var error = JsonConvert.DeserializeObject<Messages.Error>(e.Message, JsonSettings);
                    var messageType = error.Message.Equals("Failed to subscribe", StringComparison.InvariantCultureIgnoreCase) ||
                                      error.Message.Equals("Authentication Failed", StringComparison.InvariantCultureIgnoreCase)
                        ? BrokerageMessageType.Error
                        : BrokerageMessageType.Warning;
                    var message = $"Message:{error.Message} - Reason:{error.Reason}";

                    OnMessage(new BrokerageMessageEvent(messageType, -1, $"GDAXBrokerage.OnMessage: {message}"));
                }
                else if (raw.Type == "match")
                {
                    OnMatch(e.Message);
                    return;
                }
                else if (raw.Type == "open" || raw.Type == "change" || raw.Type == "done" || raw.Type == "received" || raw.Type == "subscriptions" || raw.Type == "last_match")
                {
                    //known messages we don't need to handle or log
                    return;
                }

                Log.Trace($"GDAXWebsocketsBrokerage.OnMessage: Unexpected message format: {e.Message}");
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }

        /// <summary>
        /// Initialize the instance of this class
        /// </summary>
        /// <param name="wssUrl">The web socket base url</param>
        /// <param name="websocket">instance of websockets client</param>
        /// <param name="restClient">instance of rest client</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="passPhrase">pass phrase</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="priceProvider">The price provider for missing FX conversion rates</param>
        /// <param name="aggregator">the aggregator for consolidating ticks</param>
        /// <param name="job">The live job packet</param>
        protected void Initialize(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret,
            string passPhrase, IAlgorithm algorithm, IPriceProvider priceProvider, IDataAggregator aggregator, LiveNodePacket job)
        {
            if (IsInitialized)
            {
                return;
            }
            base.Initialize(wssUrl, websocket, restClient, apiKey, apiSecret);
            _job = job;
            FillSplit = new ConcurrentDictionary<long, GDAXFill>();
            _passPhrase = passPhrase;
            _algorithm = algorithm;
            _priceProvider = priceProvider;
            _aggregator = aggregator;

            _isDataQueueHandler = this is GDAXDataQueueHandler;

            _fillMonitorTask = Task.Factory.StartNew(FillMonitorAction, _ctsFillMonitor.Token);

            var subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            subscriptionManager.SubscribeImpl += (s, t) =>
            {
                Subscribe(s);
                return true;
            };
            subscriptionManager.UnsubscribeImpl += (s, t) => Unsubscribe(s);

            SubscriptionManager = subscriptionManager;
        }

        private void OnSnapshot(string data)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<Messages.Snapshot>(data);

                var symbol = _symbolMapper.GetLeanSymbol(message.ProductId, SecurityType.Crypto, Market.GDAX);

                DefaultOrderBook orderBook;
                if (!_orderBooks.TryGetValue(symbol, out orderBook))
                {
                    orderBook = new DefaultOrderBook(symbol);
                    _orderBooks[symbol] = orderBook;
                }
                else
                {
                    orderBook.BestBidAskUpdated -= OnBestBidAskUpdated;
                    orderBook.Clear();
                }

                foreach (var row in message.Bids)
                {
                    var price = decimal.Parse(row[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                    var size = decimal.Parse(row[1], NumberStyles.Float, CultureInfo.InvariantCulture);
                    orderBook.UpdateBidRow(price, size);
                }
                foreach (var row in message.Asks)
                {
                    var price = decimal.Parse(row[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                    var size = decimal.Parse(row[1], NumberStyles.Float, CultureInfo.InvariantCulture);
                    orderBook.UpdateAskRow(price, size);
                }

                orderBook.BestBidAskUpdated += OnBestBidAskUpdated;

                if (_isDataQueueHandler)
                {
                    EmitQuoteTick(symbol, orderBook.BestBidPrice, orderBook.BestBidSize, orderBook.BestAskPrice, orderBook.BestAskSize);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnBestBidAskUpdated(object sender, BestBidAskUpdatedEventArgs e)
        {
            if (_isDataQueueHandler)
            {
                EmitQuoteTick(e.Symbol, e.BestBidPrice, e.BestBidSize, e.BestAskPrice, e.BestAskSize);
            }
        }

        private void OnL2Update(string data)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<Messages.L2Update>(data);

                var symbol = _symbolMapper.GetLeanSymbol(message.ProductId, SecurityType.Crypto, Market.GDAX);

                var orderBook = _orderBooks[symbol];

                foreach (var row in message.Changes)
                {
                    var side = row[0];
                    var price = Convert.ToDecimal(row[1], CultureInfo.InvariantCulture);
                    var size = decimal.Parse(row[2], NumberStyles.Float, CultureInfo.InvariantCulture);
                    if (side == "buy")
                    {
                        if (size == 0)
                        {
                            orderBook.RemoveBidRow(price);
                        }
                        else
                        {
                            orderBook.UpdateBidRow(price, size);
                        }
                    }
                    else if (side == "sell")
                    {
                        if (size == 0)
                        {
                            orderBook.RemoveAskRow(price);
                        }
                        else
                        {
                            orderBook.UpdateAskRow(price, size);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Data: " + data);
                throw;
            }
        }

        private void OnMatch(string data)
        {
            // deserialize the current match (trade) message
            var message = JsonConvert.DeserializeObject<Messages.Matched>(data, JsonSettings);

            // message received from the "matches" channel
            if (_isDataQueueHandler)
            {
                EmitTradeTick(message);
            }
        }

        private void EmitFillOrderEvent(Messages.Fill fill, Order order)
        {
            var symbol = _symbolMapper.GetLeanSymbol(fill.ProductId, SecurityType.Crypto, Market.GDAX);

            if (!FillSplit.ContainsKey(order.Id))
            {
                FillSplit[order.Id] = new GDAXFill(order);
            }

            var split = FillSplit[order.Id];
            split.Add(fill);

            // is this the total order at once? Is this the last split fill?
            var isFinalFill = Math.Abs(fill.Size) == Math.Abs(order.Quantity) || Math.Abs(split.OrderQuantity) == Math.Abs(split.TotalQuantity);

            var status = isFinalFill ? OrderStatus.Filled : OrderStatus.PartiallyFilled;

            var direction = fill.Side == "sell" ? OrderDirection.Sell : OrderDirection.Buy;

            var fillPrice = fill.Price;
            var fillQuantity = direction == OrderDirection.Sell ? -fill.Size : fill.Size;

            var currency = order.PriceCurrency == string.Empty
                ? _algorithm.Securities[symbol].SymbolProperties.QuoteCurrency
                : order.PriceCurrency;

            var orderFee = new OrderFee(new CashAmount(fill.Fee, currency));

            var orderEvent = new OrderEvent
            (
                order.Id, symbol, fill.CreatedAt, status,
                direction, fillPrice, fillQuantity,
                orderFee, $"GDAX Fill Event {direction}"
            );

            // when the order is completely filled, we no longer need it in the active order list
            if (orderEvent.Status == OrderStatus.Filled)
            {
                Order outOrder;
                CachedOrderIDs.TryRemove(order.Id, out outOrder);

                PendingOrder removed;
                _pendingOrders.TryRemove(fill.OrderId, out removed);
            }

            OnOrderEvent(orderEvent);
        }

        /// <summary>
        /// Retrieves a price tick for a given symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public Tick GetTick(Symbol symbol)
        {
            var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(symbol);

            var req = new RestRequest($"/products/{brokerageSymbol}/ticker", Method.GET);
            var response = ExecuteRestRequest(req, GdaxEndpointType.Public);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"GDAXBrokerage.GetTick: request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
            }

            var tick = JsonConvert.DeserializeObject<Messages.Tick>(response.Content);
            return new Tick(tick.Time, symbol, tick.Bid, tick.Ask) { Quantity = tick.Volume };
        }

        /// <summary>
        /// Emits a new quote tick
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="bidPrice">The bid price</param>
        /// <param name="bidSize">The bid size</param>
        /// <param name="askPrice">The ask price</param>
        /// <param name="askSize">The ask price</param>
        private void EmitQuoteTick(Symbol symbol, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            _aggregator.Update(new Tick
            {
                AskPrice = askPrice,
                BidPrice = bidPrice,
                Value = (askPrice + bidPrice) / 2m,
                Time = DateTime.UtcNow,
                Symbol = symbol,
                TickType = TickType.Quote,
                AskSize = askSize,
                BidSize = bidSize
            });
        }

        /// <summary>
        /// Emits a new trade tick from a match message
        /// </summary>
        private void EmitTradeTick(Messages.Matched message)
        {
            var symbol = _symbolMapper.GetLeanSymbol(message.ProductId, SecurityType.Crypto, Market.GDAX);

            _aggregator.Update(new Tick
            {
                Value = message.Price,
                Time = DateTime.UtcNow,
                Symbol = symbol,
                TickType = TickType.Trade,
                Quantity = message.Size
            });
        }

        /// <summary>
        /// Creates websocket message subscriptions for the supplied symbols
        /// </summary>
        protected override bool Subscribe(IEnumerable<Symbol> symbols)
        {
            var fullList = GetSubscribed().Union(symbols);
            var pendingSymbols = new List<Symbol>();
            foreach (var item in fullList)
            {
                if (_symbolMapper.IsKnownLeanSymbol(item))
                {
                    pendingSymbols.Add(item);
                }
                else if (item.SecurityType == SecurityType.Crypto)
                {
                    Log.Error($"Unknown GDAX symbol: {item.Value}");
                }
                else
                {
                    //todo: refactor this outside brokerage
                    //alternative service: http://openexchangerates.org/latest.json
                    PollTick(item);
                }
            }

            var products = pendingSymbols
                .Select(s => _symbolMapper.GetBrokerageSymbol(s))
                .ToArray();

            var payload = new
            {
                type = "subscribe",
                product_ids = products,
                channels = ChannelNames
            };

            if (payload.product_ids.Length == 0)
            {
                return true;
            }

            var token = GetAuthenticationToken(string.Empty, "GET", "/users/self/verify");

            var json = JsonConvert.SerializeObject(new
            {
                type = payload.type,
                channels = payload.channels,
                product_ids = payload.product_ids,
                timestamp = token.Timestamp,
                key = ApiKey,
                passphrase = _passPhrase,
                signature = token.Signature,
            });

            WebSocket.Send(json);

            Log.Trace("GDAXBrokerage.Subscribe: Sent subscribe.");
            return true;
        }

        /// <summary>
        /// Poll for new tick to refresh conversion rate of non-USD denomination
        /// </summary>
        /// <param name="symbol"></param>
        public void PollTick(Symbol symbol)
        {
            int delay = 36000;
            var token = _canceller.Token;
            var listener = Task.Factory.StartNew(() =>
            {
                Log.Trace($"GDAXBrokerage.PollLatestTick: started polling for ticks: {symbol.Value}");

                while (true)
                {
                    var rate = GetConversionRate(symbol);

                    var latest = new Tick
                    {
                        Value = rate,
                        Time = DateTime.UtcNow,
                        Symbol = symbol,
                        TickType = TickType.Quote
                    };
                    _aggregator.Update(latest);

                    int count = 0;
                    while (++count < delay)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(1000);
                    }

                    if (token.IsCancellationRequested) break;
                }

                Log.Trace($"PollLatestTick: stopped polling for ticks: {symbol.Value}");
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private decimal GetConversionRate(Symbol symbol)
        {
            try
            {
                return _priceProvider.GetLastPrice(symbol);
            }
            catch (Exception e)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, 0, $"GetConversionRate: {e.Message}"));
                return 0;
            }
        }

        /// <summary>
        /// Ends current subscriptions
        /// </summary>
        public bool Unsubscribe(IEnumerable<Symbol> symbols)
        {
            if (WebSocket.IsOpen)
            {
                var products = symbols
                    .Select(s => _symbolMapper.GetBrokerageSymbol(s))
                    .ToArray();

                var payload = new
                {
                    type = "unsubscribe",
                    channels = ChannelNames,
                    product_ids = products
                };

                WebSocket.Send(JsonConvert.SerializeObject(payload));
            }
            return true;
        }

        private void FillMonitorAction()
        {
            Log.Trace("GDAXBrokerage.FillMonitorAction(): task started");

            try
            {
                foreach (var order in GetOpenOrders())
                {
                    _pendingOrders.TryAdd(order.BrokerId.First(), new PendingOrder(order));
                }

                while (!_ctsFillMonitor.IsCancellationRequested)
                {
                    _fillMonitorResetEvent.WaitOne(TimeSpan.FromMilliseconds(_fillMonitorTimeout), _ctsFillMonitor.Token);

                    foreach (var kvp in _pendingOrders)
                    {
                        var orderId = kvp.Key;
                        var pendingOrder = kvp.Value;

                        var request = new RestRequest($"/fills?order_id={orderId}", Method.GET);
                        GetAuthenticationToken(request);

                        var response = ExecuteRestRequest(request, GdaxEndpointType.Private, false);

                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            OnMessage(new BrokerageMessageEvent(
                                BrokerageMessageType.Warning,
                                -1,
                                $"GDAXBrokerage.FillMonitorAction(): request failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}"));

                            continue;
                        }

                        var fills = JsonConvert.DeserializeObject<List<Messages.Fill>>(response.Content);
                        foreach (var fill in fills.OrderBy(x => x.TradeId))
                        {
                            if (fill.TradeId <= pendingOrder.LastEmittedFillTradeId)
                            {
                                continue;
                            }

                            EmitFillOrderEvent(fill, pendingOrder.Order);

                            pendingOrder.LastEmittedFillTradeId = fill.TradeId;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, exception.Message));
            }

            Log.Trace("GDAXBrokerage.FillMonitorAction(): task ended");
        }

        private class PendingOrder
        {
            public Order Order { get; }
            public long LastEmittedFillTradeId { get; set; }

            public PendingOrder(Order order)
            {
                Order = order;
            }
        }
    }
}
