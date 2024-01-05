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

namespace QuantConnect.CoinbaseBrokerage.Models.Enums;

/// <summary>
/// Failure Create Order Reason
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum FailureCreateOrderReason
{
    [EnumMember(Value = "UNKNOWN_FAILURE_REASON")]
    UnknownFailureReason = 0,

    [EnumMember(Value = "UNSUPPORTED_ORDER_CONFIGURATION")]
    UnsupportedOrderConfiguration = 1,

    [EnumMember(Value = "INVALID_SIDE")]
    InvalidSide = 2,

    [EnumMember(Value = "INVALID_PRODUCT_ID")]
    InvalidProductId = 3,

    [EnumMember(Value = "INVALID_SIZE_PRECISION")]
    InvalidSizePrecision = 4,

    [EnumMember(Value = "INVALID_PRICE_PRECISION")]
    InvalidPricePrecision = 5,

    [EnumMember(Value = "INSUFFICIENT_FUND")]
    InsufficientFund = 6,

    [EnumMember(Value = "INVALID_LEDGER_BALANCE")]
    InvalidLedgerBalance = 7,

    [EnumMember(Value = "ORDER_ENTRY_DISABLED")]
    OrderEntryDisabled = 8,

    [EnumMember(Value = "INELIGIBLE_PAIR")]
    IneligiblePair = 9,

    [EnumMember(Value = "INVALID_LIMIT_PRICE_POST_ONLY")]
    InvalidLimitPricePostOnly = 10,

    [EnumMember(Value = "INVALID_LIMIT_PRICE")]
    InvalidLimitPrice = 11,

    [EnumMember(Value = "INVALID_NO_LIQUIDITY")]
    InvalidNoLiquidity = 12,

    [EnumMember(Value = "INVALID_REQUEST")]
    InvalidRequest = 13,

    [EnumMember(Value = "COMMANDER_REJECTED_NEW_ORDER")]
    CommanderRejectedNewOrder = 14,

    [EnumMember(Value = "INSUFFICIENT_FUNDS")]
    InsufficientFunds = 15
}
