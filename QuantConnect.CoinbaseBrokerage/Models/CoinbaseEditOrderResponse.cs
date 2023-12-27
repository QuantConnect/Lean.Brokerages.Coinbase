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

namespace QuantConnect.CoinbaseBrokerage.Models;

public readonly struct CoinbaseEditOrderResponse
{
    /// <summary>
    /// Whether the order edit request was placed.
    /// </summary>
    [JsonProperty("success")]
    public bool Success { get; }

    [JsonProperty("errors")]
    public Error[] Errors { get; }

    [JsonConstructor]
    public CoinbaseEditOrderResponse(bool success, Error[] errors)
    {
        Success = success;
        Errors = errors;
    }
}

public readonly struct Error
{
    [JsonProperty("edit_failure_reason")]
    public string EditFailureReason { get; }

    [JsonProperty("preview_failure_reason")]
    public string PreviewFailureReason { get; }

    [JsonConstructor]
    public Error(string editFailureReason, string previewFailureReason)
    {
        EditFailureReason = editFailureReason;
        PreviewFailureReason = previewFailureReason;
    }
}
