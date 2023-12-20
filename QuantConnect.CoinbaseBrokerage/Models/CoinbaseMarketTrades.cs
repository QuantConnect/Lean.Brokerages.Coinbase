﻿/*
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

namespace QuantConnect.CoinbaseBrokerage.Models;
public readonly struct CoinbaseMarketTrades
{
    [JsonProperty("trades")]
    public IEnumerable<Trades> Trades { get; }

    [JsonProperty("best_bid")]
    public decimal BestBid { get; }

    [JsonProperty("best_ask")]
    public decimal BestAsk { get; }

    [JsonConstructor]
    public CoinbaseMarketTrades(IEnumerable<Trades> trades, decimal bestBid, decimal bestAsk)
    {
        Trades = trades;
        BestBid = bestBid;
        BestAsk = bestAsk;
    }
}

public readonly struct Trades
{
    [JsonProperty("trade_id")]
    public string TradeId { get; }

    [JsonProperty("product_id")]
    public string ProductId { get; }

    [JsonProperty("price")]
    public decimal Price { get; }

    [JsonProperty("size")]
    public decimal Size { get; }

    [JsonProperty("time")]
    public DateTime Time { get; }

    [JsonProperty("side")]
    [JsonConverter(typeof(StringEnumConverter))]
    public OrderSide Side { get; }

    [JsonProperty("bid")]
    public decimal? Bid { get; }

    [JsonProperty("ask")]
    public decimal? Ask { get; }

    [JsonConstructor]
    public Trades(string tradeId, string productId, decimal price, decimal size, DateTime time, OrderSide side,
        decimal? bid, decimal? ask)
    {
        TradeId = tradeId;
        ProductId = productId;
        Price = price;
        Size = size;
        Time = time;
        Side = side;
        Bid = bid;
        Ask = ask;
    }
}
