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
using System.Collections.Generic;
using QuantConnect.CoinbaseBrokerage.Models;
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

    public CoinbaseApi(string apiKey, string apiKeySecret, string restApiUrl)
    {
        _apiClient = new CoinbaseApiClient(apiKey, apiKeySecret, restApiUrl, maxGateLimitOccurrences);
    }

    public IEnumerable<CoinbaseOrder> GetListOrders(BrokerageEnums.OrderStatus orderStatus)
    {
        var request = new RestRequest($"{_apiPrefix}/brokerage/orders/historical/batch", Method.GET);

        if (orderStatus != BrokerageEnums.OrderStatus.UNKNOWN_ORDER_STATUS)
        {
            request.AddQueryParameter("order_status", orderStatus.ToString());
        }

        var response = _apiClient.ExecuteRequest(request);

        return JsonConvert.DeserializeObject<CoinbaseOrderResponse>(response.Content).Orders;
    }

    public CoinbaseCancelOrder CancelOrders(List<string> brokerIds)
    {
        var request = new RestRequest($"{_apiPrefix}/brokerage/orders/batch_cancel", Method.POST);

        request.AddJsonBody(JsonConvert.SerializeObject(new { order_ids = brokerIds }));

        var response = _apiClient.ExecuteRequest(request);

        return JsonConvert.DeserializeObject<CoinbaseCancelOrders>(response.Content).Result.First();
    }

    public CoinbaseMarketTrades GetMarketTrades(string productId, int limit = 1)
    {
        if (string.IsNullOrEmpty(productId))
        {
            throw new ArgumentException("CoinbaseApi:GetMarketTrades(): productId is null or empty");
        }

        var request = new RestRequest($"{_apiPrefix}/brokerage/products/{productId}/ticker", Method.GET);

        request.AddQueryParameter("limit", limit.ToStringInvariant());

        var response = _apiClient.ExecuteRequest(request);

        return JsonConvert.DeserializeObject<CoinbaseMarketTrades>(response.Content);
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
