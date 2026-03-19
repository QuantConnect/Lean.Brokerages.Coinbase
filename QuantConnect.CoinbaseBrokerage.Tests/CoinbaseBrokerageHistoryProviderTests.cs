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

namespace QuantConnect.Brokerages.Coinbase.Tests
{
    [TestFixture]
    public class CoinbaseBrokerageHistoryProviderTests
    {
        [Test, TestCaseSource(nameof(TestParameters))]
        public void GetsHistory(Symbol symbol, Resolution resolution, TickType tickType, DateTime startUtc, DateTime endUtc, bool unsupported)
        {
            var brokerage = new CoinbaseBrokerage(
                Config.Get("coinbase-url", "wss://advanced-trade-ws.coinbase.com"),
                Config.Get("coinbase-api-name"),
                Config.Get("coinbase-api-private-key"),
                Config.Get("coinbase-rest-api", "https://api.coinbase.com"),
                null,
                null);

            var request = new HistoryRequest(startUtc,
                endUtc,
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
                var refDate = new DateTime(2025, 1, 1);
                var BTCUSD = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Coinbase);
                var BTCUSDC = Symbol.Create("BTCUSDC", SecurityType.Crypto, Market.Coinbase);
                var ETHUSD = Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Coinbase);

                // valid parameters
                yield return new TestCaseData(BTCUSD, Resolution.Minute, TickType.Trade, refDate.AddDays(-5), refDate, false);
                yield return new TestCaseData(BTCUSD, Resolution.Minute, TickType.Trade, refDate.Add(-Time.OneHour), refDate, false);
                yield return new TestCaseData(BTCUSD, Resolution.Hour, TickType.Trade, refDate.Add(-Time.OneDay), refDate, false);
                yield return new TestCaseData(BTCUSD, Resolution.Daily, TickType.Trade, refDate.AddDays(-15), refDate, false);

                yield return new TestCaseData(BTCUSDC, Resolution.Minute, TickType.Trade, refDate.Add(-Time.OneHour), refDate, false);
                yield return new TestCaseData(BTCUSDC, Resolution.Hour, TickType.Trade, refDate.Add(-Time.OneDay), refDate, false);

                // start before ETH listing (May 18 2016), listing falls within the first 300-day batch
                yield return new TestCaseData(ETHUSD, Resolution.Daily, TickType.Trade, new DateTime(2016, 1, 1), new DateTime(2017, 1, 1), false);

                // start just before the 300 day window that includes the listing: first batch returns empty, second batch finds data
                yield return new TestCaseData(ETHUSD, Resolution.Daily, TickType.Trade, new DateTime(2015, 7, 20), new DateTime(2025, 9, 1), false);

                // start well before listing, should return data from the listing date onwards
                yield return new TestCaseData(ETHUSD, Resolution.Daily, TickType.Trade, new DateTime(2010, 1, 1), new DateTime(2017, 1, 1), false);

                // invalid period
                yield return new TestCaseData(BTCUSD, Resolution.Daily, TickType.Trade, refDate, refDate.AddDays(-15), true);

                // quote tick type, null result
                yield return new TestCaseData(BTCUSD, Resolution.Daily, TickType.Quote, refDate.AddDays(-15), refDate, true);
                yield return new TestCaseData(BTCUSD, Resolution.Daily, TickType.OpenInterest, refDate.AddDays(-15), refDate, true);

                // invalid resolution, null result
                yield return new TestCaseData(BTCUSD, Resolution.Tick, TickType.Trade, refDate.AddSeconds(-15), refDate, true);
                yield return new TestCaseData(BTCUSD, Resolution.Second, TickType.Trade, refDate.Add(-Time.OneMinute), refDate, true);

                // invalid symbol, null result
                yield return new TestCaseData(Symbol.Create("ABCXYZ", SecurityType.Crypto, Market.Coinbase), Resolution.Daily, TickType.Trade, refDate.AddDays(-15), refDate, true);

                // invalid security type, null result
                yield return new TestCaseData(Symbols.EURGBP, Resolution.Daily, TickType.Trade, refDate.AddDays(-15), refDate, true);

                // invalid market, null result
                yield return new TestCaseData(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Binance), Resolution.Daily, TickType.Trade, refDate.AddDays(-15), refDate, true);
            }
        }
    }
}
