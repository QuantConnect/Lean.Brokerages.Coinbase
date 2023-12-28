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
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using QuantConnect.Securities;
using QuantConnect.Brokerages;
using QuantConnect.Orders.Fees;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.CoinbaseBrokerage.Models;
using QuantConnect.CoinbaseBrokerage.Models.Enums;
using QuantConnect.CoinbaseBrokerage.Models.Constants;
using QuantConnect.CoinbaseBrokerage.Models.WebSocket;

namespace QuantConnect.CoinbaseBrokerage
{
    public partial class CoinbaseBrokerage
    {
        /// <summary>
        /// Represents a collection of order books associated with symbols in a thread-safe manner.
        /// </summary>
        private readonly ConcurrentDictionary<Symbol, DefaultOrderBook> _orderBooks = new();

        /// <summary>
        /// Represents a rate limiter for controlling the frequency of WebSocket operations.
        /// </summary>
        /// <see cref="https://docs.cloud.coinbase.com/advanced-trade-api/docs/ws-rate-limits"/>
        private RateGate _webSocketRateLimit = new(7, TimeSpan.FromSeconds(1));

        /// <summary>
        /// Represents an integer variable used to keep track of sequence numbers associated with WS feed messages.
        /// </summary>
        private int _sequenceNumbers = 0;

        /// <summary>
        /// Use to sync subscription process on WebSocket User Updates
        /// </summary>
        private readonly ManualResetEvent _webSocketSubscriptionOnUserUpdateResetEvent = new(false);

        /// <summary>
        /// Cancellation token source associated with this instance.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// Use like synchronization context for threads
        /// </summary>
        private readonly object _synchronizationContext = new object();

        private void SubscribeOnWebSocketFeed(object _, EventArgs __)
        {
            // launch a task so we don't block WebSocket and can send and receive
            Task.Factory.StartNew(() =>
            {
                Log.Debug($"{nameof(CoinbaseBrokerage)}:Open on Heartbeats channel");
                ManageChannelSubscription(WebSocketSubscriptionType.Subscribe, CoinbaseWebSocketChannels.Heartbeats);

                // TODO: not working properly: https://forums.coinbasecloud.dev/t/type-error-message-failure-to-subscribe/5689
                _webSocketSubscriptionOnUserUpdateResetEvent.Reset();
                Log.Debug($"{nameof(CoinbaseBrokerage)}:Connect: on User channel");
                ManageChannelSubscription(WebSocketSubscriptionType.Subscribe, CoinbaseWebSocketChannels.User);

                if (!_webSocketSubscriptionOnUserUpdateResetEvent.WaitOne(TimeSpan.FromSeconds(30), _cancellationTokenSource.Token))
                {
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "SubscriptionOnWSFeed", "Failed to subscribe on `user update` channels"));
                }
            }, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="_"></param>
        /// <param name="webSocketMessage"></param>
        protected override void OnMessage(object _, WebSocketMessage webSocketMessage)
        {
            var data = webSocketMessage.Data as WebSocketClientWrapper.TextMessage;

            Log.Debug($"{nameof(CoinbaseBrokerage)}.{nameof(OnMessage)}: {data.Message}");

            try
            {
                var obj = JObject.Parse(data.Message);

                var channel = obj[CoinbaseWebSocketChannels.Channel]?.Value<string>();

                //this means an error has occurred
                if (channel == null)
                {
                    Log.Debug($"{nameof(CoinbaseBrokerage)}.{nameof(OnMessage)}.ERROR: {data.Message}");
                    _cancellationTokenSource.Cancel();
                    return;
                }

                var newSequenceNumbers = obj["sequence_num"].Value<int>();

                // https://docs.cloud.coinbase.com/advanced-trade-api/docs/ws-overview#sequence-numbers
                if (newSequenceNumbers != 0 && newSequenceNumbers != _sequenceNumbers + 1)
                {
                    return;
                }

                _sequenceNumbers = newSequenceNumbers;

                switch (channel)
                {
                    case CoinbaseWebSocketChannels.MarketTrades:
                        var message = obj.ToObject<CoinbaseWebSocketMessage<CoinbaseMarketTradesEvent>>();
                        if (message.Events[0].Type == WebSocketEventType.Update)
                        {
                            EmitTradeTick(message.Events[0]);
                        }
                        break;
                    case CoinbaseWebSocketChannels.User:
                        var orderUpdate = obj.ToObject<CoinbaseWebSocketMessage<CoinbaseUserEvent>>();
                        if (orderUpdate.Events[0].Type == WebSocketEventType.Snapshot)
                        {
                            // When we have subscribed to whatever channel we should send signal to event 
                            _webSocketSubscriptionOnUserUpdateResetEvent.Set();
                            break;
                        }
                        HandleOrderUpdate(orderUpdate.Events[0].Orders, orderUpdate.Timestamp.UtcDateTime);
                        break;
                    case CoinbaseWebSocketChannels.Level2Response:
                        var level2Data = obj.ToObject<CoinbaseWebSocketMessage<CoinbaseLevel2Event>>();
                        switch (level2Data.Events[0].Type)
                        {
                            case WebSocketEventType.Snapshot:
                                Level2Snapshot(level2Data.Events[0]);
                                break;
                            case WebSocketEventType.Update:
                                Level2Update(level2Data.Events[0]);
                                break;
                            default:
                                throw new ArgumentException();
                        };
                        break;
                }
            }
            catch (Exception ex)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {ex.Message} Exception: {ex}"));
            }
        }

        /// <summary>
        /// Handle order update based on WS message
        /// </summary>
        /// <param name="orders">brokerage order</param>
        /// <param name="eventTimestampUtc">timestamp(UTC) is occurred event </param>
        private void HandleOrderUpdate(List<CoinbaseWebSocketOrderResponse> orders, DateTime eventTimestampUtc)
        {
            foreach (var order in orders)
            {
                var leanOrder = OrderProvider.GetOrdersByBrokerageId(order.OrderId).FirstOrDefault();

                if (leanOrder == null)
                {
                    continue;
                }

                // Skip: pending on brokerage
                // Skip: cancel status cuz we return order message from CancelOrder()
                if (order.Status == Models.Enums.OrderStatus.Pending || order.Status == Models.Enums.OrderStatus.Cancelled)
                {
                    continue;
                }

                // Skip: Order was submitted on brokerage - successfully
                if (order.Status == Models.Enums.OrderStatus.Open && order.CumulativeQuantity == 0)
                {
                    continue;
                }

                // Skip: Order was filled but brokerage has not return status.Filled yet
                if (order.LeavesQuantity == 0 && order.Status == Models.Enums.OrderStatus.Open)
                {
                    continue;
                }

                // order.CumulativeQuantity > 0 && order.LeavesQuantity != 0 && order.Status == Models.Enums.OrderStatus.Open
                var leanOrderStatus = Orders.OrderStatus.PartiallyFilled;

                if (order.LeavesQuantity == 0 && order.Status == Models.Enums.OrderStatus.Filled)
                {
                    leanOrderStatus = Orders.OrderStatus.Filled;
                }

                CurrencyPairUtil.DecomposeCurrencyPair(leanOrder.Symbol, out _, out var quoteCurrency);

                var orderEvent = new OrderEvent(
                    leanOrder.Id,
                    leanOrder.Symbol,
                    eventTimestampUtc,
                    leanOrderStatus,
                    leanOrder.Direction,
                    order.AveragePrice.Value,
                    order.CumulativeQuantity.Value * Math.Sign(leanOrder.Quantity),
                     new OrderFee(new CashAmount(order.TotalFees.Value, quoteCurrency))
                    );

                OnOrderEvent(orderEvent);
            }
        }

        private void Level2Snapshot(CoinbaseLevel2Event snapshotData)
        {
            var symbol = _symbolMapper.GetLeanSymbol(snapshotData.ProductId, SecurityType.Crypto, MarketName);

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

            foreach (var update in snapshotData.Updates)
            {
                if (update.Side == CoinbaseLevel2UpdateSide.Bid)
                {
                    orderBook.UpdateBidRow(update.PriceLevel.Value, update.NewQuantity.Value);
                    continue;
                }

                if (update.Side == CoinbaseLevel2UpdateSide.Offer)
                {
                    orderBook.UpdateAskRow(update.PriceLevel.Value, update.NewQuantity.Value);
                }
            }

            orderBook.BestBidAskUpdated += OnBestBidAskUpdated;

            EmitQuoteTick(symbol, orderBook.BestBidPrice, orderBook.BestBidSize, orderBook.BestAskPrice, orderBook.BestAskSize);
        }

        private void OnBestBidAskUpdated(object sender, BestBidAskUpdatedEventArgs e)
        {
            EmitQuoteTick(e.Symbol, e.BestBidPrice, e.BestBidSize, e.BestAskPrice, e.BestAskSize);
        }

        private void Level2Update(CoinbaseLevel2Event updateData)
        {
            var leanSymbol = _symbolMapper.GetLeanSymbol(updateData.ProductId, SecurityType.Crypto, MarketName);

            if (!_orderBooks.TryGetValue(leanSymbol, out var orderBook))
            {
                Log.Error($"Attempting to update a non existent order book for {leanSymbol}");
                return;
            }

            foreach (var update in updateData.Updates)
            {
                switch (update.Side)
                {
                    case CoinbaseLevel2UpdateSide.Bid:
                        if (update.NewQuantity.Value == 0)
                        {
                            orderBook.RemoveBidRow(update.PriceLevel.Value);
                        }
                        else
                        {
                            orderBook.UpdateBidRow(update.PriceLevel.Value, update.NewQuantity.Value);
                        }
                        continue;
                    case CoinbaseLevel2UpdateSide.Offer:
                        if (update.NewQuantity.Value == 0)
                        {
                            orderBook.RemoveAskRow(update.PriceLevel.Value);
                        }
                        else
                        {
                            orderBook.UpdateAskRow(update.PriceLevel.Value, update.NewQuantity.Value);
                        }
                        continue;
                }
            }
        }

        private void EmitTradeTick(CoinbaseMarketTradesEvent tradeUpdates)
        {
            foreach (var trade in tradeUpdates.Trades)
            {
                var symbol = _symbolMapper.GetLeanSymbol(trade.ProductId, SecurityType.Crypto, MarketName);

                var tick = new Tick
                {
                    Value = trade.Price.Value,
                    Time = trade.Time.UtcDateTime,
                    Symbol = symbol,
                    TickType = TickType.Trade,
                    Quantity = trade.Size.Value,
                    Exchange = MarketName
                };

                lock (_synchronizationContext)
                {
                    _aggregator.Update(tick);
                }
            }
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
            var tick = new Tick
            {
                AskPrice = askPrice,
                BidPrice = bidPrice,
                Time = DateTime.UtcNow,
                Symbol = symbol,
                TickType = TickType.Quote,
                AskSize = askSize,
                BidSize = bidSize
            };
            tick.SetValue();

            lock (_synchronizationContext)
            {
                _aggregator.Update(tick);
            }
        }

        /// <summary>
        /// Creates WebSocket message subscriptions for the supplied symbols
        /// </summary>
        protected override bool Subscribe(IEnumerable<Symbol> symbols)
        {
            SubscribeSymbolsOnDataChannels(symbols.ToList());

            return true;
        }

        /// <summary>
        /// Ends current subscriptions
        /// </summary>
        public bool Unsubscribe(IEnumerable<Symbol> leanSymbols)
        {
            SubscribeSymbolsOnDataChannels(leanSymbols.ToList(), WebSocketSubscriptionType.Unsubscribe);

            return true;
        }

        /// <summary>
        /// Subscribes to real-time data channels for the provided list of symbols.
        /// </summary>
        /// <param name="symbols">The list of symbols to subscribe to.</param>
        /// <remarks>
        /// This method subscribes to WebSocket channels for each provided symbol, converting them to brokerage symbols using
        /// the symbol mapper. It then iterates through the available WebSocket channels and manages the subscription by
        /// invoking the <see cref="ManageChannelSubscription"/> method with the appropriate parameters.
        /// </remarks>
        /// <seealso cref="ManageChannelSubscription"/>
        private void SubscribeSymbolsOnDataChannels(List<Symbol> symbols, WebSocketSubscriptionType subscriptionType = WebSocketSubscriptionType.Subscribe)
        {
            var products = symbols.Select(symbol => _symbolMapper.GetBrokerageSymbol(symbol)).ToList();

            if (products.Count == 0)
            {
                return;
            }

            foreach (var channel in CoinbaseWebSocketChannels.WebSocketChannelList)
            {
                ManageChannelSubscription(subscriptionType, channel, products);
            }
        }

        /// <summary>
        /// Manages WebSocket subscriptions by subscribing or unsubscribing to a specified channel.
        /// </summary>
        /// <param name="subscriptionType">The type of WebSocket subscription (subscribe or unsubscribe).</param>
        /// <param name="channel">The channel to subscribe or unsubscribe from. <see cref="CoinbaseWebSocketChannels"/></param>
        /// <param name="productIds">Optional list of product IDs associated with the subscription.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private void ManageChannelSubscription(WebSocketSubscriptionType subscriptionType, string channel, List<string> productIds = null)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException($"{nameof(CoinbaseBrokerage)}.{nameof(ManageChannelSubscription)}: ChannelRequired:", nameof(channel));
            }

            if (!IsConnected)
            {
                throw new InvalidOperationException($"{nameof(CoinbaseBrokerage)}.{nameof(ManageChannelSubscription)}: WebSocketMustBeConnected");
            }

            var (apiKey, timestamp, signature) = _coinbaseApi.GetWebSocketSignatures(channel, productIds);

            var json = JsonConvert.SerializeObject(
                new CoinbaseSubscriptionMessage(apiKey, channel, productIds, signature, timestamp, subscriptionType));

            Log.Debug($"{nameof(CoinbaseBrokerage)}.{nameof(ManageChannelSubscription)}:send json message: " + json);

            _webSocketRateLimit.WaitToProceed();

            WebSocket.Send(json);
        }
    }
}
