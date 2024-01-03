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
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.CoinbaseBrokerage.Models.Enums;

namespace QuantConnect.CoinbaseBrokerage
{
    /// <summary>
    /// Coinbase Brokerage - IHistoryProvider implementation
    /// </summary>
    public partial class CoinbaseBrokerage
    {
        /// <summary>
        /// Prevent spam to external source
        /// </summary>
        private bool _loggedCoinbaseSupportsOnlyTradeBars = false;

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        public override IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            // Coinbase API only allows us to support history requests for TickType.Trade
            if (request.TickType != TickType.Trade)
            {
                if (!_loggedCoinbaseSupportsOnlyTradeBars)
                {
                    _loggedCoinbaseSupportsOnlyTradeBars = true;
                    _algorithm?.Debug($"Warning.{nameof(CoinbaseBrokerage)}: history provider only supports trade information, does not support quotes.");
                    Log.Error($"{nameof(CoinbaseBrokerage)}.{nameof(GetHistory)}(): only supports TradeBars");
                }
                yield break;
            }

            if (!_symbolMapper.IsKnownLeanSymbol(request.Symbol))
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidSymbol",
                    $"Unknown symbol: {request.Symbol.Value}, no history returned"));
                yield break;
            }

            if (request.Symbol.SecurityType != SecurityType.Crypto)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidSecurityType",
                    $"{request.Symbol.SecurityType} security type not supported, no history returned"));
                yield break;
            }

            if (request.StartTimeUtc >= request.EndTimeUtc)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidDateRange",
                    "The history request start date must precede the end date, no history returned"));
                yield break;
            }

            if (request.Resolution == Resolution.Tick || request.Resolution == Resolution.Second)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "InvalidResolution",
                    $"{request.Resolution} resolution not supported, no history returned"));
                yield break;
            }

            Log.Debug($"{nameof(CoinbaseBrokerage)}.{nameof(GetHistory)}: Submitting request: {request.Symbol.Value}: {request.Resolution} {request.StartTimeUtc} UTC -> {request.EndTimeUtc} UTC");

            foreach (var tradeBar in GetHistoryFromCandles(request))
            {
                yield return tradeBar;
            }
        }

        /// <summary>
        /// Returns TradeBars from Coinbase candles (only for Minute/Hour/Daily resolutions)
        /// </summary>
        /// <param name="request">The history request instance</param>
        private IEnumerable<TradeBar> GetHistoryFromCandles(HistoryRequest request)
        {
            var productId = _symbolMapper.GetBrokerageSymbol(request.Symbol);
            var resolutionTimeSpan = request.Resolution.ToTimeSpan();
            var granularityInSec = Convert.ToInt32(resolutionTimeSpan.TotalSeconds);

            var startTime = request.StartTimeUtc;
            var endTime = request.EndTimeUtc;
            var maximumRange = TimeSpan.FromSeconds(300 * granularityInSec);

            var granularity = request.Resolution switch
            {
                Resolution.Minute => CandleGranularity.OneMinute,
                Resolution.Hour => CandleGranularity.OneHour,
                Resolution.Daily => CandleGranularity.OneDay,
                _ => throw new NotSupportedException($"The resolution {request.Resolution} is not supported.")
            };

            do
            {
                var maximumEndTime = startTime.Add(maximumRange);
                if (endTime > maximumEndTime)
                {
                    endTime = maximumEndTime;
                }

                var candles = _coinbaseApi.GetProductCandles(productId, startTime, endTime, granularity);

                TradeBar lastTradeBar = null;
                foreach (var candle in candles)
                {
                    if (candle.Start.UtcDateTime < startTime)
                    {
                        // Note from Coinbase docs:
                        // If data points are readily available, your response may contain as many as 300 candles
                        // and some of those candles may precede your declared start value.
                        yield break;
                    }

                    var tradeBar = new TradeBar(
                        candle.Start.UtcDateTime,
                        request.Symbol,
                        candle.Open,
                        candle.High,
                        candle.Low,
                        candle.Close,
                        candle.Volume,
                        resolutionTimeSpan
                        );

                    lastTradeBar = tradeBar;
                    yield return tradeBar;
                }

                startTime = lastTradeBar?.EndTime ?? request.EndTimeUtc;
                endTime = request.EndTimeUtc;
            } while (startTime < request.EndTimeUtc);
        }
    }
}
