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
using QuantConnect.Configuration;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using QuantConnect.Brokerages.Coinbase.Api;
using QuantConnect.Brokerages.Coinbase.Models.Enums;
using QuantConnect.Brokerages.Coinbase.Models.WebSocket;

namespace QuantConnect.Brokerages.Coinbase.Tests
{
    [TestFixture]
    [Explicit("Use tests for more clarification of API")]
    public class CoinbaseApiTests
    {
        private CoinbaseApi CoinbaseApi { get; set; }

        [SetUp]
        public void Setup()
        {
            var name = Config.Get("coinbase-api-name");
            var priavteKey = Config.Get("coinbase-api-private-key");

            CoinbaseApi = CreateCoinbaseApi(name, priavteKey);
        }

        [TestCase("", "", typeof(ArgumentOutOfRangeException))]
        [TestCase("organizations/2c7dhs-a3a3-4acf-aa0c-f68584f34c37/apiKeys/41090ffa-asd2-4040-815f-afaf63747e35", "-----BEGIN EC PRIVATE KEY-----\nMHcCAQEEIPcJGfXYEdLQi0iFj1xvGfPwuRNoeddwuKS4xL2NrlGWpoAoGCCqGSM49\nAwEHoUQDQgAEclN+asd/EhJ3UjOWkHmP/iqGBv5NkNJ75bUq\nVgxS4aU3/djHiIuSf27QasdOFIDGJLmOn7YiQ==\n-----END EC PRIVATE KEY-----\n", typeof(CryptographicException))]
        [TestCase("organizations/2c7dhs-a3a3-4acf-aa0c-f68584f34c37/apiKeys/41090ffa-asd2-4040-815f-afaf63747e35", "MHcCAQEEIPcJGfXYEdLQi0iFj1xvGfPwuRNoeddwuKS4xL2NrlGWpoAoGCCqGSM49\nAwEHoUQDQgAEclN+asd/EhJ3UjOWkHmP/iqGBv5NkNJ75bUq\nVgxS4aU3/djHiIuSf27QasdOFIDGJLmOn7YiQ==", typeof(CryptographicException))]
        public void InvalidAuthenticationCredentialsShouldThrowException(string name, string privateKey, Type expectedException)
        {
            try
            {
                var coinbaseApi = CreateCoinbaseApi(name, privateKey);

                // call random endpoint with incorrect credential
                Assert.Throws(expectedException, () => coinbaseApi.GetAccounts());
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf(expectedException, ex);
            }
        }

        [Test]
        public void GetListAccounts()
        {
            var accounts = CoinbaseApi.GetAccounts();

            Assert.Greater(accounts.Count(), 0);

            foreach (var account in accounts)
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

        [TestCase("/api/v3/brokerage/orders", null, "Bad Request")]
        [TestCase("/api/v3/brokerage/orders", "", "Bad Request")]
        [TestCase("/api/v3/brokerage/orders", "{null}", "Bad Request")]
        [TestCase("/api/v3/brokerage/orders", "[]", "Bad Request")]
        public void ValidateCoinbaseRestRequestWithWrongBodyParameter(string uriPath, object bodyData, string message)
        {
            var name = Config.Get("coinbase-api-name");
            var privateKey = Config.Get("coinbase-api-private-key");
            var restApiUrl = Config.Get("coinbase-rest-api", "https://api.coinbase.com");

            var request = new RestRequest($"{uriPath}", Method.POST);

            var _apiClient = new CoinbaseApiClient(name, privateKey, restApiUrl, 30);

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

        [Test]
        public void ValidateGenerateWebSocketJWTToken()
        {
            var name = Config.Get("coinbase-api-name");
            var privateKey = Config.Get("coinbase-api-private-key");
            var restApiUrl = Config.Get("coinbase-rest-api", "https://api.coinbase.com");

            var _apiClient = new CoinbaseApiClient(name, privateKey, restApiUrl, 30);

            var generateWebSocketJWTToken = _apiClient.GenerateWebSocketToken();

            var parsedPrivateKey = _apiClient.ParseKey(privateKey);

            Assert.IsTrue(IsTokenValid(generateWebSocketJWTToken, name, parsedPrivateKey));
        }

        /// <summary>
        /// Validates a JWT token using ECDsa key with the specified token ID and secret.
        /// </summary>
        /// <param name="token">The JWT token to be validated.</param>
        /// <param name="tokenId">The unique identifier for the ECDsa security key.</param>
        /// <param name="parsedPrivateKey">The ECDsa private key in Base64 format used to validate the token's signature.</param>
        /// <returns>
        /// <c>true</c> if the token is successfully validated; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method is useful for verifying the authenticity of JWT tokens using ECDsa keys. 
        /// It ensures that the token's signature matches the expected signature derived from the provided secret.
        /// </remarks>
        private bool IsTokenValid(string token, string tokenId, string parsedPrivateKey)
        {
            if (token == null)
                return false;

            var key = ECDsa.Create();
            key?.ImportECPrivateKey(Convert.FromBase64String(parsedPrivateKey), out _);

            var securityKey = new ECDsaSecurityKey(key) { KeyId = tokenId };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.FromSeconds(100),
                    ValidateLifetime = true,
                    LifetimeValidator = CustomLifetimeValidator,
                }, out var validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Custom validator for checking the token's lifetime.
        /// </summary>
        /// <param name="notBefore">The 'Not Before' date/time from the token's claims.</param>
        /// <param name="expires">The expiration date/time from the token's claims.</param>
        /// <param name="tokenToValidate">The security token being validated.</param>
        /// <param name="param">The token validation parameters.</param>
        /// <returns>
        /// <c>true</c> if the token is valid based on its expiration time; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This custom lifetime validator ensures that the JWT token has not expired. It compares the 
        /// token's expiration time against the current UTC time to determine its validity.
        /// </remarks>
        private static bool CustomLifetimeValidator(DateTime? notBefore, DateTime? expires, SecurityToken tokenToValidate, TokenValidationParameters @param)
        {
            if (expires != null)
            {
                return expires > DateTime.UtcNow;
            }
            return false;
        }

        private CoinbaseApi CreateCoinbaseApi(string name, string privateKey)
        {
            var restApiUrl = Config.Get("coinbase-rest-api", "https://api.coinbase.com");

            return new CoinbaseApi(new SymbolPropertiesDatabaseSymbolMapper(Market.Coinbase), null, name, privateKey, restApiUrl);
        }
    }
}
