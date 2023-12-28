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
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Util;
using QuantConnect.Orders;
using System.Globalization;
using QuantConnect.Brokerages;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.CoinbaseBrokerage.Models;
using QuantConnect.CoinbaseBrokerage.Converters;
using QuantConnect.CoinbaseBrokerage.Models.Enums;
using QuantConnect.CoinbaseBrokerage.Models.Requests;
using BrokerageEnums = QuantConnect.CoinbaseBrokerage.Models.Enums;

namespace QuantConnect.CoinbaseBrokerage.Api;

public class CoinbaseApi : IDisposable
{
    /// <summary>
    /// Represents the maximum number of occurrences allowed per unit of time for a gate limit.
    /// </summary>
    /// <remarks>
    /// Refer to the documentation for more details: 
    /// <see href="https://docs.cloud.coinbase.com/advanced-trade-api/docs/rest-api-rate-limits"/>.
    /// </remarks>
    private const int maxGateLimitOccurrences = 30;

    /// <summary>
    /// Represents an instance of the Coinbase API client used for communication with the Coinbase API.
    /// </summary>
    private readonly CoinbaseApiClient _apiClient;

    /// <summary>
    /// Represents the prefix used for API endpoints in the application.
    /// </summary>
    private readonly string _apiPrefix = "/api/v3";

    private JsonSerializerSettings _jsonSerializerSettings = new()
    {
        Converters = new List<JsonConverter>() { new CoinbaseDecimalStringConverter() },
        NullValueHandling = NullValueHandling.Ignore
    };

    /// <summary>
    /// Symbol mapper
    /// </summary>
    private ISymbolMapper SymbolMapper { get; }

    /// <summary>
    /// Security provider
    /// </summary>
    private ISecurityProvider SecurityProvider { get; }

    public CoinbaseApi(ISymbolMapper symbolMapper, ISecurityProvider securityProvider,
        string apiKey, string apiKeySecret, string restApiUrl)
    {
        SymbolMapper = symbolMapper;
        SecurityProvider = securityProvider;
        _apiClient = new CoinbaseApiClient(apiKey, apiKeySecret, restApiUrl, maxGateLimitOccurrences);
    }

    /// <summary>
    /// Generates WebSocket signatures for authentication.
    /// </summary>
    /// <param name="channel">The WebSocket channel for which the signature is generated.</param>
    /// <param name="productIds">A collection of product identifiers for which the signature is generated.</param>
    /// <returns>
    /// A tuple containing the API key, timestamp, and signature required for WebSocket authentication.
    /// </returns>
    /// <remarks>
    /// The <paramref name="channel"/> <see cref="Models.Constants.CoinbaseWebSocketChannels"/> parameter specifies the WebSocket channel, 
    /// and <paramref name="productIds"/> contains a collection of product identifiers for which the authentication signature is generated.
    /// </remarks>
    /// <example>
    /// This example demonstrates how to use the GetWebSocketSignatures method:
    /// <code>
    /// var (apiKey, timestamp, signature) = GetWebSocketSignatures("trades", new List<string> { "BTC-USD", "ETH-USD" });
    /// </code>
    /// </example>
    public (string apiKey, string timestamp, string signature) GetWebSocketSignatures(string channel, ICollection<string> productIds)
    {
        return _apiClient.GenerateWebSocketSignature(channel, productIds);
    }

    /// <summary>
    /// Retrieves a list of Coinbase accounts associated with the authenticated user's brokerage.
    /// </summary>
    /// <returns>An IEnumerable of CoinbaseAccount objects representing the user's brokerage accounts.</returns>
    public IEnumerable<CoinbaseAccount> GetAccounts()
    {
        var request = new RestRequest($"{_apiPrefix}/brokerage/accounts", Method.GET);

        var response = _apiClient.ExecuteRequest(request);

        return JsonConvert.DeserializeObject<CoinbaseAccountResponse>(response.Content).Accounts;
    }

    /// <summary>
    /// Retrieves a collection of historical Coinbase orders based on the specified order status.
    /// </summary>
    /// <param name="orderStatus">The status of the orders to retrieve.</param>
    /// <returns>
    /// An IEnumerable of CoinbaseOrder representing historical orders matching the specified order status.
    /// </returns>
    /// <remarks>
    /// The method constructs a request to the Coinbase API for retrieving historical orders.
    /// The optional <paramref name="orderStatus"/> parameter allows filtering orders based on their status.
    /// If the <paramref name="orderStatus"/> is set to <see cref="BrokerageEnums.OrderStatus.UnknownOrderStatus"/>,
    /// all historical orders, regardless of their status, will be retrieved.
    /// </remarks>
    public IEnumerable<CoinbaseOrder> GetOrders(BrokerageEnums.OrderStatus orderStatus)
    {
        var request = new RestRequest($"{_apiPrefix}/brokerage/orders/historical/batch", Method.GET);

        if (orderStatus != BrokerageEnums.OrderStatus.UnknownOrderStatus)
        {
            request.AddQueryParameter("order_status", orderStatus.ToStringInvariant().ToUpperInvariant());
        }

        var response = _apiClient.ExecuteRequest(request);

        return JsonConvert.DeserializeObject<CoinbaseOrderResponse>(response.Content).Orders;
    }

    /// <summary>
    /// Cancels multiple Coinbase orders identified by their broker IDs.
    /// </summary>
    /// <param name="brokerIds">A list of broker IDs representing the orders to be canceled.</param>
    /// <returns>
    /// A CoinbaseCancelOrderResult representing the result of the cancellation operation.
    /// </returns>
    /// <remarks>
    /// The method constructs a request to the Coinbase API for canceling multiple orders in batch.
    /// The <paramref name="brokerIds"/> parameter contains a list of broker IDs that uniquely identify
    /// the orders to be canceled. The method returns a result representing the outcome of the cancellation operation.
    /// </remarks>
    public CoinbaseCancelOrderResult CancelOrders(List<string> brokerIds)
    {
        var request = new RestRequest($"{_apiPrefix}/brokerage/orders/batch_cancel", Method.POST);

        request.AddJsonBody(JsonConvert.SerializeObject(new { order_ids = brokerIds }));

        var response = _apiClient.ExecuteRequest(request);

        // It always returns result, even if we have sent invalid orderId
        // The Coinbase doesn't support combo orders as a result we return First cancel order response
        return JsonConvert.DeserializeObject<CoinbaseCancelOrdersResponse>(response.Content).Result.First();
    }

    /// <summary>
    /// Get snapshot information, by product ID, about the last trades (ticks), best bid/ask, and 24h volume.
    /// </summary>
    /// <param name="productId">The trading pair, i.e., 'BTC-USD'.</param>
    /// <param name="limit">Number of trades to return. Correct Range between [1:1000] </param>
    /// <returns>An instance of the <see cref="CoinbaseMarketTrades"/> struct.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided productId is null or empty.</exception>
    public CoinbaseMarketTrades GetMarketTrades(string productId, int limit = 1)
    {
        if (string.IsNullOrEmpty(productId))
        {
            throw new ArgumentException($"{nameof(CoinbaseApi)}.{nameof(GetMarketTrades)}: productId is null or empty");
        }

        if (limit > 1000)
        {
            throw new ArgumentException($"{nameof(CoinbaseApi)}.{nameof(GetMarketTrades)}: Please provide a limit equal to or below 1000.");
        }

        var request = new RestRequest($"{_apiPrefix}/brokerage/products/{productId}/ticker", Method.GET);

        request.AddQueryParameter("limit", limit.ToStringInvariant());

        var response = _apiClient.ExecuteRequest(request);

        return JsonConvert.DeserializeObject<CoinbaseMarketTrades>(response.Content);
    }

    /// <summary>
    /// Get rates for a single product by product ID, grouped in buckets.
    /// </summary>
    /// <param name="productId">The trading pair, i.e., 'BTC-USD'.</param>
    /// <param name="start">Timestamp for starting range of aggregations, in UNIX time.</param>
    /// <param name="end">Timestamp for ending range of aggregations, in UNIX time.</param>
    /// <param name="granularity">The time slice value for each candle. <see cref="CandleGranularity"/></param>
    /// <returns>An enumerable <see cref="Candle"/> collection.</returns>
    public IEnumerable<Candle> GetProductCandles(string productId, DateTime start, DateTime end, CandleGranularity granularity)
    {
        var request = new RestRequest($"{_apiPrefix}/brokerage/products/{productId}/candles", Method.GET);

        request.AddQueryParameter("start", Time.DateTimeToUnixTimeStamp(start).ToString("F0", CultureInfo.InvariantCulture));
        request.AddQueryParameter("end", Time.DateTimeToUnixTimeStamp(end).ToString("F0", CultureInfo.InvariantCulture));
        request.AddQueryParameter("granularity", JsonConvert.SerializeObject(granularity).Replace("\"", string.Empty));

        var response = _apiClient.ExecuteRequest(request);

        // returns data backwards should use Reverse
        return JsonConvert.DeserializeObject<CoinbaseProductCandles>(response.Content).Candles.Reverse();
    }

    /// <summary>
    /// Edits an existing limit order on Coinbase brokerage.
    /// </summary>
    /// <param name="leanOrder">The limit order to be edited.</param>
    /// <returns>A response containing information about the edited order.</returns>
    public CoinbaseEditOrderResponse EditOrder(LimitOrder leanOrder)
    {
        var request = new RestRequest($"{_apiPrefix}/brokerage/orders/edit", Method.POST);

        request.AddJsonBody(JsonConvert.SerializeObject(
            new CoinbaseEditOrderRequest(leanOrder.BrokerId.Single(), leanOrder.LimitPrice, leanOrder.AbsoluteQuantity), _jsonSerializerSettings));

        var response = _apiClient.ExecuteRequest(request);

        return JsonConvert.DeserializeObject<CoinbaseEditOrderResponse>(response.Content);
    }

    /// <summary>
    /// Creates a new Coinbase order based on the specified Lean order.
    /// </summary>
    /// <param name="leanOrder">The Lean order object containing the order details.</param>
    /// <returns>
    /// A CoinbaseCreateOrderResponse representing the response from Coinbase after placing the order.
    /// </returns>
    /// <remarks>
    /// The method takes a Lean order object and converts it into the required format for placing an order
    /// using the Coinbase API. It then constructs a request to the Coinbase API for creating a new order,
    /// sends the request, and returns the response containing information about the created order.
    /// </remarks>
    public CoinbaseCreateOrderResponse CreateOrder(Order leanOrder)
    {
        var placeOrderRequest = CreateOrderRequest(leanOrder);

        var request = new RestRequest($"{_apiPrefix}/brokerage/orders", Method.POST);

        request.AddJsonBody(JsonConvert.SerializeObject(placeOrderRequest, _jsonSerializerSettings));

        var response = _apiClient.ExecuteRequest(request);

        return JsonConvert.DeserializeObject<CoinbaseCreateOrderResponse>(response.Content);
    }

    /// <summary>
    /// Creates a Coinbase order request based on the specified Lean order.
    /// </summary>
    /// <param name="leanOrder">The Lean order object containing the order details.</param>
    /// <returns>
    /// A CoinbaseCreateOrderRequest representing the request to be sent to Coinbase for order placement.
    /// </returns>
    private CoinbaseCreateOrderRequest CreateOrderRequest(Order leanOrder)
    {
        if (leanOrder.Direction == OrderDirection.Hold)
        {
            throw new NotSupportedException();
        }

        var model = new CoinbaseCreateOrderRequest(
            Guid.NewGuid(),
            SymbolMapper.GetBrokerageSymbol(leanOrder.Symbol),
            leanOrder.Direction == OrderDirection.Buy ? OrderSide.Buy : OrderSide.Sell);

        var orderProperties = leanOrder.Properties as CoinbaseOrderProperties;

        switch (leanOrder)
        {
            case MarketOrder:
                model.OrderConfiguration = new OrderConfiguration { MarketIoc = new() };
                if (leanOrder.Direction == OrderDirection.Buy)
                {
                    var price = GetTickerPrice(leanOrder.Symbol, leanOrder.Direction);
                    var minimumPriceVariation = SecurityProvider.GetSecurity(leanOrder.Symbol).SymbolProperties.MinimumPriceVariation;
                    model.OrderConfiguration.MarketIoc.QuoteSize = Math.Round(price * Math.Abs(leanOrder.Quantity) / minimumPriceVariation) * minimumPriceVariation;
                }
                else
                {
                    model.OrderConfiguration.MarketIoc.BaseSize = Math.Abs(leanOrder.Quantity);
                }
                break;
            case LimitOrder limitOrder when leanOrder.TimeInForce is Orders.TimeInForces.GoodTilCanceledTimeInForce:
                {
                    model.OrderConfiguration = new OrderConfiguration
                    {
                        LimitGtc = new()
                        {
                            BaseSize = Math.Abs(leanOrder.Quantity),
                            LimitPrice = limitOrder.LimitPrice,
                        }
                    };

                    model.OrderConfiguration.LimitGtc.PostOnly = orderProperties?.PostOnly;
                    break;
                }
            case LimitOrder limitOrder when leanOrder.TimeInForce is Orders.TimeInForces.GoodTilDateTimeInForce tilDate:
                {
                    model.OrderConfiguration = new OrderConfiguration
                    {
                        LimitGtd = new()
                        {
                            BaseSize = Math.Abs(leanOrder.Quantity),
                            LimitPrice = limitOrder.LimitPrice,
                            EndTime = tilDate.Expiry,
                        }
                    };

                    model.OrderConfiguration.LimitGtd.PostOnly = orderProperties?.PostOnly;
                    break;
                }
            case StopLimitOrder stopLimitOrder when leanOrder.TimeInForce is Orders.TimeInForces.GoodTilCanceledTimeInForce:
                var stopLimitGtc = new StopLimitGtc()
                {
                    BaseSize = Math.Abs(leanOrder.Quantity),
                    LimitPrice = stopLimitOrder.LimitPrice,
                    StopPrice = stopLimitOrder.StopPrice
                };

                var ticker = GetTickerPrice(leanOrder.Symbol, leanOrder.Direction);
                stopLimitGtc.StopDirection = stopLimitGtc.StopPrice > ticker ?
                    StopDirection.StopDirectionStopUp :
                    StopDirection.StopDirectionStopDown;

                model.OrderConfiguration = new() { StopLimitGtc = stopLimitGtc };
                break;
            case StopLimitOrder stopLimitOrder when leanOrder.TimeInForce is Orders.TimeInForces.GoodTilDateTimeInForce tilDate:
                var stopLimitGtd = new StopLimitGtd()
                {
                    EndTime = tilDate.Expiry,
                    StopPrice = stopLimitOrder.StopPrice,
                    LimitPrice = stopLimitOrder.LimitPrice,
                    BaseSize = Math.Abs(leanOrder.Quantity),
                };

                ticker = GetTickerPrice(leanOrder.Symbol, leanOrder.Direction);
                stopLimitGtd.StopDirection = stopLimitGtd.StopPrice > ticker ?
                    StopDirection.StopDirectionStopUp :
                    StopDirection.StopDirectionStopDown;

                model.OrderConfiguration = new() { StopLimitGtd = stopLimitGtd };
                break;
            default: throw new NotSupportedException($"Order type {leanOrder.Type.ToStringInvariant()} is not supported");
        };

        if (orderProperties?.SelfTradePreventionId == true)
        {
            model.SelfTradePreventionId = Guid.NewGuid();
        }

        return model;
    }

    /// <summary>
    /// Retrieves the ticker price for the specified symbol and order direction.
    /// </summary>
    /// <param name="symbol">The symbol for which to retrieve the ticker price.</param>
    /// <param name="leanOrderDirection">The order direction (Buy or Sell) for which to retrieve the ticker price.</param>
    /// <returns>
    /// The ticker price associated with the specified symbol and order direction.
    /// </returns>
    /// <remarks>
    /// The method first attempts to retrieve the ticker price from the provided security object.
    /// If the ticker price is not available or is zero, it queries the market trades for the specified symbol
    /// and retrieves the BestBid or BestAsk depending on the order direction.
    /// If the market trades data is also unavailable, the method throws a KeyNotFoundException.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the ticker price cannot be resolved due to missing market trades data.
    /// </exception>
    private decimal GetTickerPrice(Symbol symbol, OrderDirection leanOrderDirection)
    {
        var security = SecurityProvider.GetSecurity(symbol);
        var tickerPrice = leanOrderDirection == OrderDirection.Buy ? security.AskPrice : security.BidPrice;
        if (tickerPrice == 0)
        {
            var brokerageSymbol = SymbolMapper.GetBrokerageSymbol(symbol);
            var ticker = GetMarketTrades(brokerageSymbol);

            if (ticker.BestBid == 0 || ticker.BestAsk == 0)
            {
                throw new KeyNotFoundException(
                    $"CoinbaseBrokerage: Unable to resolve currency conversion pair: {symbol}");
            }

            tickerPrice = leanOrderDirection == OrderDirection.Buy ? ticker.BestAsk : ticker.BestBid;
        }

        return tickerPrice;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
    /// </summary>
    /// <remarks>
    /// This method disposes of the underlying API client safely to release any resources held by it.
    /// </remarks>
    public void Dispose()
    {
        _apiClient.DisposeSafely();
    }
}
