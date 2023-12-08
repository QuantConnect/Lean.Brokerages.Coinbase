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
using System.Text;
using System.Linq;
using QuantConnect.Util;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;

namespace QuantConnect.CoinbaseBrokerage.Api;

/// <summary>
/// Coinbase api client implementation
/// </summary>
public class CoinbaseApiClient : IDisposable
{
    private readonly string _apiKey;
    private readonly HMACSHA256 _hmacSha256;
    private readonly RestClient _restClient;
    private readonly RateGate _rateGate;

    public CoinbaseApiClient(string apiKey, string apiKeySecret, string restApiUrl, int maxRequestsPerSecond)
    {
        _apiKey = apiKey;
        _restClient = new RestClient(restApiUrl);
        _hmacSha256 = new HMACSHA256(Encoding.UTF8.GetBytes(apiKeySecret));
        _rateGate = new RateGate(maxRequestsPerSecond, Time.OneSecond);
    }

    /// <summary>
    /// Authenticates a given REST request by adding necessary headers such as CB-ACCESS-KEY, CB-ACCESS-SIGN, and CB-ACCESS-TIMESTAMP.
    /// </summary>
    /// <param name="request">The REST request to be authenticated.</param>
    /// <remarks>
    /// This method computes and adds the required authentication headers to the provided REST request, including the CB-ACCESS-KEY,
    /// CB-ACCESS-SIGN (signature), and CB-ACCESS-TIMESTAMP (timestamp) headers. The signature is generated using the HMAC-SHA256 algorithm.
    /// </remarks>
    private void AuthenticateRequest(IRestRequest request)
    {
        var body = request.Parameters.SingleOrDefault(b => b.Type == ParameterType.RequestBody);

        var urlPath = _restClient.BuildUri(request).AbsolutePath;

        var timestamp = GetNonce();

        var signature = GetSign(timestamp, request.Method.ToString(), urlPath, body?.Value.ToString() ?? string.Empty);

        request.AddHeader("CB-ACCESS-KEY", _apiKey);
        request.AddHeader("CB-ACCESS-SIGN", signature);
        request.AddHeader("CB-ACCESS-TIMESTAMP", timestamp);
    }

    /// <summary>
    /// Executes a REST request, incorporating rate limiting using a rate gate.
    /// </summary>
    /// <param name="request">The REST request to be executed.</param>
    /// <returns>
    /// An instance of <see cref="IRestResponse"/> representing the response of the executed request.
    /// </returns>
    /// <remarks>
    /// This method waits for the rate gate to allow the request to proceed before executing the provided REST request using
    /// the underlying REST client. The rate gate is used for rate limiting to control the frequency of outgoing requests.
    /// </remarks>
    [StackTraceHidden]
    public IRestResponse ExecuteRequest(IRestRequest request)
    {
        _rateGate.WaitToProceed();

        AuthenticateRequest(request);

        return _restClient.Execute(request);
    }

    /// <summary>
    /// Generates a signature for a given set of parameters using HMAC-SHA256.
    /// </summary>
    /// <param name="timeStamp">The timestamp of the request.</param>
    /// <param name="httpMethod">The HTTP method used for the request (e.g., GET, POST).</param>
    /// <param name="urlPath">The URL path of the request.</param>
    /// <param name="body">The request body.</param>
    /// <returns>
    /// A string representation of the generated signature in lowercase hexadecimal format.
    /// </returns>
    /// <remarks>
    /// The signature is computed using the HMAC-SHA256 algorithm and is typically used for authentication and message integrity.
    /// </remarks>
    public string GetSign(string timeStamp, string httpMethod, string urlPath, string body)
    {
        var preHash = timeStamp + httpMethod + urlPath + body;

        var sig = _hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(preHash));

        return Convert.ToHexString(sig).ToLower();
    }

    /// <summary>
    /// Generates a unique nonce based on the current UTC time in Unix timestamp format.
    /// </summary>
    /// <returns>
    /// A string representation of the generated nonce.
    /// </returns>
    /// <remarks>
    /// The nonce is used to ensure the uniqueness of each request, typically in the context of security and authentication.
    /// </remarks>
    private static string GetNonce()
    {
        return Time.DateTimeToUnixTimeStamp(DateTime.UtcNow).ToString("F0", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <remarks>
    /// This method disposes of the underlying HMAC-SHA256 instance safely.
    /// </remarks>
    public void Dispose()
    {
        _hmacSha256.DisposeSafely();
    }
}
