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
/// Failure Create Order Reason
/// </summary>
public enum FailureCreateOrderReason
{
    UNKNOWN_FAILURE_REASON = 0,
    UNSUPPORTED_ORDER_CONFIGURATION = 1,
    INVALID_SIDE = 2,
    INVALID_PRODUCT_ID = 3,
    INVALID_SIZE_PRECISION = 4,
    INVALID_PRICE_PRECISION = 5,
    INSUFFICIENT_FUND = 6,
    INVALID_LEDGER_BALANCE = 7,
    ORDER_ENTRY_DISABLED = 8,
    INELIGIBLE_PAIR = 9,
    INVALID_LIMIT_PRICE_POST_ONLY = 10,
    INVALID_LIMIT_PRICE = 11,
    INVALID_NO_LIQUIDITY = 12,
    INVALID_REQUEST = 13,
    COMMANDER_REJECTED_NEW_ORDER = 14,
    INSUFFICIENT_FUNDS = 15
}
