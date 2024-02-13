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
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace QuantConnect.CoinbaseBrokerage.Models;

public readonly struct CoinbaseProductCandles
{
    [JsonProperty("candles")]
    public IEnumerable<Candle> Candles { get; }

    [JsonConstructor]
    public CoinbaseProductCandles(IEnumerable<Candle> candles)
    {
        Candles = candles;
    }
}

public readonly struct Candle
{
    [JsonConverter(typeof(UnixDateTimeConverter))]
    [JsonProperty("start")]
    public DateTimeOffset Start { get; }

    [JsonProperty("low")]
    public decimal Low { get; }

    [JsonProperty("high")]
    public decimal High { get; }

    [JsonProperty("open")]
    public decimal Open { get; }

    [JsonProperty("close")]
    public decimal Close { get; }

    [JsonProperty("volume")]
    public decimal Volume { get; }

    [JsonConstructor]
    public Candle(DateTimeOffset start, decimal low, decimal high, decimal open, decimal close, decimal volume)
    {
        Start = start;
        Low = low;
        High = high;
        Open = open;
        Close = close;
        Volume = volume;
    }
}
