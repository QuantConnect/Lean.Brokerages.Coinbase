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
using Newtonsoft.Json.Converters;
using QuantConnect.CoinbaseBrokerage.Models.Enums;

namespace QuantConnect.CoinbaseBrokerage.Models.WebSocket;

public class CoinbaseLevel2Event : WebSocketEvent
{
    [JsonProperty("product_id")]
    public string ProductId { get; set; }

    [JsonProperty("updates")]
    public List<Update> Updates { get; set; }
}

public class Update
{
    [JsonProperty("side")]
    [JsonConverter(typeof(StringEnumConverter))]
    public CoinbaseLevel2UpdateSide Side { get; set; }

    [JsonProperty("price_level")]
    public decimal? PriceLevel { get; set; }

    [JsonProperty("new_quantity")]
    public decimal? NewQuantity { get; set; }
}
