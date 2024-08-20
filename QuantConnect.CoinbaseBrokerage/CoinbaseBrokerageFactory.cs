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
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Coinbase
{
    /// <summary>
    /// Factory method to create Coinbase WebSockets brokerage
    /// </summary>
    public class CoinbaseBrokerageFactory : BrokerageFactory
    {
        /// <summary>
        /// Gets the brokerage data required to run the brokerage from configuration/disk
        /// </summary>
        /// <remarks>
        /// The implementation of this property will create the brokerage data dictionary required for
        /// running live jobs. See <see cref="IJobQueueHandler.NextJob"/>
        /// </remarks>
        public override Dictionary<string, string> BrokerageData => new Dictionary<string, string>
        {
            { "coinbase-api-name", Config.Get("coinbase-api-name")},
            { "coinbase-api-private-key", Config.Get("coinbase-api-private-key")},
            // Represents the configuration setting for the Coinbase API URL.
            { "coinbase-rest-api", Config.Get("coinbase-rest-api", "https://api.coinbase.com")},
            // Represents the configuration setting for the Coinbase WebSocket URL.
            { "coinbase-url" , Config.Get("coinbase-url", "wss://advanced-trade-ws.coinbase.com")},
            // load holdings if available
            { "live-holdings", Config.Get("live-holdings")},
        };


        /// <summary>
        /// Initializes a new instance of the <see cref="CoinbaseBrokerageFactory"/> class
        /// </summary>
        public CoinbaseBrokerageFactory() : base(typeof(CoinbaseBrokerage))
        { }

        /// <summary>
        /// Gets a brokerage model that can be used to model this brokerage's unique behaviors
        /// </summary>
        /// <param name="orderProvider">The order provider</param>
        public override IBrokerageModel GetBrokerageModel(IOrderProvider orderProvider) => new CoinbaseBrokerageModel();

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override IBrokerage CreateBrokerage(Packets.LiveNodePacket job, IAlgorithm algorithm)
        {
            var errors = new List<string>();
            var name = Read<string>(job.BrokerageData, "coinbase-api-name", errors);
            var privateKey = Read<string>(job.BrokerageData, "coinbase-api-private-key", errors);
            var apiUrl = Read<string>(job.BrokerageData, "coinbase-rest-api", errors);
            var wsUrl = Read<string>(job.BrokerageData, "coinbase-url", errors);

            if (errors.Count != 0)
            {
                // if we had errors then we can't create the instance
                throw new ArgumentException(string.Join(Environment.NewLine, errors));
            }

            var brokerage = new CoinbaseBrokerage(wsUrl, name, privateKey, apiUrl, algorithm, job);

            // Add the brokerage to the composer to ensure its accessible to the live data feed.
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        { }
    }
}
