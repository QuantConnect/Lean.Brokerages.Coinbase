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

using Moq;
using System;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Tests.Brokerages;
using QuantConnect.CoinbaseBrokerage.Api;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.CoinbaseBrokerage.Tests
{
    [TestFixture]
    public partial class CoinbaseBrokerageTests : BrokerageTests
    {
        #region Properties
        protected override Symbol Symbol => Symbol.Create("BTCUSDC", SecurityType.Crypto, Market.GDAX);

        protected virtual ISymbolMapper SymbolMapper => new SymbolPropertiesDatabaseSymbolMapper(Market.GDAX);

        protected CoinbaseApi _api;

        /// <summary>
        ///     Gets the security type associated with the <see cref="BrokerageTests.Symbol" />
        /// </summary>
        protected override SecurityType SecurityType => SecurityType.Crypto;

        protected override decimal GetDefaultQuantity()
        {
            return 0.000016m;
        }
        #endregion

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            var securities = new SecurityManager(new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork))
            {
                {Symbol, CreateSecurity(Symbol)}
            };

            var transactions = new SecurityTransactionManager(null, securities);
            transactions.SetOrderProcessor(new FakeOrderProcessor());

            var algorithmSettings = new AlgorithmSettings();
            var algorithm = new Mock<IAlgorithm>();
            algorithm.Setup(a => a.Transactions).Returns(transactions);
            algorithm.Setup(a => a.BrokerageModel).Returns(new GDAXBrokerageModel());
            algorithm.Setup(a => a.Portfolio).Returns(new SecurityPortfolioManager(securities, transactions, algorithmSettings));
            algorithm.Setup(a => a.Securities).Returns(securities);

            var apiKey = Config.Get("coinbase-api-key");
            var apiSecret = Config.Get("coinbase-api-secret");
            var restApiUrl = Config.Get("coinbase-api-url", "https://api.coinbase.com");
            var webSocketUrl = Config.Get("coinbase-websocket-url", "wss://advanced-trade-ws.coinbase.com");

            _api = new CoinbaseApi(SymbolMapper, null, apiKey, apiSecret, restApiUrl);

            return new CoinbaseBrokerage(webSocketUrl, apiKey, apiSecret, restApiUrl, algorithm.Object, new AggregationManager(), null);
        }

        /// <summary>
        /// Returns wether or not the brokers order methods implementation are async
        /// </summary>
        protected override bool IsAsync()
        {
            return false;
        }

        protected override decimal GetAskPrice(Symbol symbol)
        {
            var brokerageSymbol = SymbolMapper.GetBrokerageSymbol(symbol);
            var tick = _api.GetMarketTrades(brokerageSymbol);
            return tick.BestAsk;
        }

        protected override void ModifyOrderUntilFilled(Order order, OrderTestParameters parameters, double secondsTimeout = 90)
        {
            Assert.Pass("Order update not supported");
        }

        [Test(Description = "Coinbase doesn't support margin trading")]
        public override void GetAccountHoldings()
        {
            Assert.That(Brokerage.GetAccountHoldings().Count == 0);
        }

        // stop market orders no longer supported (since 3/23/2019)
        // no stop limit support
        private static TestCaseData[] OrderParameters => new[]
        {
            new TestCaseData(new MarketOrderTestParameters(Symbol.Create("BTCUSDC", SecurityType.Crypto, Market.GDAX))),
            new TestCaseData(new LimitOrderTestParameters(Symbol.Create("BTCUSDC", SecurityType.Crypto, Market.GDAX), 305m, 300m)),
        };

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CancelOrders(OrderTestParameters parameters)
        {
            base.CancelOrders(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void LongFromZero(OrderTestParameters parameters)
        {
            base.LongFromZero(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CloseFromLong(OrderTestParameters parameters)
        {
            base.CloseFromLong(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void ShortFromZero(OrderTestParameters parameters)
        {
            base.ShortFromZero(parameters);
        }

        [Test, TestCaseSource(nameof(OrderParameters))]
        public override void CloseFromShort(OrderTestParameters parameters)
        {
            base.CloseFromShort(parameters);
        }

        [Explicit("Order modification not allowed")]
        public override void ShortFromLong(OrderTestParameters parameters)
        {
            base.ShortFromLong(parameters);
        }

        [Explicit("Order modification not allowed")]
        public override void LongFromShort(OrderTestParameters parameters)
        {
            base.LongFromShort(parameters);
        }

        [TestCase("BTCUSDC")]
        public void GetTick(string ticker)
        {
            var symbol = Symbol.Create(ticker, SecurityType.Crypto, Market.GDAX);
            var brokerageSymbol = SymbolMapper.GetBrokerageSymbol(symbol);

            var tick = _api.GetMarketTrades(brokerageSymbol);

            Assert.IsNotNull(tick);
            Assert.Greater(tick.BestAsk, 0);
            Assert.Greater(tick.BestBid, 0);
        }
    }
}