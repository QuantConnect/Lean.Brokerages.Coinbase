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

namespace QuantConnect.CoinbaseBrokerage.Models.Enums;

/// <summary>
/// Time in force policies provide guarantees about the lifetime of an order.
/// </summary>
public enum TimeInForce
{
    /// <summary>
    /// Unknown time in force orders
    /// </summary>
    UNKNOWN_TIME_IN_FORCE = 0,

    /// <summary>
    /// Good until date orders are valid till a specified date or time (within a 90-day hard limit) unless 
    /// it has been already fulfilled or cancelled.
    /// </summary>
    GOOD_UNTIL_DATE_TIME = 1,

    /// <summary>
    /// Good until canceled orders remain open on the book until canceled. 
    /// This is the default behavior if no policy is specified.
    /// </summary>
    GOOD_UNTIL_CANCELLED = 2,

    /// <summary>
    /// Immediate or cancel orders instantly cancel the remaining size of 
    /// the limit order instead of opening it on the book.
    /// </summary>
    IMMEDIATE_OR_CANCEL = 3,

    /// <summary>
    /// Fill or kill orders are rejected if the entire size cannot be matched.
    /// </summary>
    FILL_OR_KILL = 4,
}