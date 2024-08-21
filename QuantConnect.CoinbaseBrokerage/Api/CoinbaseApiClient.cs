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

using Jose;
using System;
using RestSharp;
using System.Net;
using System.Linq;
using QuantConnect.Util;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("QuantConnect.Brokerages.Coinbase.Tests")]

namespace QuantConnect.Brokerages.Coinbase.Api;

/// <summary>
/// Coinbase api client implementation
/// </summary>
public class CoinbaseApiClient : IDisposable
{
    /// <summary>
    /// Provides a thread-safe random number generator instance.
    /// </summary>
    private readonly static Random _random = new Random();

    /// <summary>
    /// Stores the CDP API key name used for authenticating requests.
    /// </summary>
    /// <remarks>
    /// This field holds the API key name, which is essential for identifying and authenticating 
    /// API requests made to Coinbase's services. The key is provided during the initialization 
    /// of the <see cref="CoinbaseApiClient"/>.
    /// </remarks>
    private readonly string _name;

    /// <summary>
    /// Represents an ECDSA private key used for cryptographic operations.
    /// The private key is initialized from a base64-encoded string and imported into an ECDSA instance.
    /// </summary>
    private readonly ECDsa _privateKey;

    /// <summary>
    /// Represents the REST client used to send HTTP requests to the Coinbase API.
    /// </summary>
    private readonly RestClient _restClient;

    /// <summary>
    /// Manages rate limiting for outbound API requests.
    /// </summary>
    private readonly RateGate _rateGate;

    /// <summary>
    /// Represents the Unix epoch time, which is the starting point for Unix time calculation.
    /// </summary>
    private static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Initializes a new instance of the <see cref="CoinbaseApiClient"/> class with the specified CDP API keys, 
    /// REST API URL, and maximum requests per second.
    /// </summary>
    /// <param name="name">The CDP API key required for authenticating requests.</param>
    /// <param name="privateKey">The CDP API key secret used to sign requests. This will be parsed into a usable format.</param>
    /// <param name="restApiUrl">The base URL of the Coinbase REST API.</param>
    /// <param name = "maxRequestsPerSecond" > The maximum number of requests that can be sent to the API per second.</param>
    /// <remarks>
    /// This constructor sets up the Coinbase API client by initializing the CDP API key, parsing the private key, 
    /// configuring the REST client with the provided API URL, and setting up a rate limiter to ensure that 
    /// requests do not exceed the specified maximum rate. The <see cref="RateGate"/> helps prevent the client 
    /// from hitting rate limits imposed by the API.
    /// </remarks>
    public CoinbaseApiClient(string name, string privateKey, string restApiUrl, int maxRequestsPerSecond)
    {
        _name = name;
        _restClient = new RestClient(restApiUrl);
        _rateGate = new RateGate(maxRequestsPerSecond, Time.OneSecond);

        _privateKey = ECDsa.Create();
        _privateKey.ImportECPrivateKey(Convert.FromBase64String(ParseKey(privateKey)), out _);
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
        var uri = _restClient.BuildUri(request);
        var generatedJWTToken = GenerateToken($"{request.Method} {uri.Host + uri.AbsolutePath}");
        request.AddOrUpdateHeader("Authorization", "Bearer " + generatedJWTToken);
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

        var response = _restClient.Execute(request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"{nameof(CoinbaseApiClient)}.{nameof(ExecuteRequest)} failed: [{(int)response.StatusCode}] {response.StatusDescription}, Content: {response.Content}, ErrorMessage: {response.ErrorMessage}");
        }

        return response;
    }

    /// <summary>
    /// Generates a JWT token for WebSocket connections.
    /// </summary>
    /// <returns>A signed JWT token as a string.</returns>
    public string GenerateWebSocketToken()
    {
        return GenerateToken();
    }

    /// <summary>
    /// Generates a JWT token with the specified parameters using ECDsa signing.
    /// </summary>
    /// <param name="uri">The URI to include in the token payload. Pass null for WebSocket tokens.</param>
    /// <returns>A signed JWT token as a string.</returns>
    /// <remarks>
    /// This method creates a JWT token with a subject, issuer, and a short expiration time, signed using the ES256 algorithm.
    /// It also includes a nonce in the token headers to prevent replay attacks.
    /// </remarks>
    private string GenerateToken(string uri = null)
    {
        var utcNow = DateTime.UtcNow;

        var payload = new Dictionary<string, object>
         {
             { "sub", _name },
             { "iss", "coinbase-cloud" },
             { "nbf", Convert.ToInt64((utcNow - EpochTime).TotalSeconds) },
             { "exp", Convert.ToInt64((utcNow.AddMinutes(1) - EpochTime).TotalSeconds) }
         };

        if (uri != null)
        {
            payload.Add("uri", uri);
        }

        var extraHeaders = new Dictionary<string, object>
         {
             { "kid", _name },
             // add nonce to prevent replay attacks with a random 10 digit number
             { "nonce", RandomHex(10) },
             { "typ", "JWT"}
         };

        var encodedToken = JWT.Encode(payload, _privateKey, JwsAlgorithm.ES256, extraHeaders);

        return encodedToken;
    }

    /// <summary>
    /// Parses a key string by removing the first and last lines and returning the remaining content as a single string.
    /// </summary>
    /// <param name="key">The key string to be parsed. It is expected to have multiple lines, with each line separated by a newline character.</param>
    /// <returns>A string that concatenates the remaining lines after the first and last lines are removed.</returns>
    /// <remarks>
    /// This method is useful when handling key formats that typically have headers and footers (e.g., PEM format). 
    /// It removes the first and last lines, which might contain non-essential information like "BEGIN" and "END" markers, 
    /// and returns the core content of the key.
    /// </remarks>
    internal string ParseKey(string key)
    {
        List<string> keyLines = new List<string>();
        keyLines.AddRange(key.Split('\n', StringSplitOptions.RemoveEmptyEntries));

        keyLines.RemoveAt(0);
        keyLines.RemoveAt(keyLines.Count - 1);

        return string.Join("", keyLines);
    }

    /// <summary>
    /// Generates a random hexadecimal string of the specified length.
    /// </summary>
    /// <param name="digits">The number of hexadecimal digits to generate.</param>
    /// <returns>A string containing a random sequence of hexadecimal characters.</returns>
    /// <remarks>
    /// If the specified number of digits is odd, the method will generate one extra random digit
    /// to ensure the output string has the exact number of requested digits.
    /// </remarks>
    private static string RandomHex(int digits)
    {
        byte[] buffer = new byte[digits / 2];
        _random.NextBytes(buffer);
        string result = string.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
        if (digits % 2 == 0)
            return result;
        return result + _random.Next(16).ToString("X");
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <remarks>
    /// This method disposes of the underlying HMAC-SHA256 instance safely.
    /// </remarks>
    public void Dispose()
    {
        _rateGate.Dispose();
        _privateKey.Dispose();
    }
}
