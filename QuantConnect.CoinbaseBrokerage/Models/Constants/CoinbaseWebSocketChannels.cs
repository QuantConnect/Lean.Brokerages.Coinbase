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

using System.Collections.Generic;

namespace QuantConnect.Brokerages.Coinbase.Models.Constants;

/// <summary>
/// The Brokerage Coinbase WebSocket feed provides the following channels
/// </summary>
public sealed class CoinbaseWebSocketChannels
{
    /// <summary>
    /// Real-time server pings to keep all connections open
    /// </summary>
    /// <remarks>
    /// Subscribe to the heartbeats channel to receive heartbeats messages for specific products every second.
    /// Heartbeats include a heartbeat_counter which verifies that no messages were missed.
    /// </remarks>
    public const string Heartbeats = "heartbeats";

    /// <summary>
    /// Only sends messages that include the authenticated user
    /// </summary>
    /// <remarks>
    /// The user channel sends updates on all of a user's open orders, including all subsequent updates of those orders.
    /// If none are provided, the WebSocket subscription is open to all product IDs.
    /// </remarks>
    public const string User = "user";

    /// <summary>
    /// All updates and easiest way to keep order book snapshot
    /// Use: when subscribe on channel update
    /// </summary>
    /// <remarks>
    /// The level2 channel guarantees delivery of all updates and is the easiest way to keep a snapshot of the order book.
    /// </remarks>
    public const string Level2Request = "level2";

    /// <summary>
    /// All updates and easiest way to keep order book snapshot
    /// Use: when parse response 
    /// </summary>
    /// <remarks>
    /// The level2 channel guarantees delivery of all updates and is the easiest way to keep a snapshot of the order book.
    /// </remarks>
    public const string Level2Response = "l2_data";

    /// <summary>
    /// Real-time updates every time a market trade happens
    /// </summary>
    /// <remarks>
    /// The market_trades channel sends market trades for a specified product on a preset interval.
    /// </remarks>
    public const string MarketTrades = "market_trades";

    /// <summary>
    /// Represents a notification about various subscription events in the system.
    /// </summary>
    /// <example>
    /// { ... "events":[{"subscriptions":{"heartbeats":["heartbeats"],"level2":["BTC-USD"],"market_trades":["BTC-USD"]}}]}
    /// </example>
    public const string Subscriptions = "subscriptions";

    /// <summary>
    /// Represents the channel information in a subscription event.
    /// </summary>
    public const string Channel = "channel";

    /// <summary>
    /// Represents a collection of WebSocket channels used for subscribing to real-time data for specific symbols.
    /// </summary>
    public readonly static ICollection<string> WebSocketChannelList = new string[] { Level2Request, MarketTrades };
}
