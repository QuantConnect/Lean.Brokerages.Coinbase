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

namespace QuantConnect.CoinbaseBrokerage.Models.Enums;

/// <summary>
/// Represents the side of an order, indicating whether it is a buy or sell order.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum OrderSide
{
    /// <summary>
    /// Indicates a buy order.
    /// </summary>
    BUY = 0,

    /// <summary>
    /// Indicates a sell order.
    /// </summary>
    SELL = 1,
}

