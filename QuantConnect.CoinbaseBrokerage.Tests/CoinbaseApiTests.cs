/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2023 QuantConnect Corporation.
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
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.CoinbaseBrokerage.Api;
using QuantConnect.CoinbaseBrokerage.Models.Enums;
using QuantConnect.CoinbaseBrokerage.Models.WebSocket;
using Newtonsoft.Json.Linq;
using QLNet;
using QuantConnect.Api;

namespace QuantConnect.CoinbaseBrokerage.Tests
{
    [TestFixture]
    [Explicit("Use tests for more clarification of API")]
    public class CoinbaseApiTests
    {
        private CoinbaseApi CoinbaseApi { get; set; }

        [SetUp]
        public void Setup()
        {
            var apiKey = Config.Get("coinbase-api-key");
            var apiKeySecret = Config.Get("coinbase-api-secret");

            CoinbaseApi = CreateCoinbaseApi(apiKey, apiKeySecret);
        }

        [TestCase("", "")]
        [TestCase("1", "2")]
        public void InvalidAuthenticationCredentialsShouldThrowException(string apiKey, string apiKeySecret)
        {
            var coinbaseApi = CreateCoinbaseApi(apiKey, apiKeySecret);

            // call random endpoint with incorrect credential
            Assert.Throws<Exception>(() => coinbaseApi.GetListAccounts());
        }

        [Test] 
        public void GetListAccounts() 
        {
            var accounts = CoinbaseApi.GetListAccounts();

            Assert.Greater(accounts.Count(), 0);

            foreach(var account in accounts)
            {
                Assert.IsTrue(account.Active);
                Assert.IsNotEmpty(account.Name);
                Assert.IsNotEmpty(account.Currency);
                Assert.IsNotEmpty(account.AvailableBalance.Currency);
                Assert.GreaterOrEqual(account.AvailableBalance.Value, 0);
                Assert.That(account.CreatedAt, Is.LessThan(DateTime.UtcNow));
            }
        }

        [Test]
        public void GetListOrdersWithDifferentOrderStatus()
        {
            foreach (var orderStatus in (OrderStatus[])Enum.GetValues(typeof(OrderStatus)))
            {
                // Not supported order status request
                if(OrderStatus.Pending == orderStatus)
                {
                    Assert.Throws<Exception>(() => CoinbaseApi.GetListOrders(orderStatus));
                    continue;
                }

                var orders = CoinbaseApi.GetListOrders(orderStatus);
                Assert.IsNotNull(orders);
            }
        }

        [TestCase("BTC-USDC")]
        public void GetProductCandlesWithDifferentCandleGranularity(string productId)
        {
            foreach (var candleGranularity in (CandleGranularity[])Enum.GetValues(typeof(CandleGranularity)))
            {
                var startDateTime = candleGranularity switch
                {
                    CandleGranularity.OneMinute => DateTime.UtcNow.AddMinutes(-10),
                    CandleGranularity.OneHour => DateTime.UtcNow.AddHours(-5),
                    CandleGranularity.OneDay => DateTime.UtcNow.AddDays(-7),
                    _ => DateTime.UtcNow.AddHours(-10)
                };

                // Not supported candle granularity request
                if (candleGranularity == CandleGranularity.UnknownGranularity)
                {
                    Assert.Throws<Exception>(() => CoinbaseApi.GetProductCandles(productId, startDateTime, DateTime.UtcNow, candleGranularity));
                    continue;
                }

                var candles = CoinbaseApi.GetProductCandles(productId, startDateTime, DateTime.UtcNow, candleGranularity);

                Assert.IsNotNull(candles);
                Assert.Greater(candles.Count(), 0);
            }
        }

        [Test]
        public void ParseWebSocketLevel2DataResponse()
        {
            #region Json Response
            string jsonLevel2Message = @"{
  ""channel"": ""l2_data"",
  ""client_id"": """",
  ""timestamp"": ""2023-12-22T11:03:21.339953956Z"",
  ""sequence_num"": 18,
  ""events"": [
    {
      ""type"": ""update"",
      ""product_id"": ""BTC-USD"",
      ""updates"": [
        {
          ""side"": ""bid"",
          ""event_time"": ""2023-12-22T11:03:21.141822Z"",
          ""price_level"": ""43640.01"",
          ""new_quantity"": ""0""
        },
        {
          ""side"": ""offer"",
          ""event_time"": ""2023-12-22T11:03:21.141822Z"",
          ""price_level"": ""44162.87"",
          ""new_quantity"": ""0.77908682""
        }
      ]
    }
  ]
}";
            #endregion

            var obj = JObject.Parse(jsonLevel2Message);

            var level2Data = obj.ToObject<CoinbaseWebSocketMessage<CoinbaseLevel2Event>>();

            Assert.IsNotNull(level2Data);
            Assert.AreEqual("l2_data", level2Data.Channel);
            Assert.IsEmpty(level2Data.ClientId);
            Assert.IsNotEmpty(level2Data.SequenceNumber);
            Assert.IsInstanceOf<DateTimeOffset>(level2Data.Timestamp);

            Assert.Greater(level2Data.Events.Count, 0);
            Assert.AreEqual("BTC-USD", level2Data.Events[0].ProductId);
            Assert.IsTrue(level2Data.Events[0].Type == WebSocketEventType.Update);
            Assert.Greater(level2Data.Events[0].Updates.Count, 0);
            foreach (var tick in level2Data.Events[0].Updates)
            {
                Assert.IsInstanceOf<DateTimeOffset>(tick.EventTime);
                Assert.GreaterOrEqual(tick.NewQuantity, 0);
                Assert.GreaterOrEqual(tick.PriceLevel, 0);
                Assert.IsInstanceOf<CoinbaseLevel2UpdateSide>(tick.Side);
            }            
        }

        private CoinbaseApi CreateCoinbaseApi(string apiKey, string apiKeySecret)
        {
            var restApiUrl = Config.Get("coinbase-api-url", "https://api.coinbase.com");

            return new CoinbaseApi(new SymbolPropertiesDatabaseSymbolMapper(Market.GDAX), null, apiKey, apiKeySecret, restApiUrl);
        }
    }
}
