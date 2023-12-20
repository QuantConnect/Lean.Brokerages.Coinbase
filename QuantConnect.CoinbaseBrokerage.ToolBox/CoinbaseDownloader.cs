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

using NodaTime;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Configuration;
using System.Collections.Generic;

namespace QuantConnect.CoinbaseBrokerage.ToolBox
{
    /// <summary>
    /// Coinbase Data Downloader class
    /// </summary>
    public class CoinbaseDownloader : IDataDownloader
    {
        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="dataDownloaderGetParameters">model class for passing in parameters for historical data</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
        {
            var symbol = dataDownloaderGetParameters.Symbol;
            var resolution = dataDownloaderGetParameters.Resolution;
            var startUtc = dataDownloaderGetParameters.StartUtc;
            var endUtc = dataDownloaderGetParameters.EndUtc;
            var tickType = dataDownloaderGetParameters.TickType;

            if (tickType != TickType.Trade)
            {
                return Enumerable.Empty<BaseData>();
            }

            var historyRequest = new HistoryRequest(
                startUtc,
                endUtc,
                resolution == Resolution.Tick ? typeof(Tick) : tickType == TickType.Trade ? typeof(TradeBar) : typeof(OpenInterest),
                symbol,
                resolution,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                DateTimeZone.Utc,
                resolution,
                false,
                false,
                DataNormalizationMode.Raw,
                tickType);

            var brokerage = CreateBrokerage();
            var data = brokerage.GetHistory(historyRequest);
            return data;
        }

        /// <summary>
        /// Creates and initializes a new instance of the <see cref="Brokerage"/> class for Coinbase integration.
        /// </summary>
        /// <remarks>
        /// This method retrieves necessary configuration values such as API key, API secret, and API URL from the application configuration.
        /// </remarks>
        /// <returns>
        /// A new instance of the <see cref="Brokerage"/> class configured for Coinbase integration.
        /// </returns>
        /// <seealso cref="Brokerage"/>
        /// <seealso cref="CoinbaseBrokerage"/>
        /// <example>
        /// <code>
        /// var coinbaseBrokerage = CreateBrokerage();
        /// // Use the coinbaseBrokerage instance for trading and other operations.
        /// </code>
        /// </example>
        private Brokerage CreateBrokerage()
        {
            var apiKey = Config.Get("coinbase-api-key", "");
            var apiSecret = Config.Get("coinbase-api-secret", "");
            var restApiUrl = Config.Get("coinbase-api-url", "https://api.coinbase.com");
            return new CoinbaseBrokerage(string.Empty, apiKey, apiSecret, restApiUrl, null, null, null);
        }
    }
}
