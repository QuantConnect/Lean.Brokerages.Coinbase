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
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace QuantConnect.Brokerages.Coinbase.Models.Enums;

/// <summary>
/// Coinbase available order status
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum OrderStatus
{
    /// <summary>
    /// Unknown order status
    /// </summary>
    [EnumMember(Value = "UNKNOWN_ORDER_STATUS")]
    UnknownOrderStatus,

    /// <summary>
    /// Order is not yet open
    /// </summary>
    [EnumMember(Value = "PENDING")]
    Pending,

    /// <summary>
    /// Order is waiting to be fully filled
    /// </summary>
    [EnumMember(Value = "OPEN")]
    Open,

    /// <summary>
    /// Order is 100% filled
    /// </summary>
    [EnumMember(Value = "FILLED")]
    Filled,

    /// <summary>
    /// Order was cancelled by user or system
    /// </summary>
    [EnumMember(Value = "CANCELLED")]
    Cancelled,

    /// <summary>
    /// TWAP(Time-weighted average price) order was not filled by the expiry time
    /// </summary>
    [EnumMember(Value = "EXPIRED")]
    Expired,

    /// <summary>
    /// Order cannot be placed at all
    /// </summary>
    [EnumMember(Value = "FAILED")]
    Failed,
}
