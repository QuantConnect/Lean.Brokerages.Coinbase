/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
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
using NodaTime;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Tests;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;

namespace QuantConnect.CoinbaseBrokerage.Tests
{
    [TestFixture]
    public class CoinbaseBrokerageHistoryProviderTests
    {
        [Test, TestCaseSource(nameof(TestParameters))]
        public void GetsHistory(Symbol symbol, Resolution resolution, TickType tickType, TimeSpan period, bool shouldBeEmpty)
        {
            var aggregator = new AggregationManager();

            var brokerage = new CoinbaseBrokerage(
                Config.Get("coinbase-websocket-url", "wss://advanced-trade-ws.coinbase.com"),
                Config.Get("coinbase-api-key"), Config.Get("coinbase-api-secret"), Config.Get("coinbase-api-url"), null, aggregator, null);

            var historyProvider = new BrokerageHistoryProvider();
            historyProvider.SetBrokerage(brokerage);
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, new DataPermissionManager(), null));

            var now = DateTime.UtcNow;

            var requests = new[]
            {
                new HistoryRequest(now.Add(-period),
                    now,
                    typeof(TradeBar),
                    symbol,
                    resolution,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                    DateTimeZone.Utc,
                    resolution,
                    false,
                    false,
                    DataNormalizationMode.Adjusted,
                    tickType)
            };

            var history = historyProvider.GetHistory(requests, TimeZones.Utc).ToList();

            foreach (var slice in history)
            {
                var bar = slice.Bars[symbol];

                Log.Trace($"{bar.Time}: {bar.Symbol} - O={bar.Open}, H={bar.High}, L={bar.Low}, C={bar.Close}, V={bar.Volume}");
            }

            if (shouldBeEmpty)
            {
                Assert.IsTrue(history.Count == 0);
            }
            else
            {
                Assert.IsTrue(history.Count > 0);
            }

            Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);
        }

        private static TestCaseData[] TestParameters()
        {
            TestGlobals.Initialize();
            var btcusd = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX);

            return new[]
            {
                // valid parameters
                new TestCaseData(btcusd, Resolution.Minute, TickType.Trade, TimeSpan.FromDays(5), false),
                new TestCaseData(btcusd, Resolution.Minute, TickType.Trade, Time.OneHour, false),
                new TestCaseData(btcusd, Resolution.Hour, TickType.Trade, Time.OneDay, false),
                new TestCaseData(btcusd, Resolution.Daily, TickType.Trade, TimeSpan.FromDays(15), false),

                // quote tick type, no error, empty result
                new TestCaseData(btcusd, Resolution.Daily, TickType.Quote, TimeSpan.FromDays(15), true),

                // invalid resolution, no error, empty result
                new TestCaseData(btcusd, Resolution.Tick, TickType.Trade, TimeSpan.FromSeconds(15), true),
                new TestCaseData(btcusd, Resolution.Second, TickType.Trade, Time.OneMinute, true),

                // invalid period, no error, empty result
                new TestCaseData(btcusd, Resolution.Daily, TickType.Trade, TimeSpan.FromDays(-15), true),

                // invalid symbol, no error, empty result
                new TestCaseData(Symbol.Create("ABCXYZ", SecurityType.Crypto, Market.GDAX), Resolution.Daily, TickType.Trade, TimeSpan.FromDays(15), true),

                // invalid security type, no error, empty result
                new TestCaseData(Symbols.EURGBP, Resolution.Daily, TickType.Trade, TimeSpan.FromDays(15), true)
            };
        }
    }
}