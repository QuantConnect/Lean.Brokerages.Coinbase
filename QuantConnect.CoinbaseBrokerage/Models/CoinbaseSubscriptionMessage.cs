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
using QuantConnect.CoinbaseBrokerage.Models.Enums;

namespace QuantConnect.CoinbaseBrokerage.Models;

/// <summary>
/// Represents a message used for subscribing to WebSocket channels on Coinbase.
/// </summary>
public readonly struct CoinbaseSubscriptionMessage
{
    /// <summary>
    /// Gets the API key for authentication (if required).
    /// </summary>
    [JsonProperty("api_key")]
    public string ApiKey { get; }

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
    /// Gets the signature for authentication (if required).
    /// </summary>
    [JsonProperty("signature")]
    public string Signature { get; }

    /// <summary>
    /// Gets the timestamp of the subscription message.
    /// </summary>
    [JsonProperty("timestamp")]
    public string Timestamp { get; }

    /// <summary>
    /// Gets the type of WebSocket subscription.
    /// </summary>
    [JsonProperty("type")]
    public WebSocketSubscriptionType Type { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoinbaseSubscriptionMessage"/> struct.
    /// </summary>
    /// <param name="apiKey">The API key for authentication (if required).</param>
    /// <param name="channel">The channel to subscribe to.</param>
    /// <param name="productIds">The list of product IDs associated with the subscription.</param>
    /// <param name="signature">The signature for authentication (if required).</param>
    /// <param name="timestamp">The timestamp of the subscription message.</param>
    /// <param name="type">The type of WebSocket subscription.</param>
    [JsonConstructor]
    public CoinbaseSubscriptionMessage(string apiKey, string channel, List<string> productIds,
        string signature, string timestamp, WebSocketSubscriptionType type)
    {
        ApiKey = apiKey;
        Channel = channel;
        ProductIds = productIds;
        Signature = signature;
        Timestamp = timestamp;
        Type = type;
    }
}
