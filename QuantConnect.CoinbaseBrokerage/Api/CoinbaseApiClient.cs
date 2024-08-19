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
using System.Text;
using System.Linq;
using QuantConnect.Util;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace QuantConnect.Brokerages.Coinbase.Api;

/// <summary>
/// Coinbase api client implementation
/// </summary>
public class CoinbaseApiClient : IDisposable
{
    private readonly static Random _random = new Random();
    private readonly string _apiKey;
    private readonly string _parsedCbPrivateKey;
    private readonly HMACSHA256 _hmacSha256;
    private readonly RestClient _restClient;
    private readonly RateGate _rateGate;

    public CoinbaseApiClient(string apiKey, string apiKeySecret, string restApiUrl, int maxRequestsPerSecond)
    {
        _apiKey = apiKey;
        _restClient = new RestClient(restApiUrl);
        _parsedCbPrivateKey = ParseKey(apiKeySecret);
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
        var uri = _restClient.BuildUri(request);
        var generatedJWTToken = GenerateRestToken(_apiKey, _parsedCbPrivateKey, $"{request.Method} {uri.Host + uri.AbsolutePath}");

        if (!IsTokenValid(generatedJWTToken, _apiKey, _parsedCbPrivateKey))
        {
            throw new InvalidOperationException("The generated JWT token is invalid. Authentication failed.");
        }

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

    public string GenerateWebSocketJWTToken()
    {
        return GenerateWebSocketToken(_apiKey, _parsedCbPrivateKey);
    }

    private static string GenerateRestToken(string name, string secret, string uri)
    {
        var privateKeyBytes = Convert.FromBase64String(secret); // Assuming PEM is base64 encoded
        using var key = ECDsa.Create();
        key.ImportECPrivateKey(privateKeyBytes, out _);

        var payload = new Dictionary<string, object>
             {
                 { "sub", name },
                 { "iss", "coinbase-cloud" },
                 { "nbf", Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) },
                 { "exp", Convert.ToInt64((DateTime.UtcNow.AddMinutes(1) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) },
                 { "uri", uri }
             };

        var extraHeaders = new Dictionary<string, object>
             {
                 { "kid", name },
                 // add nonce to prevent replay attacks with a random 10 digit number
                 { "nonce", RandomHex(10) },
                 { "typ", "JWT"}
             };

        var encodedToken = JWT.Encode(payload, key, JwsAlgorithm.ES256, extraHeaders);

        return encodedToken;
    }

    /// <summary>
    /// Generates a JWT token with the specified name and secret using ECDsa signing.
    /// </summary>
    /// <param name="name">The name to be used as the subject ("sub") and key identifier ("kid") in the token payload and headers.</param>
    /// <param name="privateKey">The ECDsa private key in Base64 format used to sign the token.</param>
    /// <returns>A signed JWT token as a string.</returns>
    /// <remarks>
    /// This method creates a JWT token with a subject, issuer, and a short expiration time, signed using the ES256 algorithm. 
    /// It also includes a nonce in the token headers to prevent replay attacks.
    /// </remarks>
    private static string GenerateWebSocketToken(string name, string privateKey)
    {
        var privateKeyBytes = Convert.FromBase64String(privateKey); // Assuming PEM is base64 encoded
        using var key = ECDsa.Create();
        key.ImportECPrivateKey(privateKeyBytes, out _);

        var payload = new Dictionary<string, object>
             {
                 { "sub", name },
                 { "iss", "coinbase-cloud" },
                 { "nbf", Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) },
                 { "exp", Convert.ToInt64((DateTime.UtcNow.AddMinutes(1) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) },
             };

        var extraHeaders = new Dictionary<string, object>
             {
                 { "kid", name },
                 // add nonce to prevent replay attacks with a random 10 digit number
                 { "nonce", RandomHex(10) },
                 { "typ", "JWT"}
             };

        var encodedToken = JWT.Encode(payload, key, JwsAlgorithm.ES256, extraHeaders);

        return encodedToken;
    }

    /// <summary>
    /// Validates a JWT token using ECDsa key with the specified token ID and secret.
    /// </summary>
    /// <param name="token">The JWT token to be validated.</param>
    /// <param name="tokenId">The unique identifier for the ECDsa security key.</param>
    /// <param name="secret">The ECDsa private key in Base64 format used to validate the token's signature.</param>
    /// <returns>
    /// <c>true</c> if the token is successfully validated; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method is useful for verifying the authenticity of JWT tokens using ECDsa keys. 
    /// It ensures that the token's signature matches the expected signature derived from the provided secret.
    /// </remarks>
    private static bool IsTokenValid(string token, string tokenId, string secret)
    {
        if (token == null)
            return false;

        var key = ECDsa.Create();
        key?.ImportECPrivateKey(Convert.FromBase64String(secret), out _);

        var securityKey = new ECDsaSecurityKey(key) { KeyId = tokenId };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.FromSeconds(100),
                ValidateLifetime = true,
                LifetimeValidator = CustomLifetimeValidator,
            }, out var validatedToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool CustomLifetimeValidator(DateTime? notBefore, DateTime? expires, SecurityToken tokenToValidate, TokenValidationParameters @param)
    {
        if (expires != null)
        {
            return expires > DateTime.UtcNow;
        }
        return false;
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
    private string ParseKey(string key)
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
        _hmacSha256.DisposeSafely();
    }
}
