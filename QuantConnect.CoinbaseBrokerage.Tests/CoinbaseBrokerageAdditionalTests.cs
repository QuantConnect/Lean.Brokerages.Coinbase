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
using NUnit.Framework;
using System.Threading;
using QuantConnect.Orders;
using QuantConnect.Logging;
using QuantConnect.Algorithm;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using System.Collections.Generic;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests.Common.Securities;
using QuantConnect.Brokerages.Coinbase.Models;
using QuantConnect.Brokerages.Coinbase.Models.Enums;

namespace QuantConnect.Brokerages.Coinbase.Tests
{
    [TestFixture]
    public class CoinbaseBrokerageAdditionalTests
    {
        [Explicit("`user` channel sometimes doesn't subscribed in WebSocket.Open event")]
        [TestCase(5)]
        public void BrokerageConnectionAndReconnectionTest(int amountAttempt)
        {
            int counter = 0;
            var cancellationTokenSource = new CancellationTokenSource();
            var resetEvent = new AutoResetEvent(false);

            using (var brokerage = GetBrokerage())
            {
                brokerage.Message += (_, brokerageMessageEvent) =>
                {
                    Log.Debug("");
                    Log.Debug($"Brokerage:Error: {brokerageMessageEvent.Message}");
                    resetEvent.Set();
                };

                do
                {
                    Log.Debug("");
                    Log.Debug($"BrokerageConnectionAndReconnectionTest: connection attempt: #{counter}");
                    brokerage.Connect();
                    Assert.IsTrue(brokerage.IsConnected);

                    // cool down 
                    Assert.IsTrue(resetEvent.WaitOne(TimeSpan.FromSeconds(60), cancellationTokenSource.Token));

                    //Assert.IsFalse(hasError);

                    Log.Debug("");
                    Log.Debug($"BrokerageConnectionAndReconnectionTest: disconnect attempt: #{counter}");
                    brokerage.Disconnect();
                    Assert.IsFalse(brokerage.IsConnected);

                    // cool down 
                    resetEvent.WaitOne(TimeSpan.FromSeconds(10), cancellationTokenSource.Token);

                } while (++counter < amountAttempt);
            }
        }

        /// <remarks>
        /// Actual response of 'POST https://api-sandbox.coinbase.com/api/v3/brokerage/orders', no top level 'order_id'
        /// </remarks>
        [Test]
        public void CreateOrderResponseReturnsTheOrderIdOfTheSuccessResponse()
        {
            var json = @"{""success"":true, ""success_response"":{""order_id"":""f898eaf4-6ffc-47be-a159-7ff292e5cdcf"", ""product_id"":""BTC-USD"",
""side"":""SELL"", ""client_order_id"":""sandbox_success_order"", ""attached_order_id"":""""}, ""order_configuration"":{""limit_limit_gtc"":
{""base_size"":""1"", ""limit_price"":""70000.44"", ""post_only"":false, ""rfq_disabled"":false, ""reduce_only"":false, ""currency_size"":null, ""oco_ref"":""""}}}";

            var response = JsonConvert.DeserializeObject<CoinbaseCreateOrderResponse>(json);

            Assert.IsTrue(response.Success);
            Assert.AreEqual("f898eaf4-6ffc-47be-a159-7ff292e5cdcf", response.OrderId);
            Assert.AreEqual("BTC-USD", response.SuccessResponse.Value.ProductId);
            Assert.AreEqual(OrderSide.Sell, response.SuccessResponse.Value.Side);
        }

        [TestCase(@"""order_id"": """",", TestName = "EmptyTopLevelOrderId")]
        [TestCase("", TestName = "NoTopLevelOrderId")]
        public void CreateOrderResponseFallsBackToTheSuccessResponseOrderId(string topLevelOrderId)
        {
            var json = @"{
    ""success"": true,
    ""failure_reason"": ""UNKNOWN_FAILURE_REASON""," + topLevelOrderId + @"
    ""success_response"": { ""order_id"": ""0e0b8a12-1234-4b0e-9b0e-1b0e0b8a1234"" }
}";

            var response = JsonConvert.DeserializeObject<CoinbaseCreateOrderResponse>(json);

            Assert.IsTrue(response.Success);
            Assert.AreEqual("0e0b8a12-1234-4b0e-9b0e-1b0e0b8a1234", response.OrderId);
        }

        [Test]
        public void CreateOrderResponseSupportsTheDeprecatedTopLevelOrderId()
        {
            var json = @"{
    ""success"": true,
    ""failure_reason"": ""UNKNOWN_FAILURE_REASON"",
    ""order_id"": ""0e0b8a12-1234-4b0e-9b0e-1b0e0b8a1234""
}";

            var response = JsonConvert.DeserializeObject<CoinbaseCreateOrderResponse>(json);

            Assert.IsTrue(response.Success);
            Assert.AreEqual("0e0b8a12-1234-4b0e-9b0e-1b0e0b8a1234", response.OrderId);
            Assert.IsFalse(response.SuccessResponse.HasValue);
        }

        [Test]
        public void CreateOrderResponseParsesTheErrorResponse()
        {
            var json = @"{
    ""success"": false,
    ""failure_reason"": ""UNKNOWN_FAILURE_REASON"",
    ""error_response"": {
        ""error"": ""INSUFFICIENT_FUND"",
        ""message"": ""Insufficient balance in source account"",
        ""error_details"": """",
        ""preview_failure_reason"": ""PREVIEW_INSUFFICIENT_FUND""
    }
}";

            var response = JsonConvert.DeserializeObject<CoinbaseCreateOrderResponse>(json);

            Assert.IsFalse(response.Success);
            Assert.IsNull(response.OrderId);
            Assert.AreEqual(FailureCreateOrderReason.InsufficientFund, response.ErrorResponse.Value.Error);
            Assert.AreEqual("PREVIEW_INSUFFICIENT_FUND", response.ErrorResponse.Value.PreviewFailureReason);
        }

        /// <summary>
        /// Live `user` channel messages of a filled market order: pending, open, a partial fill and 3 identical filled updates
        /// </summary>
        private static readonly string[] FilledMarketOrderUserMessages =
        [
            @"{""channel"":""user"",""timestamp"":""2026-08-31T17:21:57.391543058Z"",""sequence_num"":0,""events"":[{""type"":""snapshot"",""orders"":[]}]}",
            @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:02.825175497Z"",""sequence_num"":1,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""0"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0"",""filled_value"":""0"",""leaves_quantity"":""1.1"",""number_of_fills"":""0"",""order_id"":""554c0a44-e8f6-45a0-9df4-8f02cec2d309"",""order_side"":""BUY"",""order_type"":""Market"",""product_id"":""ETH-USDC"",""status"":""PENDING"",""total_fees"":""0"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}",
            @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:02.825197723Z"",""sequence_num"":2,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""0"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0"",""filled_value"":""0"",""leaves_quantity"":""1.1"",""number_of_fills"":""0"",""order_id"":""554c0a44-e8f6-45a0-9df4-8f02cec2d309"",""order_side"":""BUY"",""order_type"":""Market"",""product_id"":""ETH-USDC"",""status"":""OPEN"",""total_fees"":""0"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}",
            @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:02.825209944Z"",""sequence_num"":3,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""2468.31"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0.00043754"",""filled_value"":""1.0799843574"",""leaves_quantity"":""0.0070558303112"",""number_of_fills"":""1"",""order_id"":""554c0a44-e8f6-45a0-9df4-8f02cec2d309"",""order_side"":""BUY"",""order_type"":""Market"",""product_id"":""ETH-USDC"",""status"":""OPEN"",""total_fees"":""0.0129598122888"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}",
            @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:02.825222632Z"",""sequence_num"":4,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""2468.31"",""cancel_reason"":""Internal error"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0.00043754"",""filled_value"":""1.0799843574"",""leaves_quantity"":""0"",""number_of_fills"":""1"",""order_id"":""554c0a44-e8f6-45a0-9df4-8f02cec2d309"",""order_side"":""BUY"",""order_type"":""Market"",""product_id"":""ETH-USDC"",""status"":""FILLED"",""total_fees"":""0.0129598122888"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}",
            @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:03.075485575Z"",""sequence_num"":5,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""2468.31"",""cancel_reason"":""Internal error"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0.00043754"",""filled_value"":""1.0799843574"",""leaves_quantity"":""0"",""number_of_fills"":""1"",""order_id"":""554c0a44-e8f6-45a0-9df4-8f02cec2d309"",""order_side"":""BUY"",""order_type"":""Market"",""product_id"":""ETH-USDC"",""status"":""FILLED"",""total_fees"":""0.0129598122888"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}",
            @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:03.075516602Z"",""sequence_num"":6,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""2468.31"",""cancel_reason"":""Internal error"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0.00043754"",""filled_value"":""1.0799843574"",""leaves_quantity"":""0"",""number_of_fills"":""1"",""order_id"":""554c0a44-e8f6-45a0-9df4-8f02cec2d309"",""order_side"":""BUY"",""order_type"":""Market"",""product_id"":""ETH-USDC"",""status"":""FILLED"",""total_fees"":""0.0129598122888"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}",
        ];

        [Test]
        public void EmitsASingleFillForARepeatedFilledUpdate()
        {
            var symbol = Symbol.Create("ETHUSDC", SecurityType.Crypto, Market.Coinbase);
            var order = new MarketOrder(symbol, 0.00044567m, new DateTime(2026, 8, 31, 17, 22, 2, DateTimeKind.Utc));
            order.BrokerId.Add("554c0a44-e8f6-45a0-9df4-8f02cec2d309");

            var orderEvents = ReplayUserMessages(order, FilledMarketOrderUserMessages);

            // the three repeated filled updates must not be reported again
            Assert.AreEqual(2, orderEvents.Count);
            Assert.AreEqual(Orders.OrderStatus.PartiallyFilled, orderEvents[0].Status);
            Assert.AreEqual(0.00043754m, orderEvents[0].FillQuantity);
            Assert.AreEqual(2468.31m, orderEvents[0].FillPrice);
            Assert.AreEqual(Orders.OrderStatus.Filled, orderEvents[1].Status);

            // the emitted fills have to add up to what was actually filled
            Assert.AreEqual(0.00043754m, orderEvents.Sum(orderEvent => orderEvent.FillQuantity));
            Assert.AreEqual(0.0129598122888m, orderEvents.Sum(orderEvent => orderEvent.OrderFee.Value.Amount));
            Assert.AreEqual("USDC", orderEvents[0].OrderFee.Value.Currency);
        }

        /// <summary>
        /// An order filling 0.001 at 2000 and then 0.001 at 2200, so its 'avg_price' ends up at 2100
        /// </summary>
        private static readonly string[] TwoFillsUserMessages =
        [
            @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:02.825209944Z"",""sequence_num"":1,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""2000"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0.001"",""filled_value"":""2.0"",""leaves_quantity"":""0.001"",""number_of_fills"":""1"",""order_id"":""11111111-2222-3333-4444-555555555555"",""order_side"":""BUY"",""order_type"":""Limit"",""product_id"":""ETH-USDC"",""status"":""OPEN"",""total_fees"":""0.01"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}",
            @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:02.825209944Z"",""sequence_num"":2,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""2100"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0.002"",""filled_value"":""4.2"",""leaves_quantity"":""0"",""number_of_fills"":""1"",""order_id"":""11111111-2222-3333-4444-555555555555"",""order_side"":""BUY"",""order_type"":""Limit"",""product_id"":""ETH-USDC"",""status"":""FILLED"",""total_fees"":""0.021"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}",
        ];

        [Test]
        public void PricesEachFillByTheValueItAdded()
        {
            var symbol = Symbol.Create("ETHUSDC", SecurityType.Crypto, Market.Coinbase);
            var order = new LimitOrder(symbol, 0.002m, 2200m, new DateTime(2026, 8, 31, 17, 22, 2, DateTimeKind.Utc));
            order.BrokerId.Add("11111111-2222-3333-4444-555555555555");

            var orderEvents = ReplayUserMessages(order, TwoFillsUserMessages);

            Assert.AreEqual(2, orderEvents.Count);

            Assert.AreEqual(0.001m, orderEvents[0].FillQuantity);
            Assert.AreEqual(2000m, orderEvents[0].FillPrice);
            Assert.AreEqual(0.01m, orderEvents[0].OrderFee.Value.Amount);

            // the second fill added 2.2 for 0.001, so 2200 and not the 2100 order average
            Assert.AreEqual(0.001m, orderEvents[1].FillQuantity);
            Assert.AreEqual(2200m, orderEvents[1].FillPrice);
            Assert.AreEqual(0.011m, orderEvents[1].OrderFee.Value.Amount);

            // pricing each fill on its own keeps lean's average equal to the order 'avg_price'
            var quantity = orderEvents.Sum(orderEvent => orderEvent.FillQuantity);
            Assert.AreEqual(0.002m, quantity);
            Assert.AreEqual(2100m, orderEvents.Sum(orderEvent => orderEvent.FillPrice * orderEvent.FillQuantity) / quantity);
        }

        /// <summary>
        /// A partial fill followed by the re-delivery of a stale earlier update, out of order
        /// </summary>
        private static readonly string[] OutOfOrderUserMessages =
        [
            @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:02.825209944Z"",""sequence_num"":2,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""2100"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0.002"",""filled_value"":""4.2"",""leaves_quantity"":""0.001"",""number_of_fills"":""2"",""order_id"":""22222222-3333-4444-5555-666666666666"",""order_side"":""BUY"",""order_type"":""Limit"",""product_id"":""ETH-USDC"",""status"":""OPEN"",""total_fees"":""0.021"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}",
            @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:02.825209944Z"",""sequence_num"":1,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""2000"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0.001"",""filled_value"":""2.0"",""leaves_quantity"":""0.002"",""number_of_fills"":""1"",""order_id"":""22222222-3333-4444-5555-666666666666"",""order_side"":""BUY"",""order_type"":""Limit"",""product_id"":""ETH-USDC"",""status"":""OPEN"",""total_fees"":""0.01"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}",
        ];

        [Test]
        public void IgnoresAStaleUpdateThatArrivedOutOfOrder()
        {
            var symbol = Symbol.Create("ETHUSDC", SecurityType.Crypto, Market.Coinbase);
            var order = new LimitOrder(symbol, 0.003m, 2200m, new DateTime(2026, 8, 31, 17, 22, 2, DateTimeKind.Utc));
            order.BrokerId.Add("22222222-3333-4444-5555-666666666666");

            var orderEvents = ReplayUserMessages(order, OutOfOrderUserMessages);

            // the stale update holds quantity we already reported, it must not emit a reversing fill
            Assert.AreEqual(1, orderEvents.Count);
            Assert.AreEqual(0.002m, orderEvents[0].FillQuantity);
            Assert.AreEqual(2100m, orderEvents[0].FillPrice);
            Assert.AreEqual(0.021m, orderEvents[0].OrderFee.Value.Amount);
        }

        /// <summary>
        /// A partial fill of 0.001 at 2000, its order still has 0.001 to go
        /// </summary>
        private const string PartialFillUserMessage = @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:02.825209944Z"",""sequence_num"":1,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""2000"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0.001"",""filled_value"":""2.0"",""leaves_quantity"":""0.001"",""number_of_fills"":""1"",""order_id"":""33333333-4444-5555-6666-777777777777"",""order_side"":""BUY"",""order_type"":""Limit"",""product_id"":""ETH-USDC"",""status"":""OPEN"",""total_fees"":""0.01"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}";

        [Test]
        public void IgnoresARepeatedPartialFillUpdate()
        {
            var symbol = Symbol.Create("ETHUSDC", SecurityType.Crypto, Market.Coinbase);
            var order = new LimitOrder(symbol, 0.002m, 2200m, new DateTime(2026, 8, 31, 17, 22, 2, DateTimeKind.Utc));
            order.BrokerId.Add("33333333-4444-5555-6666-777777777777");

            var orderEvents = ReplayUserMessages(order, [PartialFillUserMessage, PartialFillUserMessage]);

            // the duplicate holds no new quantity, it must not emit a zero quantity fill
            Assert.AreEqual(1, orderEvents.Count);
            Assert.AreEqual(0.001m, orderEvents[0].FillQuantity);
            Assert.AreEqual(2000m, orderEvents[0].FillPrice);
        }

        /// <summary>
        /// The first fill update carries no 'filled_value', the second does
        /// </summary>
        private static readonly string[] MissingFilledValueUserMessages =
        [
            @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:02.825209944Z"",""sequence_num"":1,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""2000"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0.001"",""leaves_quantity"":""0.001"",""number_of_fills"":""1"",""order_id"":""44444444-5555-6666-7777-888888888888"",""order_side"":""BUY"",""order_type"":""Limit"",""product_id"":""ETH-USDC"",""status"":""OPEN"",""total_fees"":""0.01"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}",
            @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:02.825209944Z"",""sequence_num"":2,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""2100"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0.002"",""filled_value"":""4.2"",""leaves_quantity"":""0"",""number_of_fills"":""2"",""order_id"":""44444444-5555-6666-7777-888888888888"",""order_side"":""BUY"",""order_type"":""Limit"",""product_id"":""ETH-USDC"",""status"":""FILLED"",""total_fees"":""0.021"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}",
        ];

        [Test]
        public void PricesTheFillWhenAnUpdateOmitsFilledValue()
        {
            var symbol = Symbol.Create("ETHUSDC", SecurityType.Crypto, Market.Coinbase);
            var order = new LimitOrder(symbol, 0.002m, 2200m, new DateTime(2026, 8, 31, 17, 22, 2, DateTimeKind.Utc));
            order.BrokerId.Add("44444444-5555-6666-7777-888888888888");

            var orderEvents = ReplayUserMessages(order, MissingFilledValueUserMessages);

            Assert.AreEqual(2, orderEvents.Count);

            // no 'filled_value', so the first fill is priced at the order average
            Assert.AreEqual(0.001m, orderEvents[0].FillQuantity);
            Assert.AreEqual(2000m, orderEvents[0].FillPrice);

            // the value baseline must advance with the first fill, else this would be (4.2 - 0) / 0.001 = 4200
            Assert.AreEqual(0.001m, orderEvents[1].FillQuantity);
            Assert.AreEqual(2200m, orderEvents[1].FillPrice);
            Assert.AreEqual(Orders.OrderStatus.Filled, orderEvents[1].Status);
        }

        /// <summary>
        /// A filled update that carries no 'leaves_quantity'
        /// </summary>
        private const string FilledWithoutLeavesQuantityUserMessage = @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:02.825209944Z"",""sequence_num"":1,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""2200"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0.001"",""filled_value"":""2.2"",""number_of_fills"":""1"",""order_id"":""55555555-6666-7777-8888-999999999999"",""order_side"":""BUY"",""order_type"":""Limit"",""product_id"":""ETH-USDC"",""status"":""FILLED"",""total_fees"":""0.01"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}";

        [Test]
        public void ClosesTheOrderWhenTheFilledUpdateOmitsLeavesQuantity()
        {
            var symbol = Symbol.Create("ETHUSDC", SecurityType.Crypto, Market.Coinbase);
            var order = new LimitOrder(symbol, 0.001m, 2200m, new DateTime(2026, 8, 31, 17, 22, 2, DateTimeKind.Utc));
            order.BrokerId.Add("55555555-6666-7777-8888-999999999999");

            var orderEvents = ReplayUserMessages(order, [FilledWithoutLeavesQuantityUserMessage]);

            // the status is what closes the order, else it would sit at partially filled forever
            Assert.AreEqual(1, orderEvents.Count);
            Assert.AreEqual(Orders.OrderStatus.Filled, orderEvents[0].Status);
            Assert.AreEqual(0.001m, orderEvents[0].FillQuantity);
        }

        /// <summary>
        /// An update holding an order with no 'order_id' followed by a filled one
        /// </summary>
        private const string OrderWithoutIdUserMessage = @"{""channel"":""user"",""timestamp"":""2026-08-31T17:22:02.825209944Z"",""sequence_num"":1,""events"":[{""type"":""update"",""orders"":[{""avg_price"":""0"",""cumulative_quantity"":""0"",""status"":""OPEN""},{""avg_price"":""2200"",""client_order_id"":""e59c259c-f05c-4784-b0dd-96dc61fc67ae"",""cumulative_quantity"":""0.001"",""filled_value"":""2.2"",""leaves_quantity"":""0"",""number_of_fills"":""1"",""order_id"":""66666666-7777-8888-9999-000000000000"",""order_side"":""BUY"",""order_type"":""Limit"",""product_id"":""ETH-USDC"",""status"":""FILLED"",""total_fees"":""0.01"",""creation_time"":""2026-08-31T17:22:02.591563Z""}]}]}";

        [Test]
        public void ReportsTheFillOfAnUpdateHoldingAnOrderWithoutId()
        {
            var symbol = Symbol.Create("ETHUSDC", SecurityType.Crypto, Market.Coinbase);
            var order = new LimitOrder(symbol, 0.001m, 2200m, new DateTime(2026, 8, 31, 17, 22, 2, DateTimeKind.Utc));
            order.BrokerId.Add("66666666-7777-8888-9999-000000000000");

            var orderEvents = ReplayUserMessages(order, [OrderWithoutIdUserMessage]);

            // the order with no id must be skipped without taking the rest of the update down with it
            Assert.AreEqual(1, orderEvents.Count);
            Assert.AreEqual(Orders.OrderStatus.Filled, orderEvents[0].Status);
            Assert.AreEqual(0.001m, orderEvents[0].FillQuantity);
        }

        /// <summary>
        /// Replays the web socket messages and returns the order events they emitted
        /// </summary>
        private static List<OrderEvent> ReplayUserMessages(Order order, IEnumerable<string> messages)
        {
            var orderProvider = new FakeOrderProcessor();
            orderProvider.AddOrder(order);

            var orderEvents = new List<OrderEvent>();
            var brokerage = new TestCoinbaseBrokerage(orderProvider);
            brokerage.OrdersStatusChanged += (_, events) => orderEvents.AddRange(events);

            foreach (var message in messages)
            {
                brokerage.ReceiveMessage(message);
            }

            return orderEvents;
        }

        /// <summary>
        /// Exposes the web socket message handler without the brokerage initialization
        /// </summary>
        private class TestCoinbaseBrokerage : CoinbaseBrokerage
        {
            public TestCoinbaseBrokerage(IOrderProvider orderProvider)
            {
                OrderProvider = orderProvider;
            }

            public void ReceiveMessage(string message)
            {
                OnMessage(this, new WebSocketMessage(null, new WebSocketClientWrapper.TextMessage { Message = message }));
            }
        }

        private static CoinbaseBrokerage GetBrokerage()
        {
            var wssUrl = Config.Get("coinbase-url", "wss://advanced-trade-ws.coinbase.com");
            var restApiUrl = Config.Get("coinbase-rest-api", "https://api.coinbase.com");
            var name = Config.Get("coinbase-api-name");
            var privateKey = Config.Get("coinbase-api-private-key");
            var algorithm = new QCAlgorithm();

            return new CoinbaseBrokerage(wssUrl, name, privateKey, restApiUrl, algorithm, null);
        }
    }
}
