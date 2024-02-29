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
using QuantConnect.Brokerages.Coinbase.Models.Enums;

namespace QuantConnect.Brokerages.Coinbase.Models;

public readonly struct CoinbaseCreateOrderResponse
{
    /// <summary>
    /// Whether the order was created.
    /// </summary>
    [JsonProperty("success")]
    public bool Success { get; }

    /// <summary>
    /// Failure Reason
    /// </summary>
    [JsonProperty("failure_reason")]
    [JsonConverter(typeof(StringEnumConverter))]
    public FailureCreateOrderReason FailureReason { get; }

    /// <summary>
    /// The ID of the order created
    /// </summary>
    [JsonProperty("order_id")]
    public string OrderId { get; }

    /// <summary>
    /// If Success - true, get success response data otherwise <see cref="ErrorResponse"/>
    /// </summary>
    public SuccessResponse? SuccessResponse { get; }

    /// <summary>
    /// If Success - false, get error response otherwise <see cref="SuccessResponse"/>
    /// </summary>
    [JsonProperty("error_response")]
    public ErrorResponse? ErrorResponse { get; }

    /// <summary>
    /// Order Configuration
    /// </summary>
    [JsonProperty("order_configuration")]
    public OrderConfiguration OrderConfiguration { get; }

    [JsonConstructor]
    public CoinbaseCreateOrderResponse(bool success, FailureCreateOrderReason failureReason, string orderId,
        SuccessResponse? successResponse, ErrorResponse? errorResponse, OrderConfiguration orderConfiguration)
    {
        Success = success;
        FailureReason = failureReason;
        OrderId = orderId;
        SuccessResponse = successResponse;
        ErrorResponse = errorResponse;
        OrderConfiguration = orderConfiguration;
    }
}

/// <summary>
/// Error Response
/// </summary>
public readonly struct ErrorResponse
{
    /// <summary>
    /// Error
    /// </summary>
    [JsonProperty("error")]
    [JsonConverter(typeof(StringEnumConverter))]
    public FailureCreateOrderReason Error { get; }

    /// <summary>
    /// Generic error message explaining why the order was not created
    /// </summary>
    [JsonProperty("message")]
    public string Message { get; }

    /// <summary>
    /// Descriptive error message explaining why the order was not created
    /// </summary>
    [JsonProperty("error_details")]
    public string ErrorDetails { get; }

    /// <summary>
    /// Preview Failure Reason
    /// </summary>
    [JsonProperty("preview_failure_reason")]
    public string PreviewFailureReason { get; }

    [JsonConstructor]
    public ErrorResponse(FailureCreateOrderReason error, string message, string errorDetails, string previewFailureReason)
    {
        Error = error;
        Message = message;
        ErrorDetails = errorDetails;
        PreviewFailureReason = previewFailureReason;
    }
}

/// <summary>
/// Success Response
/// </summary>
public readonly struct SuccessResponse
{
    /// <summary>
    /// The ID of the order created
    /// </summary>
    [JsonProperty("order_id")]
    public string OrderId { get; }

    /// <summary>
    /// The product this order was created for e.g. 'BTC-USD'
    /// </summary>
    [JsonProperty("product_id")]
    public string ProductId { get; }

    /// <summary>
    /// Order Side
    /// </summary>
    [JsonProperty("side")]
    public OrderSide Side { get; }

    /// <summary>
    /// Client set unique uuid for this order
    /// </summary>
    [JsonProperty("client_order_id")]
    public string ClientOrderId { get; }

    [JsonConstructor]
    public SuccessResponse(string orderId, string productId, OrderSide side, string clientOrderId)
    {
        OrderId = orderId;
        ProductId = productId;
        Side = side;
        ClientOrderId = clientOrderId;
    }
}
