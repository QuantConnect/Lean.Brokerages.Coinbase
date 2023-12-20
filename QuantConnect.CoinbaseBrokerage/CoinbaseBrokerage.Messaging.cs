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
using QuantConnect.Util;
using QuantConnect.Orders;
using QuantConnect.Logging;
using Newtonsoft.Json.Linq;
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
        private RateGate _webSocketRateLimit = new(750, TimeSpan.FromSeconds(1));

        /// <summary>
        /// Collection of partial split messages
        /// </summary>
        public ConcurrentDictionary<long, GDAXFill> FillSplit { get; set; }

        private readonly ConcurrentDictionary<string, PendingOrder> _pendingOrders = new();

        private void SubscribeOnWebSocketFeed(object _, EventArgs __)
        {
            Log.Debug($"{nameof(CoinbaseBrokerage)}:Open on Heartbeats channel");
            ManageChannelSubscription(WebSocketSubscriptionType.Subscribe, CoinbaseWebSocketChannels.Heartbeats);
            Log.Debug($"{nameof(CoinbaseBrokerage)}:Connect: on User channel");
            ManageChannelSubscription(WebSocketSubscriptionType.Subscribe, CoinbaseWebSocketChannels.User);
        }

        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="_"></param>
        /// <param name="webSocketMessage"></param>
        protected override void OnMessage(object _, WebSocketMessage webSocketMessage)
        {
            var data = webSocketMessage.Data as WebSocketClientWrapper.TextMessage;

            Log.Debug($"{nameof(CoinbaseBrokerage)}:{nameof(OnMessage)}: {data.Message}");

            try
            {
                var obj = JObject.Parse(data.Message);

                var channel = obj[CoinbaseWebSocketChannels.Channel]?.Value<string>();

                if( channel == null ) 
                {
                    // We got this error from `user` channel
                    // obj["message"] {failure to subscribe}
                }

                switch (channel)
                {
                    case CoinbaseWebSocketChannels.MarketTrades:
                        var message = obj.ToObject<CoinbaseWebSocketMessage<CoinbaseMarketTradesEvent>>();
                        if (message.Events[0].Type == WebSocketEventType.update)
                        {
                            EmitTradeTick(message.Events[0]);
                        }
                        break;
                    case CoinbaseWebSocketChannels.User:
                        var message2 = obj.ToObject<CoinbaseWebSocketMessage<CoinbaseUserEvent>>();
                        break;
                    case CoinbaseWebSocketChannels.Level2Response:
                        var level2Data = obj.ToObject<CoinbaseWebSocketMessage<CoinbaseLevel2Event>>();
                        switch (level2Data.Events[0].Type)
                        {
                            case WebSocketEventType.snapshot:
                                Level2Snapshot(level2Data.Events[0]);
                                break;
                            case WebSocketEventType.update:
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
                if (update.Side == CoinbaseLevel2UpdateSide.bid)
                {
                    orderBook.UpdateBidRow(update.PriceLevel.Value, update.NewQuantity.Value);
                    continue;
                }

                if (update.Side == CoinbaseLevel2UpdateSide.offer)
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
                    case CoinbaseLevel2UpdateSide.bid:
                        if (update.NewQuantity.Value == 0)
                        {
                            orderBook.RemoveBidRow(update.PriceLevel.Value);
                        }
                        else
                        {
                            orderBook.UpdateBidRow(update.PriceLevel.Value, update.NewQuantity.Value);
                        }
                        continue;
                    case CoinbaseLevel2UpdateSide.offer:
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

                _aggregator.Update(new Tick
                {
                    Value = trade.Price.Value,
                    Time = trade.Time.UtcDateTime,
                    Symbol = symbol,
                    TickType = TickType.Trade,
                    Quantity = trade.Size.Value,
                    Exchange = MarketName
                });
            }
        }

        private void EmitFillOrderEvent(Fill fill, Order order)
        {
            var symbol = _symbolMapper.GetLeanSymbol(fill.ProductId, SecurityType.Crypto, MarketName);

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
                    Log.Error($"{nameof(CoinbaseBrokerage)}:{nameof(Subscribe)}: Unknown symbol: {item.Value}");
                }
            }

            var products = pendingSymbols.Select(symbol => _symbolMapper.GetBrokerageSymbol(symbol)).ToList();

            if (products.Count == 0)
            {
                return true;
            }

            foreach (var channel in CoinbaseWebSocketChannels.WebSocketChannelList)
            {
                ManageChannelSubscription(WebSocketSubscriptionType.Subscribe, channel, products);
            }

            return true;
        }

        /// <summary>
        /// Ends current subscriptions
        /// </summary>
        public bool Unsubscribe(IEnumerable<Symbol> leanSymbols)
        {
            var brokerageSymbols = leanSymbols.Select(symbol => _symbolMapper.GetBrokerageSymbol(symbol)).ToList();

            foreach (var channel in CoinbaseWebSocketChannels.WebSocketChannelList)
            {
                ManageChannelSubscription(WebSocketSubscriptionType.Unsubscribe, channel, brokerageSymbols);
            }

            return true;
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
                throw new ArgumentException($"{nameof(CoinbaseBrokerage)}:SubscribeToChannel: ChannelRequired:", nameof(channel));
            }

            if (!IsConnected)
            {
                throw new InvalidOperationException($"{nameof(CoinbaseBrokerage)}:SubscribeToChannel: WebSocketMustBeConnected");
            }

            productIds ??= new List<string> { "" };

            var (apiKey, timestamp, signature) = _coinbaseApi.GetWebSocketSignatures(channel, productIds);

            var json = JsonConvert.SerializeObject(
                new CoinbaseSubscriptionMessage(apiKey, channel, productIds, signature, timestamp, subscriptionType));

            Log.Debug("SubscribeToChannel:send json message: " + json);

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
