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

using QuantConnect.ToolBox;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Brokerages.Coinbase.Api;

namespace QuantConnect.Brokerages.Coinbase.ToolBox
{
    /// <summary>
    /// Coinbase implementation of <see cref="IExchangeInfoDownloader"/>
    /// </summary>
    public class CoinbaseExchangeInfoDownloader : IExchangeInfoDownloader
    {
        /// <summary>
        /// Market name
        /// </summary>
        public string Market => QuantConnect.Market.Coinbase;

        /// <summary>
        /// Security Type
        /// </summary>
        public string SecurityType => QuantConnect.SecurityType.Crypto.SecurityTypeToLower();

        /// <summary>
        /// Pulling data from a remote source
        /// </summary>
        /// <returns>Enumerable of exchange info</returns>
        public IEnumerable<string> Get()
        {
            var coinbaseApi = CreateCoinbaseApi();
            var products = coinbaseApi.GetProducts();

            foreach (var product in products)
            {
                var symbol = product.ProductId.Replace("-", string.Empty);
                var description = $"{product.BaseName}-{product.QuoteName}";
                var quoteCurrency = product.QuoteCurrencyId;
                var contractMultiplier = 1;
                var minimum_price_variation = product.QuoteIncrement;
                var lot_size = product.BaseIncrement;
                var marketTicker = product.ProductId;
                var minimum_order_size = product.BaseMinSize;

                // market,symbol,type,description,quote_currency,contract_multiplier,minimum_price_variation,lot_size,market_ticker,minimum_order_size
                yield return $"{Market},{symbol},{SecurityType},{description},{quoteCurrency},{contractMultiplier},{minimum_price_variation},{lot_size},{marketTicker},{minimum_order_size}";
            }
        }

        /// <summary>
        /// Creates and initializes a new instance of the <see cref="CoinbaseApi"/> class integration.
        /// </summary>
        /// <returns></returns>
        private CoinbaseApi CreateCoinbaseApi()
        {
            var apiKey = Config.Get("coinbase-api-key");
            var apiSecret = Config.Get("coinbase-api-secret");
            var restApiUrl = Config.Get("coinbase-rest-api", "https://api.coinbase.com");
            return new CoinbaseApi(null, null, apiKey, apiSecret, restApiUrl);
        }
    }
}
