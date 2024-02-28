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
/// Time in force policies provide guarantees about the lifetime of an order.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum TimeInForce
{
    /// <summary>
    /// Unknown time in force orders
    /// </summary>
    [EnumMember(Value = "UNKNOWN_TIME_IN_FORCE")]
    UnknownTimeInForce = 0,

    /// <summary>
    /// Good until date orders are valid till a specified date or time (within a 90-day hard limit) unless 
    /// it has been already fulfilled or cancelled.
    /// </summary>
    [EnumMember(Value = "GOOD_UNTIL_DATE_TIME")]
    GoodUntilDateTime = 1,

    /// <summary>
    /// Good until canceled orders remain open on the book until canceled. 
    /// This is the default behavior if no policy is specified.
    /// </summary>
    [EnumMember(Value = "GOOD_UNTIL_CANCELLED")]
    GoodUntilCancelled = 2,

    /// <summary>
    /// Immediate or cancel orders instantly cancel the remaining size of 
    /// the limit order instead of opening it on the book.
    /// </summary>
    [EnumMember(Value = "IMMEDIATE_OR_CANCEL")]
    ImmediateOrCancel = 3,

    /// <summary>
    /// Fill or kill orders are rejected if the entire size cannot be matched.
    /// </summary>
    [EnumMember(Value = "FILL_OR_KILL")]
    FillOrKill = 4,
}