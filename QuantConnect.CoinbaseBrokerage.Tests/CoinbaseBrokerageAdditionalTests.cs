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
using QuantConnect.Logging;
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Brokerages.Coinbase.Tests
{
    [TestFixture]
    public class CoinbaseBrokerageAdditionalTests
    {
        [Explicit("`user` channel sometimes doesn't subscribed in WebSocket.Open event")]
        [TestCase(5)]
        public void BrokerageConnectionAndReconnectionTest(int amountAttempt)
        {
            int counter = 0;
            var cancellationTokenSource = new CancellationTokenSource();
            var resetEvent = new AutoResetEvent(false);

            using (var brokerage = GetBrokerage())
            {
                brokerage.Message += (_, brokerageMessageEvent) =>
                {
                    Log.Debug("");
                    Log.Debug($"Brokerage:Error: {brokerageMessageEvent.Message}");
                    resetEvent.Set();
                };

                do
                {
                    Log.Debug("");
                    Log.Debug($"BrokerageConnectionAndReconnectionTest: connection attempt: #{counter}");
                    brokerage.Connect();
                    Assert.IsTrue(brokerage.IsConnected);

                    // cool down 
                    Assert.IsTrue(resetEvent.WaitOne(TimeSpan.FromSeconds(60), cancellationTokenSource.Token));

                    //Assert.IsFalse(hasError);

                    Log.Debug("");
                    Log.Debug($"BrokerageConnectionAndReconnectionTest: disconnect attempt: #{counter}");
                    brokerage.Disconnect();
                    Assert.IsFalse(brokerage.IsConnected);

                    // cool down 
                    resetEvent.WaitOne(TimeSpan.FromSeconds(10), cancellationTokenSource.Token);

                } while (++counter < amountAttempt);
            }
        }

        private static CoinbaseBrokerage GetBrokerage()
        {
            var wssUrl = Config.Get("coinbase-url", "wss://advanced-trade-ws.coinbase.com");
            var restApiUrl = Config.Get("coinbase-rest-api", "https://api.coinbase.com");
            var name = Config.Get("coinbase-api-key");
            var privateKey = Config.Get("coinbase-api-secret");
            var algorithm = new QCAlgorithm();
            var aggregator = new AggregationManager();

            return new CoinbaseBrokerage(wssUrl, name, privateKey, restApiUrl, algorithm, aggregator, null);
        }
    }
}
