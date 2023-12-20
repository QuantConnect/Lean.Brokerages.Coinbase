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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.GDAX;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Tests.Brokerages.GDAX
{
    [TestFixture]
    public class GDAXBrokerageAdditionalTests
    {
        [Ignore("`user` channel sometimes doesn't subscribed in WebSocket.Open event")]
        [TestCase(5)]
        public void BrokerageConnectionAndReconnectionTest(int amountAttempt)
        {
            int counter = 0;
            var cancellationTokenSource = new CancellationTokenSource();
            using (var brokerage = GetBrokerage())  
            {
                var hasError = false;

                brokerage.Message += (_, brokerageMessageEvent) => {
                    Log.Debug("");
                    Log.Debug($"Brokerage:Error: {brokerageMessageEvent.Message}");
                    hasError = true; 
                };

                do
                {
                    Log.Debug("");
                    Log.Debug($"BrokerageConnectionAndReconnectionTest: connection attempt: #{counter}");
                    brokerage.Connect();
                    Assert.IsTrue(brokerage.IsConnected);

                    // cool down 
                    cancellationTokenSource.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));

                    Assert.IsFalse(hasError);

                    Log.Debug("");
                    Log.Debug($"BrokerageConnectionAndReconnectionTest: disconnect attempt: #{counter}");
                    brokerage.Disconnect();
                    Assert.IsFalse(brokerage.IsConnected);

                    // cool down 
                    cancellationTokenSource.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));

                } while (++counter < amountAttempt);
            }
        }

        [Test]
        public void DataQueueHandlerConnectsAndSubscribes()
        {
            var symbols = new[]
            {
                "LTCUSD", "LTCEUR", "LTCBTC",
                "BTCUSD", "BTCEUR", "BTCGBP",
                "ETHBTC", "ETHUSD", "ETHEUR",
                "BCHBTC", "BCHUSD", "BCHEUR",
                "XRPUSD", "XRPEUR", "XRPBTC",
                "EOSUSD", "EOSEUR", "EOSBTC",
                "XLMUSD", "XLMEUR", "XLMBTC",
                "ETCUSD", "ETCEUR", "ETCBTC",
                "ZRXUSD", "ZRXEUR", "ZRXBTC"
            }
            .Select(ticker => Symbol.Create(ticker, SecurityType.Crypto, Market.GDAX))
            .ToList();

            using (var dqh = GetBrokerage())
            {
                dqh.Connect();
                Assert.IsTrue(dqh.IsConnected);

                dqh.Subscribe(symbols);

                Thread.Sleep(5000);

                dqh.Unsubscribe(symbols);

                dqh.Disconnect();
                Assert.IsFalse(dqh.IsConnected);
            }
        }

        private static TestGDAXDataQueueHandler GetBrokerage()
        {
            var wssUrl = Config.Get("coinbase-websocket-url", "wss://advanced-trade-ws.coinbase.com");
            var apiKey = Config.Get("coinbase-api-key");
            var apiSecret = Config.Get("coinbase-api-secret");
            var restApiUrl = Config.Get("coinbase-api-url");
            var algorithm = new QCAlgorithm();
            var aggregator = new AggregationManager();

            return new TestGDAXDataQueueHandler(wssUrl, apiKey, apiSecret, restApiUrl, algorithm, aggregator, null);
        }

        private class TestGDAXDataQueueHandler : GDAXBrokerage
        {
            public TestGDAXDataQueueHandler(string wssUrl, string apiKey,
                string apiSecret,
                string restApiUrl,
                IAlgorithm algorithm,
                IDataAggregator aggregator,
                LiveNodePacket job
                )
                : base(wssUrl, apiKey, apiSecret, restApiUrl, algorithm, aggregator, job)
            {
            }

            public void Subscribe(IEnumerable<Symbol> symbols)
            {
                base.Subscribe(symbols);
            }
        }
    }
}
