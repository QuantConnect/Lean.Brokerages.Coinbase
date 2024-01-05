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

namespace QuantConnect.CoinbaseBrokerage.Models;

public readonly struct CoinbaseCancelOrdersResponse
{
    [JsonProperty("results")]
    public IEnumerable<CoinbaseCancelOrderResult> Result { get; }

    [JsonConstructor]
    public CoinbaseCancelOrdersResponse(IEnumerable<CoinbaseCancelOrderResult> result) => Result = result;
}


public readonly struct CoinbaseCancelOrderResult
{
    /// <summary>
    /// Whether the cancel request was submitted successfully.
    /// </summary>
    [JsonProperty("success")]
    public bool Success { get; }

    /// <summary>
    /// Failure Reason
    /// </summary>
    [JsonProperty("failure_reason")]
    public string FailureReason { get; }

    /// <summary>
    /// The IDs of order cancel request was initiated for
    /// </summary>
    [JsonProperty("order_id")]
    public string OrderId { get; }

    [JsonConstructor]
    public CoinbaseCancelOrderResult(bool success, string failureReason, string orderId)
    {
        Success = success;
        FailureReason = failureReason;
        OrderId = orderId;
    }
}