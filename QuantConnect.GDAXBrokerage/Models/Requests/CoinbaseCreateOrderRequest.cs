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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QuantConnect.CoinbaseBrokerage.Models.Enums;

namespace QuantConnect.CoinbaseBrokerage.Models.Requests;

/// <summary>
/// Coinbase place order api request
/// </summary>
public struct CoinbaseCreateOrderRequest
{
    [JsonProperty("client_order_id")]
    public Guid ClientOrderId { get; }

    [JsonProperty("product_id")]
    public string ProductId { get; }

    [JsonProperty("side")]
    [JsonConverter(typeof(StringEnumConverter))]
    public OrderSide Side { get; }

    [JsonProperty("order_configuration")]
    public OrderConfiguration OrderConfiguration { get; set; }

    /// <summary>
    /// Self trade prevention ID, to prevent an order crossing against the same user
    /// </summary>
    [JsonProperty("self_trade_prevention_id")]
    public Guid? SelfTradePreventionId { get; set; } = null;

    public CoinbaseCreateOrderRequest(Guid clientOrderId, string productId, OrderSide side): this()
    {
        ClientOrderId = clientOrderId;
        ProductId = productId;
        Side = side;
    }
}