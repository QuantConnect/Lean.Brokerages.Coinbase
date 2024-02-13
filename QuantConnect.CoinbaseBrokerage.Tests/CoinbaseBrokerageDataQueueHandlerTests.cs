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
using System.Linq;
using NUnit.Framework;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Logging;
using Microsoft.CodeAnalysis;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.CoinbaseBrokerage.Tests
{
    /// <summary>
    /// The class contains DataQueueHandler's tests
    /// </summary>
    [TestFixture]
    public partial class CoinbaseBrokerageTests
    {
        private CoinbaseBrokerage _brokerage { get => (CoinbaseBrokerage)Brokerage; }

        private static readonly Symbol BTCUSDC = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Coinbase);

        private static IEnumerable<TestCaseData> TestParameters
        {
            get
            {
                yield return new TestCaseData(BTCUSDC, Resolution.Tick);
                yield return new TestCaseData(BTCUSDC, Resolution.Second);
                yield return new TestCaseData(BTCUSDC, Resolution.Minute);
                yield return new TestCaseData(Symbol.Create("ETHUSD", SecurityType.Crypto, Market.Coinbase), Resolution.Minute);
                yield return new TestCaseData(Symbol.Create("GRTUSD", SecurityType.Crypto, Market.Coinbase), Resolution.Second);
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

            _brokerage.Message += (_, brokerageMessageEvent) =>
            {
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

        private static IEnumerable<TestCaseData> LiquidSymbolsSubscriptionConfigs
        {
            get
            {
                var liquidSymbols = new (string, Resolution)[10]
                {
                    ("SOLUSD",Resolution.Tick),
                    ("BTCUSD", Resolution.Second),
                    ("ETHUSD", Resolution.Tick),
                    ("XRPUSD", Resolution.Second),
                    ("ADAUSD", Resolution.Tick),
                    ("AVAXUSD", Resolution.Second),
                    ("DOGEUSD", Resolution.Tick),
                    ("DOTUSD", Resolution.Second),
                    ("LINKUSD", Resolution.Tick),
                    ("MATICUSD", Resolution.Second)
                };

                var symbols = new List<SubscriptionDataConfig>();
                foreach (var (ticker, resolution) in liquidSymbols)
                {
                    var symbol = Symbol.Create(ticker, SecurityType.Crypto, Market.Coinbase);

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

                yield return new TestCaseData(symbols);
            }
        }

        [Test, TestCaseSource(nameof(LiquidSymbolsSubscriptionConfigs))]
        public void SubscribeOnMultipleSymbols(List<SubscriptionDataConfig> liquidSymbolsSubscriptionConfigs)
        {
            var cancelationToken = new CancellationTokenSource();
            var startTime = DateTime.UtcNow;
            var tickResetEvent = new ManualResetEvent(false);

            _brokerage.Message += (_, brokerageMessageEvent) =>
            {
                Log.Debug("");
                Log.Debug($"Brokerage:Error: {brokerageMessageEvent.Message}");
                cancelationToken.Cancel();
            };

            var symbolTicks = new Dictionary<Symbol, bool>();
            foreach (var config in liquidSymbolsSubscriptionConfigs)
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

                            if (symbolTicks.Count == liquidSymbolsSubscriptionConfigs.Count / 2)
                            {
                                tickResetEvent.Set();
                            }
                        }
                    });
            }

            if (!tickResetEvent.WaitOne(TimeSpan.FromSeconds(180), cancelationToken.Token))
            {
                Assert.Fail("Reset event has not signaled or cancellationToken was canceled");
            }

            foreach (var config in liquidSymbolsSubscriptionConfigs)
            {
                _brokerage.Unsubscribe(config);
            }

            Thread.Sleep(2000);

            cancelationToken.Cancel();
        }

        private static IEnumerable<IEnumerable<Symbol>> BitcoinTradingPairs
        {
            get
            {
                yield return new List<Symbol>
                {
                    Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Coinbase),
                    Symbol.Create("BTCUSDC", SecurityType.Crypto, Market.Coinbase),
                    Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Coinbase)
                };
            }
        }

        [TestCaseSource(nameof(BitcoinTradingPairs))]
        public void SubscribeOnDifferentUSDTickers(List<Symbol> symbols)
        {
            var resetEvent = new ManualResetEvent(false);
            var cancelationTokenSource = new CancellationTokenSource();

            var configs = new List<SubscriptionDataConfig>();

            var dataReceivedForType = new Dictionary<(Type, Symbol), int>();

            foreach (var symbol in symbols)
            {
                configs.Add(GetSubscriptionDataConfig<QuoteBar>(symbol, Resolution.Second));
                configs.Add(GetSubscriptionDataConfig<TradeBar>(symbol, Resolution.Second));

                dataReceivedForType.Add((typeof(QuoteBar), symbol), 0);
                dataReceivedForType.Add((typeof(TradeBar), symbol), 0);
            }

            foreach (var config in configs)
            {
                ProcessFeed(_brokerage.Subscribe(config, (s, e) => { }),
                cancelationTokenSource,
                (tick) =>
                {
                    if (tick != null)
                    {
                        Log.Debug($"Tick: {tick}");

                        if (tick is TradeBar tb)
                        {
                            dataReceivedForType[(tb.GetType(), tb.Symbol)] += 1;
                        }

                        if (tick is QuoteBar qb)
                        {
                            dataReceivedForType[(qb.GetType(), qb.Symbol)] += 1;
                        }

                        if (dataReceivedForType.Values.All(x => x > 0))
                        {
                            resetEvent.Set();
                        }
                    }
                });
            }

            Assert.IsTrue(resetEvent.WaitOne(TimeSpan.FromSeconds(30), cancelationTokenSource.Token));

            foreach (var config in configs)
            {
                _brokerage.Unsubscribe(config);
            }

            Thread.Sleep(2000);

            cancelationTokenSource.Cancel();
        }
    }
}
