/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2023 QuantConnect Corporation.
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
using RestSharp;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using QuantConnect.Api;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Orders;
using Newtonsoft.Json.Linq;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Brokerages;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Orders.Fees;
using System.Collections.Generic;
using QuantConnect.Configuration;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using QuantConnect.CoinbaseBrokerage.Api;
using BrokerageEnums = QuantConnect.CoinbaseBrokerage.Models.Enums;

namespace QuantConnect.CoinbaseBrokerage
{
    /// <summary>
    /// Represents a partial class for interacting with the Coinbase brokerage using WebSocket communication.
    /// </summary>
    [BrokerageFactory(typeof(CoinbaseBrokerageFactory))]
    public partial class CoinbaseBrokerage : BaseWebsocketsBrokerage
    {
        /// <summary>
        /// Live job task packet: container for any live specific job variables
        /// </summary>
        private LiveNodePacket _job;

        /// <summary>
        /// Provide data from external algorithm
        /// </summary>
        private IAlgorithm _algorithm;

        /// <summary>
        /// Represents an instance of the Coinbase API.
        /// </summary>
        private CoinbaseApi _coinbaseApi;

        /// <summary>
        ///  Provides the mapping between Lean symbols and brokerage symbols
        /// </summary>
        private SymbolPropertiesDatabaseSymbolMapper _symbolMapper;

        /// <summary>
        /// Represents the name of the market associated with the application.
        /// </summary>
        private static readonly string MarketName = Market.GDAX;

        /// <summary>
        /// Checks if the WebSocket connection is connected or in the process of connecting
        /// </summary>
        public override bool IsConnected => WebSocket.IsOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoinbaseBrokerage"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name associated with the Coinbase brokerage instance.</param>
        public CoinbaseBrokerage(string name) : base(name)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoinbaseBrokerage"/> class with set of parameters.
        /// </summary>
        /// <param name="webSocketUrl">WebSockets url</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="restApiUrl">api secret</param>
        /// <param name="algorithm">the algorithm instance is required to retreive account type</param>
        /// <param name="aggregator">consolidate ticks</param>
        /// <param name="job">The live job packet</param>
        public CoinbaseBrokerage(string webSocketUrl, string apiKey, string apiSecret, string restApiUrl,
            IAlgorithm algorithm, IDataAggregator aggregator, LiveNodePacket job)
            : base(MarketName)
        {
            Initialize(
                webSocketUrl: webSocketUrl,
                apiKey: apiKey,
                apiSecret: apiSecret,
                restApiUrl: restApiUrl,
                algorithm: algorithm,
                aggregator: aggregator,
                job: job
            );
        }

        /// <summary>
        /// Initialize the instance of this class
        /// </summary>
        /// <param name="webSocketUrl">The web socket base url</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="aggregator">the aggregator for consolidating ticks</param>
        /// <param name="job">The live job packet</param>
        protected void Initialize(string webSocketUrl, string apiKey, string apiSecret, string restApiUrl,
            IAlgorithm algorithm, IDataAggregator aggregator, LiveNodePacket job)
        {
            if (IsInitialized)
            {
                return;
            }

            Initialize(webSocketUrl, new WebSocketClientWrapper(), null, apiKey, apiSecret);

            WebSocket.Open += SubscribeOnWebSocketFeed;

            _job = job;
            _algorithm = algorithm;
            _aggregator = aggregator;
            _symbolMapper = new SymbolPropertiesDatabaseSymbolMapper(MarketName);
            _coinbaseApi = new CoinbaseApi(_symbolMapper, algorithm?.Portfolio, apiKey, apiSecret, restApiUrl);

            FillSplit = new ConcurrentDictionary<long, GDAXFill>();

            SubscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager()
            {
                SubscribeImpl = (symbols, _) => Subscribe(symbols),
                UnsubscribeImpl = (symbols, _) => Unsubscribe(symbols)
            };

            // ValidateSubscription();
        }

        #region IBrokerage
        /// <summary>
        /// Creates a new order
        /// </summary>
        /// <param name="order">Lean Order</param>
        /// <returns>true - order placed successfully otherwise false</returns>
        public override bool PlaceOrder(Order order)
        {
            var response = _coinbaseApi.CreateOrder(order);

            if (!response.Success)
            {
                var errorMessage =
                    response.ErrorResponse.Value.Error == BrokerageEnums.FailureCreateOrderReason.UnknownFailureReason
                    ? response.ErrorResponse.Value.PreviewFailureReason : response.ErrorResponse.Value.Error.ToString();
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "CoinbaseBrokerage Order Event")
                { Status = OrderStatus.Invalid, Message = errorMessage });
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Warning, "PlaceOrderInvalid", errorMessage));
                return false;
            }

            //TODO: Why did we use this ?
            //if (CachedOrderIDs.ContainsKey(order.Id))
            //{
            //    CachedOrderIDs[order.Id].BrokerId.Add(response.OrderId);
            //}
            //else
            //{
            //    order.BrokerId.Add(response.OrderId);
            //    CachedOrderIDs.TryAdd(order.Id, order);
            //}

            //// Add fill splits in all cases; we'll need to handle market fills too.
            //FillSplit.TryAdd(order.Id, new GDAXFill(order));

            //_pendingOrders.TryAdd(response.OrderId, new PendingOrder(order));

            order.BrokerId.Add(response.OrderId);

            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero,
                "CoinbaseBrokerage Order Event")
            { Status = OrderStatus.Submitted });

            return true;
        }

        /// <summary>
        /// This operation is not supported
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool UpdateOrder(Order order)
        {
            // Is temporarily unavailable, Status: https://status.coinbase.com/incidents/7y4cpt0r2dcd
            throw new NotSupportedException($"{nameof(CoinbaseBrokerage)}:{nameof(UpdateOrder)}: Order update not supported. Please cancel and re-create.");
        }

        /// <summary>
        /// Cancels an order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool CancelOrder(Order order)
        {
            var cancelOrder = _coinbaseApi.CancelOrders(order.BrokerId);

            if (!cancelOrder.Success)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, "CancelOrder",
                    $"Coinbase has not canceled order, error: {cancelOrder.FailureReason}"));
                return false;
            }

            // TODO: Why did we use this ?
            _pendingOrders.TryRemove(cancelOrder.OrderId, out _);

            OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, OrderFee.Zero, "Coinbase Order Event")
            { Status = OrderStatus.Canceled });

            return true;
        }

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public override void Connect()
        {
            base.Connect();
        }

        /// <summary>
        /// Closes the websockets connection
        /// </summary>
        public override void Disconnect()
        {
            WebSocket.Close();
        }

        /// <summary>
        /// Gets all orders not yet closed
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {
            var list = new List<Order>();

            var openOrders = _coinbaseApi.GetListOrders(BrokerageEnums.OrderStatus.Open);

            foreach (var order in openOrders)
            {
                Order leanOrder = default;

                var symbol = _symbolMapper.GetLeanSymbol(order.ProductId, SecurityType.Crypto, MarketName);

                if (order.OrderConfiguration.MarketIoc != null)
                {
                    var quantity = order.Side == BrokerageEnums.OrderSide.Buy ?
                        order.OrderConfiguration.MarketIoc.QuoteSize : Decimal.Negate(order.OrderConfiguration.MarketIoc.BaseSize);
                    leanOrder = new MarketOrder(symbol, quantity, order.CreatedTime, order.AverageFilledPrice);
                }
                else if (order.OrderConfiguration.LimitGtc != null)
                {
                    var quantity = order.Side == BrokerageEnums.OrderSide.Buy ? order.OrderConfiguration.LimitGtc.BaseSize : Decimal.Negate(order.OrderConfiguration.LimitGtc.BaseSize);
                    leanOrder = new LimitOrder(symbol, quantity, order.OrderConfiguration.LimitGtc.LimitPrice, order.CreatedTime);
                }
                else if (order.OrderConfiguration.LimitGtd != null)
                {
                    var quantity = order.Side == BrokerageEnums.OrderSide.Buy ? order.OrderConfiguration.LimitGtd.BaseSize : Decimal.Negate(order.OrderConfiguration.LimitGtd.BaseSize);
                    leanOrder = new LimitOrder(symbol, quantity, order.OrderConfiguration.LimitGtd.LimitPrice, order.CreatedTime);
                    leanOrder.Properties.TimeInForce = ConvertTimeInForce(order.TimeInForce, order.OrderConfiguration.LimitGtd.EndTime);
                }
                else if (order.OrderConfiguration.LimitIoc != null)
                {
                    var quantity = order.Side == BrokerageEnums.OrderSide.Buy ? order.OrderConfiguration.LimitIoc.BaseSize : Decimal.Negate(order.OrderConfiguration.LimitIoc.BaseSize);
                    leanOrder = new LimitOrder(symbol, quantity, order.OrderConfiguration.LimitIoc.LimitPrice, order.CreatedTime);
                }
                else if (order.OrderConfiguration.StopLimitGtc != null)
                {
                    var quantity = order.Side == BrokerageEnums.OrderSide.Buy ? order.OrderConfiguration.StopLimitGtc.BaseSize : Decimal.Negate(order.OrderConfiguration.StopLimitGtc.BaseSize);
                    leanOrder = new StopLimitOrder(symbol, quantity, order.OrderConfiguration.StopLimitGtc.StopPrice, order.OrderConfiguration.StopLimitGtc.LimitPrice, order.CreatedTime);
                }
                else if (order.OrderConfiguration.StopLimitGtd != null)
                {
                    var quantity = order.Side == BrokerageEnums.OrderSide.Buy ? order.OrderConfiguration.StopLimitGtd.BaseSize : Decimal.Negate(order.OrderConfiguration.StopLimitGtd.BaseSize);
                    leanOrder = new StopLimitOrder(symbol, quantity, order.OrderConfiguration.StopLimitGtd.StopPrice, order.OrderConfiguration.StopLimitGtd.LimitPrice, order.CreatedTime);
                    leanOrder.Properties.TimeInForce = ConvertTimeInForce(order.TimeInForce, order.OrderConfiguration.StopLimitGtd.EndTime);
                }

                leanOrder.Status = ConvertOrderStatus(order);
                leanOrder.BrokerId.Add(order.OrderId);

                list.Add(leanOrder);
            }

            // TODO: Why did we use this ?
            foreach (var item in list)
            {
                if (item.Status.IsOpen())
                {
                    var cached = CachedOrderIDs.Where(c => c.Value.BrokerId.Contains(item.BrokerId.First()));
                    if (cached.Any())
                    {
                        CachedOrderIDs[cached.First().Key] = item;
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Gets all open positions
        /// </summary>
        /// <returns></returns>
        public override List<Holding> GetAccountHoldings()
        {
            /*
             * On launching the algorithm the cash balances are pulled and stored in the cashbook.
             * Try loading pre-existing currency swaps from the job packet if provided
             */
            return base.GetAccountHoldings(_job?.BrokerageData, _algorithm?.Securities.Values);
        }

        /// <summary>
        /// Gets the total account cash balance
        /// </summary>
        /// <returns></returns>
        public override List<CashAmount> GetCashBalance()
        {
            var list = new List<CashAmount>();

            var accounts = _coinbaseApi.GetListAccounts();

            foreach (var item in accounts)
            {
                if (item.AvailableBalance.Value > 0m)
                {
                    list.Add(new CashAmount(item.AvailableBalance.Value, item.AvailableBalance.Currency));
                }
            }

            return list;
        }

        /// <summary>
        /// Checks if this brokerage supports the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>returns true if brokerage supports the specified symbol; otherwise false</returns>
        protected virtual bool CanSubscribe(Symbol symbol)
        {
            return !symbol.Value.Contains("UNIVERSE") &&
                symbol.SecurityType == SecurityType.Crypto &&
                symbol.ID.Market == MarketName;
        }

        #endregion

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            _aggregator.DisposeSafely();

            _webSocketRateLimit.DisposeSafely();
        }

        #region Utils

        private TimeInForce ConvertTimeInForce(BrokerageEnums.TimeInForce timeInForce, DateTime expiryDate = default)
        {
            switch (timeInForce)
            {
                case BrokerageEnums.TimeInForce.GoodUntilDateTime:
                    return TimeInForce.GoodTilDate(expiryDate);
                case BrokerageEnums.TimeInForce.GoodUntilCancelled:
                default:
                    return TimeInForce.GoodTilCanceled;
            }
        }

        #endregion

        private class ModulesReadLicenseRead : QuantConnect.Api.RestResponse
        {
            [JsonProperty(PropertyName = "license")]
            public string License;
            [JsonProperty(PropertyName = "organizationId")]
            public string OrganizationId;
        }

        /// <summary>
        /// Validate the user of this project has permission to be using it via our web API.
        /// </summary>
        private static void ValidateSubscription()
        {
            try
            {
                var productId = 183;
                var userId = Config.GetInt("job-user-id");
                var token = Config.Get("api-access-token");
                var organizationId = Config.Get("job-organization-id", null);
                // Verify we can authenticate with this user and token
                var api = new ApiConnection(userId, token);
                if (!api.Connected)
                {
                    throw new ArgumentException("Invalid api user id or token, cannot authenticate subscription.");
                }
                // Compile the information we want to send when validating
                var information = new Dictionary<string, object>()
                {
                    {"productId", productId},
                    {"machineName", Environment.MachineName},
                    {"userName", Environment.UserName},
                    {"domainName", Environment.UserDomainName},
                    {"os", Environment.OSVersion}
                };
                // IP and Mac Address Information
                try
                {
                    var interfaceDictionary = new List<Dictionary<string, object>>();
                    foreach (var nic in NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.OperationalStatus == OperationalStatus.Up))
                    {
                        var interfaceInformation = new Dictionary<string, object>();
                        // Get UnicastAddresses
                        var addresses = nic.GetIPProperties().UnicastAddresses
                            .Select(uniAddress => uniAddress.Address)
                            .Where(address => !IPAddress.IsLoopback(address)).Select(x => x.ToString());
                        // If this interface has non-loopback addresses, we will include it
                        if (!addresses.IsNullOrEmpty())
                        {
                            interfaceInformation.Add("unicastAddresses", addresses);
                            // Get MAC address
                            interfaceInformation.Add("MAC", nic.GetPhysicalAddress().ToString());
                            // Add Interface name
                            interfaceInformation.Add("name", nic.Name);
                            // Add these to our dictionary
                            interfaceDictionary.Add(interfaceInformation);
                        }
                    }
                    information.Add("networkInterfaces", interfaceDictionary);
                }
                catch (Exception)
                {
                    // NOP, not necessary to crash if fails to extract and add this information
                }
                // Include our OrganizationId is specified
                if (!string.IsNullOrEmpty(organizationId))
                {
                    information.Add("organizationId", organizationId);
                }
                var request = new RestRequest("modules/license/read", Method.POST) { RequestFormat = DataFormat.Json };
                request.AddParameter("application/json", JsonConvert.SerializeObject(information), ParameterType.RequestBody);
                api.TryRequest(request, out ModulesReadLicenseRead result);
                if (!result.Success)
                {
                    throw new InvalidOperationException($"Request for subscriptions from web failed, Response Errors : {string.Join(',', result.Errors)}");
                }

                var encryptedData = result.License;
                // Decrypt the data we received
                DateTime? expirationDate = null;
                long? stamp = null;
                bool? isValid = null;
                if (encryptedData != null)
                {
                    // Fetch the org id from the response if we are null, we need it to generate our validation key
                    if (string.IsNullOrEmpty(organizationId))
                    {
                        organizationId = result.OrganizationId;
                    }
                    // Create our combination key
                    var password = $"{token}-{organizationId}";
                    var key = SHA256.HashData(Encoding.UTF8.GetBytes(password));
                    // Split the data
                    var info = encryptedData.Split("::");
                    var buffer = Convert.FromBase64String(info[0]);
                    var iv = Convert.FromBase64String(info[1]);
                    // Decrypt our information
                    using var aes = new AesManaged();
                    var decryptor = aes.CreateDecryptor(key, iv);
                    using var memoryStream = new MemoryStream(buffer);
                    using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                    using var streamReader = new StreamReader(cryptoStream);
                    var decryptedData = streamReader.ReadToEnd();
                    if (!decryptedData.IsNullOrEmpty())
                    {
                        var jsonInfo = JsonConvert.DeserializeObject<JObject>(decryptedData);
                        expirationDate = jsonInfo["expiration"]?.Value<DateTime>();
                        isValid = jsonInfo["isValid"]?.Value<bool>();
                        stamp = jsonInfo["stamped"]?.Value<int>();
                    }
                }
                // Validate our conditions
                if (!expirationDate.HasValue || !isValid.HasValue || !stamp.HasValue)
                {
                    throw new InvalidOperationException("Failed to validate subscription.");
                }

                var nowUtc = DateTime.UtcNow;
                var timeSpan = nowUtc - Time.UnixTimeStampToDateTime(stamp.Value);
                if (timeSpan > TimeSpan.FromHours(12))
                {
                    throw new InvalidOperationException("Invalid API response.");
                }
                if (!isValid.Value)
                {
                    throw new ArgumentException($"Your subscription is not valid, please check your product subscriptions on our website.");
                }
                if (expirationDate < nowUtc)
                {
                    throw new ArgumentException($"Your subscription expired {expirationDate}, please renew in order to use this product.");
                }
            }
            catch (Exception e)
            {
                Log.Error($"ValidateSubscription(): Failed during validation, shutting down. Error : {e.Message}");
                System.Environment.Exit(1);
            }
        }
    }
}
