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
/// Failure Cancel Order Reason
/// </summary>
public enum FailureCancelOrderReason
{
    UNKNOWN_CANCEL_FAILURE_REASON = 0,
    INVALID_CANCEL_REQUEST = 1,
    UNKNOWN_CANCEL_ORDER = 2,
    COMMANDER_REJECTED_CANCEL_ORDER = 3,
    DUPLICATE_CANCEL_REQUEST = 4
}