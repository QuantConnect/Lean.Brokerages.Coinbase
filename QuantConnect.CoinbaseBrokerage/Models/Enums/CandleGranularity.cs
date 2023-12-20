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
/// Represents the granularity of candlestick data for financial charting.
/// </summary>
public enum CandleGranularity
{
    /// <summary>
    /// Unknown granularity.
    /// </summary>
    UNKNOWN_GRANULARITY = 0,

    /// <summary>
    /// Granularity representing one-minute intervals.
    /// </summary>
    ONE_MINUTE = 1,

    /// <summary>
    /// Granularity representing one-hour intervals.
    /// </summary>
    ONE_HOUR = 2,

    /// <summary>
    /// Granularity representing one-day intervals.
    /// </summary>
    ONE_DAY = 3,
}