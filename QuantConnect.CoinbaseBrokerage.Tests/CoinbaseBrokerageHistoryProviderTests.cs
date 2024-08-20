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
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Brokerages.Coinbase.Tests
{
    [TestFixture]
    public class CoinbaseBrokerageHistoryProviderTests
    {
        [Test, TestCaseSource(nameof(TestParameters))]
        public void GetsHistory(Symbol symbol, Resolution resolution, TickType tickType, TimeSpan period, bool unsupported)
        {
            var brokerage = new CoinbaseBrokerage(
                Config.Get("coinbase-url", "wss://advanced-trade-ws.coinbase.com"),
                Config.Get("coinbase-api-name"),
                Config.Get("coinbase-api-private-key"),
                Config.Get("coinbase-rest-api", "https://api.coinbase.com"),
                null,
                new AggregationManager(),
                null);

            var now = DateTime.UtcNow;
            var request = new HistoryRequest(now.Add(-period),
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
                tickType);

            var history = brokerage.GetHistory(request)?.ToList();

            if (unsupported)
            {
                Assert.IsNull(history);
                return;
            }

            Assert.IsNotNull(history);
            Assert.IsNotEmpty(history);

            foreach (var bar in history.Cast<TradeBar>())
            {
                Log.Trace($"{bar.Time}: {bar.Symbol} - O={bar.Open}, H={bar.High}, L={bar.Low}, C={bar.Close}, V={bar.Volume}");
            }

            Log.Trace("Data points retrieved: " + history.Count);
        }

        private static IEnumerable<TestCaseData> TestParameters
        {
            get
            {
                TestGlobals.Initialize();
                var BTCUSD = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Coinbase);
                var BTCUSDC = Symbol.Create("BTCUSDC", SecurityType.Crypto, Market.Coinbase);

                // valid parameters
                yield return new TestCaseData(BTCUSD, Resolution.Minute, TickType.Trade, TimeSpan.FromDays(5), false);
                yield return new TestCaseData(BTCUSD, Resolution.Minute, TickType.Trade, Time.OneHour, false);
                yield return new TestCaseData(BTCUSD, Resolution.Hour, TickType.Trade, Time.OneDay, false);
                yield return new TestCaseData(BTCUSD, Resolution.Daily, TickType.Trade, TimeSpan.FromDays(15), false);

                yield return new TestCaseData(BTCUSDC, Resolution.Minute, TickType.Trade, Time.OneHour, false);
                yield return new TestCaseData(BTCUSDC, Resolution.Hour, TickType.Trade, Time.OneDay, false);

                // invalid period
                yield return new TestCaseData(BTCUSD, Resolution.Daily, TickType.Trade, TimeSpan.FromDays(-15), true);

                // quote tick type, null result
                yield return new TestCaseData(BTCUSD, Resolution.Daily, TickType.Quote, TimeSpan.FromDays(15), true);
                yield return new TestCaseData(BTCUSD, Resolution.Daily, TickType.OpenInterest, TimeSpan.FromDays(15), true);

                // invalid resolution, null result
                yield return new TestCaseData(BTCUSD, Resolution.Tick, TickType.Trade, TimeSpan.FromSeconds(15), true);
                yield return new TestCaseData(BTCUSD, Resolution.Second, TickType.Trade, Time.OneMinute, true);

                // invalid symbol, null result
                yield return new TestCaseData(Symbol.Create("ABCXYZ", SecurityType.Crypto, Market.Coinbase), Resolution.Daily, TickType.Trade, TimeSpan.FromDays(15), true);

                // invalid security type, null result
                yield return new TestCaseData(Symbols.EURGBP, Resolution.Daily, TickType.Trade, TimeSpan.FromDays(15), true);

                // invalid market, null result
                yield return new TestCaseData(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Binance), Resolution.Daily, TickType.Trade, TimeSpan.FromDays(15), true);
            }
        }
    }
}
