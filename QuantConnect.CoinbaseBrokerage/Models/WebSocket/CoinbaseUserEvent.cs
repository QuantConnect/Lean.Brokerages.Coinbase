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

using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Brokerages.Coinbase.Models.Enums;

namespace QuantConnect.Brokerages.Coinbase.Models.WebSocket;

public class CoinbaseUserEvent : WebSocketEvent
{
    [JsonProperty("orders")]
    public List<CoinbaseWebSocketOrderResponse> Orders { get; set; }
}

/// <summary>
/// Represents a response from the Coinbase WebSocket for order-related information.
/// </summary>
public class CoinbaseWebSocketOrderResponse
{
    /// <summary>
    /// Unique identifier of order
    /// </summary>
    [JsonProperty("order_id")]
    public string OrderId { get; set; }

    /// <summary>
    /// Unique identifier of order specified by client
    /// </summary>
    [JsonProperty("client_order_id")]
    public string ClientOrderId { get; set; }

    /// <summary>
    /// Amount the order is filled, in base currency
    /// </summary>
    [JsonProperty("cumulative_quantity")]
    public decimal? CumulativeQuantity { get; set; }

    /// <summary>
    /// Amount remaining, in same currency as order was placed in (quote or base)
    /// </summary>
    [JsonProperty("leaves_quantity")]
    public decimal? LeavesQuantity { get; set; }

    /// <summary>
    /// Average filled price of the order so far
    /// </summary>
    [JsonProperty("avg_price")]
    public decimal? AveragePrice { get; set; }

    /// <summary>
    /// Commission paid for the order
    /// </summary>
    [JsonProperty("total_fees")]
    public decimal? TotalFees { get; set; }

    /// <summary>
    /// Order Status
    /// </summary>
    [JsonProperty("status")]
    public OrderStatus Status { get; set; }

    /// <summary>
    /// The product ID for which this order was placed
    /// </summary>
    [JsonProperty("product_id")]
    public string ProductId { get; set; }

    /// <summary>
    /// When the order was placed
    /// </summary>
    [JsonProperty("creation_time")]
    public DateTimeOffset CreationTime { get; set; }

    /// <summary>
    /// Can be one of: BUY, SELL
    /// </summary>
    [JsonProperty("order_side")]
    public OrderSide OrderSide { get; set; }

    /// <summary>
    /// Can be one of: Limit, Market, Stop Limit
    /// </summary>
    [JsonProperty("order_type")]
    public string OrderType { get; set; }

    /// <summary>
    /// Cancel Reason
    /// </summary>
    /// <example>
    /// "User requested cancel"
    /// </example>
    [JsonProperty("cancel_reason")]
    public string CancelReason { get; set; }

    /// <summary>
    /// Reject Reason
    /// </summary>
    [JsonProperty("reject_Reason")]
    public string RejectReason { get; set; }
}
