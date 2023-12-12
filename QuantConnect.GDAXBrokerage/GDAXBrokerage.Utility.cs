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
using RestSharp;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using QuantConnect.CoinbaseBrokerage.Models;
using BrokerageEnums = QuantConnect.CoinbaseBrokerage.Models.Enums;

namespace QuantConnect.Brokerages.GDAX
{
    /// <summary>
    /// Utility methods for GDAX brokerage
    /// </summary>
    public partial class GDAXBrokerage
    {
        /// <summary>
        /// Sign Header
        /// </summary>
        public const string SignHeader = "CB-ACCESS-SIGN";
        /// <summary>
        /// Key Header
        /// </summary>
        public const string KeyHeader = "CB-ACCESS-KEY";
        /// <summary>
        /// Timestamp Header
        /// </summary>
        public const string TimeHeader = "CB-ACCESS-TIMESTAMP";
        private const string Open = "OPEN";
        private const string Pending = "pending";
        private const string Active = "active";
        private const string Done = "done";
        private const string Settled = "settled";

        /// <summary>
        /// Creates an auth token and adds to the request
        /// </summary>
        /// <param name="request">the rest request</param>
        /// <returns>a token representing the request params</returns>
        public AuthenticationToken GetAuthenticationToken(IRestRequest request)
        {
            var body = request.Parameters.SingleOrDefault(b => b.Type == ParameterType.RequestBody);

            var url = RestClient.BuildUri(request).AbsolutePath;

            var token = GetAuthenticationToken(body?.Value.ToString() ?? string.Empty, request.Method.ToString().ToUpperInvariant(), url);

            request.AddHeader(KeyHeader, ApiKey);
            request.AddHeader(SignHeader, token.Signature);
            request.AddHeader(TimeHeader, token.Timestamp);

            return token;
        }

        /// <summary>
        /// Creates an auth token to sign a request
        /// </summary>
        /// <param name="body">the request body as json</param>
        /// <param name="method">the http method</param>
        /// <param name="url">the request url</param>
        /// <returns></returns>
        public AuthenticationToken GetAuthenticationToken(string body, string method, string url)
        {
            var token = new AuthenticationToken
            {
                Key = ApiKey,
                //todo: query time server to correct for time skew
                Timestamp = Time.DateTimeToUnixTimeStamp(DateTime.UtcNow).ToString("F0", CultureInfo.InvariantCulture)
            };

            var prehash = token.Timestamp + method + url + body;

            var hmacKey = Encoding.UTF8.GetBytes(ApiSecret);
            var dataBytes = Encoding.UTF8.GetBytes(prehash);

            using (var hmac = new HMACSHA256(hmacKey))
            {
                var sig = hmac.ComputeHash(dataBytes);
                token.Signature = Convert.ToHexString(sig).ToLower();
            }

            return token;
        }

        private static Orders.OrderStatus ConvertOrderStatus(CoinbaseOrder order)
        {
            if (order.CompletionPercentage > 0 && order.CompletionPercentage != 100)
            {
                return Orders.OrderStatus.PartiallyFilled;
            }
            else if (order.Status == BrokerageEnums.OrderStatus.OPEN)
            {
                return Orders.OrderStatus.Submitted;
            }
            else if (order.Status == BrokerageEnums.OrderStatus.FILLED)
            {
                return Orders.OrderStatus.Filled;
            }

            return Orders.OrderStatus.None;
        }
    }
}
