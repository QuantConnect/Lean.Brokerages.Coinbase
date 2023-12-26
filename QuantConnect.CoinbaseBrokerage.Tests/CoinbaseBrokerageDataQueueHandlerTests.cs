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
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace QuantConnect.CoinbaseBrokerage.Tests
{
    /// <summary>
    /// The class contains DataQueueHandler's tests
    /// </summary>
    [TestFixture]
    public partial class CoinbaseBrokerageTests
    {
        private CoinbaseBrokerage _brokerage { get => (CoinbaseBrokerage)Brokerage; }

        private static readonly Symbol BTCUSDC = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX);

        private static IEnumerable<TestCaseData> TestParameters
        {
            get
            {
                yield return new TestCaseData(BTCUSDC, Resolution.Tick);
                yield return new TestCaseData(BTCUSDC, Resolution.Second);
                yield return new TestCaseData(BTCUSDC, Resolution.Minute);
                yield return new TestCaseData(Symbol.Create("ETHUSD", SecurityType.Crypto, Market.GDAX), Resolution.Minute);
                yield return new TestCaseData(Symbol.Create("GRTUSD", SecurityType.Crypto, Market.GDAX), Resolution.Second);
            }
        }

        [Test, TestCaseSource(nameof(TestParameters))]
        public void StreamsData(Symbol symbol, Resolution resolution)
        {
            var startTime = DateTime.UtcNow;
            var cancelationToken = new CancellationTokenSource();
            var subscriptionEvent = new ManualResetEvent(false);

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

            _brokerage.Message += (_, brokerageMessageEvent) => {
                Log.Debug("");
                Log.Debug($"Brokerage:Error: {brokerageMessageEvent.Message}");
                subscriptionEvent.Set();
            };

            var trade = new ManualResetEvent(false);
            var quote = new ManualResetEvent(false);
            foreach (var config in configs)
            {
                ProcessFeed(_brokerage.Subscribe(config, (s, e) => { }),
                    cancelationToken,
                    (tick) =>
                    {
                        if (tick != null)
                        {
                            Assert.GreaterOrEqual(tick.EndTime.Ticks, startTime.Ticks);

                            Log.Debug("");

                            Assert.That(tick.Symbol, Is.EqualTo(config.Symbol));
                            Assert.NotZero(tick.Price);
                            Assert.IsTrue(tick.Price > 0, "Price was not greater then zero");
                            Assert.IsTrue(tick.Value > 0, "Value was not greater then zero");

                            if (tick is Tick)
                            {
                                Log.Debug($"Tick: {tick}");

                            }

                            if ((tick as Tick)?.TickType == TickType.Trade || tick is TradeBar)
                            {
                                Log.Debug($"TradeBar: {tick}");

                                if (resolution != Resolution.Tick)
                                {
                                    Assert.IsTrue(tick.DataType == MarketDataType.TradeBar);
                                }

                                trade.Set();
                            }

                            if ((tick as Tick)?.TickType == TickType.Quote || tick is QuoteBar)
                            {
                                Log.Debug($"QuoteBar: {tick}");
                                if (resolution != Resolution.Tick)
                                {
                                    Assert.IsTrue(tick.DataType == MarketDataType.QuoteBar);
                                }
                                quote.Set();
                            }
                        }
                    });
            }

            Assert.IsFalse(subscriptionEvent.WaitOne(TimeSpan.FromSeconds(35)));
            Assert.IsTrue(trade.WaitOne(resolution.ToTimeSpan() + TimeSpan.FromSeconds(30)));
            Assert.IsTrue(quote.WaitOne(resolution.ToTimeSpan() + TimeSpan.FromSeconds(30)));

            foreach (var config in configs)
            {
                _brokerage.Unsubscribe(config);
            }

            Thread.Sleep(2000);

            cancelationToken.Cancel();
        }

        [Test]
        public void SubscribeOnMultipleSymbols()
        {
            var configs = GetRandomSymbols();
            var cancelationToken = new CancellationTokenSource();
            var startTime = DateTime.UtcNow;
            var tickResetEvent = new ManualResetEvent(false);

            _brokerage.Message += (_, brokerageMessageEvent) => {
                Log.Debug("");
                Log.Debug($"Brokerage:Error: {brokerageMessageEvent.Message}");
                cancelationToken.Cancel();
            };

            var symbolTicks = new Dictionary<Symbol, bool>();
            foreach (var config in configs)
            {
                ProcessFeed(_brokerage.Subscribe(config, (s, e) => { }),
                    cancelationToken,
                    (tick) =>
                    {
                        if (tick != null)
                        {
                            Assert.GreaterOrEqual(tick.EndTime.Ticks, startTime.Ticks);

                            Assert.That(tick.Symbol, Is.EqualTo(config.Symbol));
                            Assert.NotZero(tick.Price);
                            Assert.IsTrue(tick.Price > 0, "Price was not greater then zero");
                            Assert.IsTrue(tick.Value > 0, "Value was not greater then zero");

                            if (!symbolTicks.TryGetValue(tick.Symbol, out var symbol))
                            {
                                symbolTicks[tick.Symbol] = true;
                            }

                            if (symbolTicks.Count == configs.Count / 2)
                            {
                                tickResetEvent.Set();
                            }
                        }
                    });
            }

            if (!tickResetEvent.WaitOne(TimeSpan.FromSeconds(120), cancelationToken.Token))
            {
                Assert.Fail("Reset event has not signaled or cancellationToken was canceled");
            }

            foreach (var config in configs)
            {
                _brokerage.Unsubscribe(config);
            }

            Thread.Sleep(2000);

            cancelationToken.Cancel();
        }

        /// <summary>
        /// Generates a list of random subscription configurations for liquid symbols.
        /// </summary>
        /// <returns>
        /// A list of <see cref="SubscriptionDataConfig"/> instances representing random subscription configurations.
        /// </returns>
        /// <remarks>
        /// This method creates a list of liquid symbols and generates random subscription configurations for each symbol.
        /// The generated configurations include various resolutions and data types such as TradeBars, QuoteBars, Trades, and Quotes.
        /// The resulting list of subscription configurations can be used to subscribe to real-time data streams for the specified symbols.
        /// </remarks>
        private static List<SubscriptionDataConfig> GetRandomSymbols()
        {
            var liquidSymbols = new string[10] { "SOLUSD", "BTCUSD", "ETHUSD", "XRPUSD", "ADAUSD", "AVAXUSD", "DOGEUSD", "DOTUSD", "LINKUSD", "MATICUSD" };

            var symbols = new List<SubscriptionDataConfig>();
            foreach (var ticker in liquidSymbols)
            {
                var symbol = Symbol.Create(ticker, SecurityType.Crypto, Market.Coinbase);

                var resolution = GetRandomResolution();
                if (resolution == Resolution.Tick)
                {
                    var tradeConfig = new SubscriptionDataConfig(GetSubscriptionDataConfig<Tick>(symbol, resolution),
                        tickType: TickType.Trade);
                    var quoteConfig = new SubscriptionDataConfig(GetSubscriptionDataConfig<Tick>(symbol, resolution),
                        tickType: TickType.Quote);

                    symbols.AddRange(new[] { tradeConfig, quoteConfig });
                }
                else
                {
                    symbols.AddRange(new[] { GetSubscriptionDataConfig<QuoteBar>(symbol, resolution), GetSubscriptionDataConfig<TradeBar>(symbol, resolution) });
                }
            }

            return symbols;
        }

        /// <summary>
        /// Generates and returns a random resolution from the available options.
        /// </summary>
        /// <returns>
        /// A <see cref="Resolution"/> value representing a randomly selected resolution,
        /// including Tick, Second, or Daily.
        /// </returns>
        /// <remarks>
        /// This method selects a random resolution from the available options: Tick, Second.
        /// It uses a secure random number generator to ensure randomness in the resolution selection.
        /// The generated resolution can be used in financial applications for specifying the time granularity
        /// of data subscriptions or time intervals.
        /// </remarks>
        private static Resolution GetRandomResolution()
        {
            Array values = new[] { Resolution.Tick, Resolution.Second };
            return (Resolution)values.GetValue(RandomNumberGenerator.GetInt32(values.Length));
        }
    }
}
