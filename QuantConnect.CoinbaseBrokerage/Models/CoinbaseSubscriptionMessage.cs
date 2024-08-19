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
using System.Collections.Generic;
using QuantConnect.Brokerages.Coinbase.Models.Enums;

namespace QuantConnect.Brokerages.Coinbase.Models;

/// <summary>
/// Represents a message used for subscribing to WebSocket channels on Coinbase.
/// </summary>
public readonly struct CoinbaseSubscriptionMessage
{
    /// <summary>
    /// Gets the JWT for authentication.
    /// </summary>
    [JsonProperty("jwt")]
    public string JWT { get; }

    /// <summary>
    /// Gets the channel to subscribe to.
    /// </summary>
    [JsonProperty("channel")]
    public string Channel { get; }

    /// <summary>
    /// Gets the list of product IDs associated with the subscription.
    /// </summary>
    [JsonProperty("product_ids")]
    public List<string> ProductIds { get; }

    /// <summary>
    /// Gets the type of WebSocket subscription.
    /// </summary>
    [JsonProperty("type")]
    public WebSocketSubscriptionType Type { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoinbaseSubscriptionMessage"/> struct.
    /// </summary>
    /// <param name="channel">The channel to subscribe to.</param>
    /// <param name="productIds">The list of product IDs associated with the subscription.</param>
    /// <param name="jwtToken">The generated JWT token for authentication.</param>
    /// <param name="type">The type of WebSocket subscription.</param>
    [JsonConstructor]
    public CoinbaseSubscriptionMessage(string channel, List<string> productIds, string jwtToken, WebSocketSubscriptionType type)
    {
        JWT = jwtToken;
        Channel = channel;
        ProductIds = productIds;
        Type = type;
    }
}
