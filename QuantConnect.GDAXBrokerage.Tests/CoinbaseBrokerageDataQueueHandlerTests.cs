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
using NUnit.Framework;
using System.Collections.Generic;
using QuantConnect.Brokerages.GDAX;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Data.Market;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Threading;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Brokerages.GDAX
{
    /// <summary>
    /// The class contains DataQueueHandler's tests
    /// </summary>
    [TestFixture]
    public partial class GDAXBrokerageIntegrationTests
    {
        private GDAXBrokerage _brokerage { get => (GDAXBrokerage)Brokerage; }

        private static readonly Symbol BTCUSDC = Symbol.Create("BTCUSDC", SecurityType.Crypto, Market.GDAX);

        private static IEnumerable<TestCaseData> TestParameters
        {
            get
            {
                yield return new TestCaseData(BTCUSDC, Resolution.Tick, false);
                yield return new TestCaseData(BTCUSDC, Resolution.Second, false);
                yield return new TestCaseData(BTCUSDC, Resolution.Minute, false);
            }
        }

        [Test, TestCaseSource(nameof(TestParameters))]
        public void StreamsData(Symbol symbol, Resolution resolution, bool throwsException)
        {
            var startTime = DateTime.UtcNow;
            var cancelationToken = new CancellationTokenSource();

            SubscriptionDataConfig[] configs;
            if (resolution == Resolution.Tick)
            {
                var tradeConfig = new SubscriptionDataConfig(GetSubscriptionDataConfig<Tick>(symbol, resolution),
                    tickType: TickType.Trade);
                var quoteConfig = new SubscriptionDataConfig(GetSubscriptionDataConfig<Tick>(symbol, resolution),
                    tickType: TickType.Quote);
                configs = new[] { tradeConfig, quoteConfig };
            }
            else
            {
                configs = new[]
                {
                    GetSubscriptionDataConfig<QuoteBar>(symbol, resolution),
                    GetSubscriptionDataConfig<TradeBar>(symbol, resolution)
                };
            }

            var trade = new ManualResetEvent(false);
            var quote = new ManualResetEvent(false);
            foreach (var config in configs)
            {
                ProcessFeed(_brokerage.Subscribe(config, (s, e) => { }),
                    cancelationToken,
                    (baseData) =>
                    {
                        if (baseData != null)
                        {
                            Assert.GreaterOrEqual(baseData.EndTime.Ticks, startTime.Ticks);

                            if ((baseData as Tick)?.TickType == TickType.Quote || baseData is QuoteBar)
                            {
                                quote.Set();
                            }
                            else if ((baseData as Tick)?.TickType == TickType.Trade || baseData is TradeBar)
                            {
                                trade.Set();
                            }
                            Log.Debug("");
                            Log.Trace($"Data received: {baseData}");
                        }
                    });
            }

            Assert.IsTrue(trade.WaitOne(resolution.ToTimeSpan() + TimeSpan.FromSeconds(30)));
            Assert.IsTrue(quote.WaitOne(resolution.ToTimeSpan() + TimeSpan.FromSeconds(30)));

            foreach (var config in configs)
            {
                _brokerage.Unsubscribe(config);
            }

            Thread.Sleep(2000);

            cancelationToken.Cancel();
        }
    }
}
