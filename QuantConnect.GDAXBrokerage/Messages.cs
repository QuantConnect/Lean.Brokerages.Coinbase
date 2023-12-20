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
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.GDAX.Messages
{
    public class Fill
    {
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("trade_id")]
        public long TradeId { get; set; }

        [JsonProperty("product_id")]
        public string ProductId { get; set; }

        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("profile_id")]
        public string ProfileId { get; set; }

        [JsonProperty("liquidity")]
        public string Liquidity { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("size")]
        public decimal Size { get; set; }

        [JsonProperty("fee")]
        public decimal Fee { get; set; }

        [JsonProperty("side")]
        public string Side { get; set; }

        [JsonProperty("settled")]
        public bool Settled { get; set; }

        [JsonProperty("usd_volume")]
        public decimal UsdVolume { get; set; }
    }
}
