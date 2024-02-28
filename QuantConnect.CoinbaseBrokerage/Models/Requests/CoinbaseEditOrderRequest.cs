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

using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Coinbase.Models.Requests;

/// <summary>
/// Coinbase edit order api request
/// </summary>
public readonly struct CoinbaseEditOrderRequest
{
    /// <summary>
    /// ID of order to edit.
    /// </summary>
    [JsonProperty("order_id")]
    public string OrderId { get; }

    /// <summary>
    /// New price for order.
    /// </summary>
    [JsonProperty("price")]
    public decimal Price { get; }

    /// <summary>
    /// New size for order
    /// </summary>
    [JsonProperty("size")]
    public decimal Size { get; }

    /// <summary>
    /// Initialize new instance of <see cref="CoinbaseEditOrderRequest"/>
    /// </summary>
    /// <param name="orderId">ID of order to edit.</param>
    /// <param name="price">New price for order.</param>
    /// <param name="size">New size for order</param>
    [JsonConstructor]
    public CoinbaseEditOrderRequest(string orderId, decimal price, decimal size)
    {
        OrderId = orderId;
        Price = price;
        Size = size;
    }
}
