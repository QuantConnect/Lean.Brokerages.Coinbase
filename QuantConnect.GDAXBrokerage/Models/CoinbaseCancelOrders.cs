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

namespace QuantConnect.GDAX.Models;

public readonly struct CoinbaseCancelOrders
{
    [JsonProperty("results")]
    public CoinbaseCancelOrder[] Result { get; }

    [JsonConstructor]
    public CoinbaseCancelOrders(CoinbaseCancelOrder[] result) => Result = result;
}


public readonly struct CoinbaseCancelOrder
{
    /// <summary>
    /// Whether the cancel request was submitted successfully.
    /// </summary>
    [JsonProperty("success")]
    public bool Success { get; }

    /// <summary>
    /// Possible values: [UNKNOWN_CANCEL_FAILURE_REASON, INVALID_CANCEL_REQUEST, UNKNOWN_CANCEL_ORDER, 
    /// COMMANDER_REJECTED_CANCEL_ORDER, DUPLICATE_CANCEL_REQUEST]
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    [JsonProperty("failure_reason")]
    public FailureReason FailureReason { get; }

    /// <summary>
    /// The IDs of order cancel request was initiated for
    /// </summary>
    [JsonProperty("order_id")]
    public string OrderId { get; }

    [JsonConstructor]
    public CoinbaseCancelOrder(bool success, FailureReason failureReason, string orderId)
    {
        Success = success;
        FailureReason = failureReason;
        OrderId = orderId;
    }
}

public enum FailureReason
{
    UNKNOWN_CANCEL_FAILURE_REASON = 0,
    INVALID_CANCEL_REQUEST = 1,
    UNKNOWN_CANCEL_ORDER = 2,
    COMMANDER_REJECTED_CANCEL_ORDER = 3,
    DUPLICATE_CANCEL_REQUEST = 4
}