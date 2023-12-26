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
using RestSharp;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using System.Collections.Generic;
using QuantConnect.CoinbaseBrokerage.Api;
using QuantConnect.CoinbaseBrokerage.Models.Enums;
using QuantConnect.CoinbaseBrokerage.Models.WebSocket;

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
            Assert.Throws<Exception>(() => coinbaseApi.GetAccounts());
        }

        [Test] 
        public void GetListAccounts() 
        {
            var accounts = CoinbaseApi.GetAccounts();

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

        [TestCase(OrderStatus.UnknownOrderStatus)]
        [TestCase(OrderStatus.Open)]
        [TestCase(OrderStatus.Filled)]
        [TestCase(OrderStatus.Cancelled)]
        [TestCase(OrderStatus.Expired)]
        [TestCase(OrderStatus.Failed)]
        public void GetListOrdersWithDifferentOrderStatus(OrderStatus orderStatus)
        {
            var orders = CoinbaseApi.GetOrders(orderStatus);
            Assert.IsNotNull(orders);
        }

        [TestCase(OrderStatus.Pending)]
        public void GetListOrderWithNotSupportedOrderStatus(OrderStatus orderStatus)
        {
            Assert.Throws<Exception>(() => CoinbaseApi.GetOrders(orderStatus));
        }

        [TestCase(CandleGranularity.OneMinute, "25/12/2023 06:30:15", "25/12/2023 06:35:15")]
        [TestCase(CandleGranularity.OneHour, "25/12/2023 06:30:15", "25/12/2023 11:35:15")]
        [TestCase(CandleGranularity.OneDay, "24/12/2023 06:30:15", "26/12/2023 06:35:15")]
        public void GetProductCandlesWithDifferentCandleGranularity(CandleGranularity candleGranularity, string startDate, string endDate, string productId = "BTC-USDC")
        {
            var startDateTime = DateTime.ParseExact(startDate, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            var endDateTime = DateTime.ParseExact(endDate, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

            var candles = CoinbaseApi.GetProductCandles(productId, startDateTime, endDateTime, candleGranularity);

            Assert.IsNotNull(candles);
            Assert.Greater(candles.Count(), 0);
        }

        [TestCase(CandleGranularity.UnknownGranularity, "25/12/2023 06:30:15", "25/12/2023 06:35:15")]
        public void GetProductCandlesWithNotSupportedCandleGranularity(CandleGranularity candleGranularity, string startDate, string endDate, string productId = "BTC-USDC")
        {
            var startDateTime = DateTime.ParseExact(startDate, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            var endDateTime = DateTime.ParseExact(endDate, "dd/MM/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

            Assert.Throws<Exception>(() => CoinbaseApi.GetProductCandles(productId, startDateTime, endDateTime, candleGranularity));
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
                Assert.GreaterOrEqual(tick.NewQuantity, 0);
                Assert.GreaterOrEqual(tick.PriceLevel, 0);
                Assert.IsInstanceOf<CoinbaseLevel2UpdateSide>(tick.Side);
            }            
        }

        [TestCase("/api/v3/brokerage/orders", null, "Unauthorized")]
        [TestCase("/api/v3/brokerage/orders", "", "Unauthorized")]
        [TestCase("/api/v3/brokerage/orders", "{null}", "Bad Request")]
        [TestCase("/api/v3/brokerage/orders", "[]", "Unauthorized")]
        public void ValidateCoinbaseRestRequestWithWrongBodyParameter(string uriPath, object bodyData, string message)
        {
            var apiKey = Config.Get("coinbase-api-key");
            var apiKeySecret = Config.Get("coinbase-api-secret");
            var restApiUrl = Config.Get("coinbase-api-url", "https://api.coinbase.com");

            var request = new RestRequest($"{uriPath}", Method.POST);

            var _apiClient = new CoinbaseApiClient(apiKey, apiKeySecret, restApiUrl, 30);

            request.AddJsonBody(bodyData);

            var exception = Assert.Throws<Exception>(() => _apiClient.ExecuteRequest(request));
            Assert.IsTrue(exception.Message.Contains(message));
        }

        [TestCase("", "INVALID_CANCEL_REQUEST")]
        [TestCase("44703527-de90-4aac-ae52-8e6910dee426", "UNKNOWN_CANCEL_ORDER")]
        public void CancelOrderWithWrongOrderId(string orderId, string errorMessage)
        {
            var fakeBrokerIds = new List<string>()
            {
                orderId
            };

            var response = CoinbaseApi.CancelOrders(fakeBrokerIds);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(errorMessage, response.FailureReason);
        }

        private CoinbaseApi CreateCoinbaseApi(string apiKey, string apiKeySecret)
        {
            var restApiUrl = Config.Get("coinbase-api-url", "https://api.coinbase.com");

            return new CoinbaseApi(new SymbolPropertiesDatabaseSymbolMapper(Market.GDAX), null, apiKey, apiKeySecret, restApiUrl);
        }
    }
}
