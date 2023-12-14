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
using QuantConnect.CoinbaseBrokerage.Models.Enums;

namespace QuantConnect.CoinbaseBrokerage.Models.WebSocket;

public class CoinbaseUserEvent : WebSocketEvent
{
    [JsonProperty("orders")]
    public List<CoinbaseOrder> Orders { get; set; }
}

public class CoinbaseOrder
{
    [JsonProperty("order_id")]
    public string OrderId { get; set; }

    [JsonProperty("client_order_id")]
    public string ClientOrderId { get; set; }

    [JsonProperty("cumulative_quantity")]
    public decimal? CumulativeQuantity { get; set; }

    [JsonProperty("leaves_quantity")]
    public decimal? LeavesQuantity { get; set; }

    [JsonProperty("avg_price")]
    public decimal? AveragePrice { get; set; }

    [JsonProperty("total_fees")]
    public decimal? TotalFees { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("product_id")]
    public string ProductId { get; set; }

    [JsonProperty("creation_time")]
    public DateTimeOffset CreationTime { get; set; }

    [JsonProperty("order_side")]
    public OrderSide OrderSide { get; set; }

    [JsonProperty("order_type")]
    public string OrderType { get; set; }
}
