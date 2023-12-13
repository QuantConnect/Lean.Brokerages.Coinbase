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

using System;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using QuantConnect.Util;
using QuantConnect.Orders;
using QuantConnect.Logging;
using System.Globalization;
using System.Threading.Tasks;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Orders.Fees;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.CoinbaseBrokerage.Models;
using QuantConnect.CoinbaseBrokerage.Models.Enums;
using QuantConnect.CoinbaseBrokerage.Models.Constants;

namespace QuantConnect.Brokerages.GDAX
{
    public partial class GDAXBrokerage
    {
        /// <summary>
        /// Represents a collection of order books associated with symbols in a thread-safe manner.
        /// </summary>
        private readonly ConcurrentDictionary<Symbol, DefaultOrderBook> _orderBooks = new();

        /// <summary>
        /// Represents a rate limiter for controlling the frequency of WebSocket operations.
        /// </summary>
        /// <see cref="https://docs.cloud.coinbase.com/advanced-trade-api/docs/ws-rate-limits"/>
        private RateGate _webSocketRateLimit = new(750, TimeSpan.FromSeconds(1));

        /// <summary>
        /// Collection of partial split messages
        /// </summary>
        public ConcurrentDictionary<long, GDAXFill> FillSplit { get; set; }

        private readonly CancellationTokenSource _canceller = new CancellationTokenSource();

        private IPriceProvider _priceProvider;

        private readonly CancellationTokenSource _ctsFillMonitor = new CancellationTokenSource();

        private readonly ConcurrentDictionary<string, PendingOrder> _pendingOrders = new();

        private void SubscribeOnWebSocketFeed(object _, EventArgs __)
        {
            Log.Debug($"{nameof(GDAXBrokerage)}:Open on Heartbeats channel");
            SubscribeToChannel(WebSocketSubscriptionType.Subscribe, CoinbaseWebSocketChannels.Heartbeats);
            Log.Debug($"{nameof(GDAXBrokerage)}:Connect: on User channel");
            SubscribeToChannel(WebSocketSubscriptionType.Subscribe, CoinbaseWebSocketChannels.User);
        }

        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="_"></param>
        /// <param name="webSocketMessage"></param>
        protected override void OnMessage(object _, WebSocketMessage webSocketMessage)
        {
            var data = webSocketMessage.Data as WebSocketClientWrapper.TextMessage;

            Log.Debug($"GDAXBrokerage.OnMessage: {data.Message}");

            try
            {
                var raw = JsonConvert.DeserializeObject<Messages.BaseMessage>(data.Message, JsonSettings);

                if (raw.Type == "heartbeat")
                {
                    return;
                }
                else if (raw.Type == "snapshot")
                {
                    OnSnapshot(data.Message);
                    return;
                }
                else if (raw.Type == "l2update")
                {
                    OnL2Update(data.Message);
                    return;
                }
                else if (raw.Type == "error")
                {
                    Log.Error($"GDAXBrokerage.OnMessage.error(): Data: {Environment.NewLine}{data.Message}");

                    var error = JsonConvert.DeserializeObject<Messages.Error>(data.Message, JsonSettings);
                    var messageType = error.Message.Equals("Failed to subscribe", StringComparison.InvariantCultureIgnoreCase) ||
                                      error.Message.Equals("Authentication Failed", StringComparison.InvariantCultureIgnoreCase)
                        ? BrokerageMessageType.Error
                        : BrokerageMessageType.Warning;
                    var message = $"Message:{error.Message} - Reason:{error.Reason}";

                    OnMessage(new BrokerageMessageEvent(messageType, -1, $"GDAXBrokerage.OnMessage: {message}"));
                }
                else if (raw.Type == "match")
                {
                    OnMatch(data.Message);
                    return;
                }
                else if (raw.Type == "open" || raw.Type == "change" || raw.Type == "done" || raw.Type == "received" || raw.Type == "subscriptions" || raw.Type == "last_match")
                {
                    //known messages we don't need to handle or log
                    return;
                }

                Log.Trace($"GDAXWebsocketsBrokerage.OnMessage: Unexpected message format: {data.Message}");
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {data.Message} Exception: {exception}"));
                throw;
            }
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

                EmitQuoteTick(symbol, orderBook.BestBidPrice, orderBook.BestBidSize, orderBook.BestAskPrice, orderBook.BestAskSize);
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnBestBidAskUpdated(object sender, BestBidAskUpdatedEventArgs e)
        {
            EmitQuoteTick(e.Symbol, e.BestBidPrice, e.BestBidSize, e.BestAskPrice, e.BestAskSize);
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
            EmitTradeTick(message);
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

            var status = isFinalFill ? Orders.OrderStatus.Filled : Orders.OrderStatus.PartiallyFilled;

            var direction = fill.Side == "sell" ? OrderDirection.Sell : OrderDirection.Buy;

            var fillPrice = fill.Price;
            var fillQuantity = direction == OrderDirection.Sell ? -fill.Size : fill.Size;

            string currency;
            if (order.PriceCurrency.IsNullOrEmpty())
            {
                CurrencyPairUtil.DecomposeCurrencyPair(symbol, out string baseCurrency, out string quoteCurrency);
                currency = quoteCurrency;
            }
            else
            {
                currency = order.PriceCurrency;
            }

            var orderFee = new OrderFee(new CashAmount(fill.Fee, currency));

            var orderEvent = new OrderEvent
            (
                order.Id, symbol, fill.CreatedAt, status,
                direction, fillPrice, fillQuantity,
                orderFee, $"GDAX Fill Event {direction}"
            );

            // when the order is completely filled, we no longer need it in the active order list
            if (orderEvent.Status == Orders.OrderStatus.Filled)
            {
                Order outOrder;
                CachedOrderIDs.TryRemove(order.Id, out outOrder);

                PendingOrder removed;
                _pendingOrders.TryRemove(fill.OrderId, out removed);
            }

            OnOrderEvent(orderEvent);
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
        /// Creates WebSocket message subscriptions for the supplied symbols
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

            var products = pendingSymbols.Select(symbol => _symbolMapper.GetBrokerageSymbol(symbol)).ToList();

            if (products.Count == 0)
            {
                return true;
            }

            foreach (var channel in CoinbaseWebSocketChannels.WebSocketChannelList)
            {
                SubscribeToChannel(WebSocketSubscriptionType.Subscribe, channel, products);
            }

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
                    //channels = ChannelNames,
                    product_ids = products
                };

                _webSocketRateLimit.WaitToProceed();

                WebSocket.Send(JsonConvert.SerializeObject(payload));
            }
            return true;
        }

        private void SubscribeToChannel(WebSocketSubscriptionType subscriptionType, string channel, List<string> productIds = null)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException($"{nameof(GDAXBrokerage)}:SubscribeToChannel: ChannelRequired:", nameof(channel));
            }

            if (!IsConnected)
            {
                throw new InvalidOperationException($"{nameof(GDAXBrokerage)}:SubscribeToChannel: WebSocketMustBeConnected");
            }

            productIds ??= new List<string> { "" };

            var (apiKey, timestamp, signature) = _coinbaseApi.GetWebSocketSignatures(channel, productIds);

            var json = JsonConvert.SerializeObject(
                new CoinbaseSubscriptionMessage(apiKey, channel, productIds, signature, timestamp, subscriptionType));

            Log.Debug("SubscribeToChannel:json: " + json);

            WebSocket.Send(json);
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
