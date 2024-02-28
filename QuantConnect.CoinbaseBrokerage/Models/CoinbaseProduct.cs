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

using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Coinbase.Models;

/// <summary>
/// Business data of Coinbase products response
/// </summary>
public class CoinbaseProductResponse
{
    /// <summary>
    /// Array of objects, each representing one product.
    /// </summary>
    [JsonProperty("products")]
    public IEnumerable<CoinbaseProduct> Products { get; set; }

    /// <summary>
    /// Number of products that were returned.
    /// </summary>
    [JsonProperty("num_products")]
    public int NumProducts { get; set; }
}

public readonly struct CoinbaseProduct
{
    /// <summary>
    /// The trading pair.
    /// </summary>
    [JsonProperty("product_id")]
    public string ProductId { get; }

    /// <summary>
    /// Name of the base currency.
    /// </summary>
    [JsonProperty("base_name")]
    public string BaseName { get; }

    /// <summary>
    /// Name of the quote currency.
    /// </summary>
    [JsonProperty("quote_name")]
    public string QuoteName { get; }

    /// <summary>
    /// Symbol of the base currency.
    /// </summary>
    [JsonProperty("base_currency_id")]
    public string BaseCurrencyId { get; }

    /// <summary>
    /// Symbol of the quote currency.
    /// </summary>
    [JsonProperty("quote_currency_id")]
    public string QuoteCurrencyId { get; }

    /// <summary>
    /// Minimum amount price can be increased or decreased at once.
    /// </summary>
    [JsonProperty("price_increment")]
    public decimal PriceIncrement { get; }

    /// <summary>
    /// Minimum amount base value can be increased or decreased at once.
    /// </summary>
    [JsonProperty("base_increment")]
    public decimal BaseIncrement { get; }

    /// <summary>
    /// Minimum amount quote value can be increased or decreased at once.
    /// </summary>
    [JsonProperty("quote_increment")]
    public decimal QuoteIncrement { get; }

    /// <summary>
    /// Minimum size that can be represented of base currency.
    /// </summary>
    [JsonProperty("base_min_size")]
    public decimal BaseMinSize { get; }

    /// <summary>
    /// Status of the product.
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; }

    [JsonConstructor]
    public CoinbaseProduct(string productId, string baseName, string quoteName, string baseCurrencyId, string quoteCurrencyId, decimal priceIncrement,
        decimal baseIncrement, decimal quoteIncrement, decimal baseMinSize, string status)
    {
        ProductId = productId;
        BaseName = baseName;
        QuoteName = quoteName;
        BaseCurrencyId = baseCurrencyId;
        QuoteCurrencyId = quoteCurrencyId;
        PriceIncrement = priceIncrement;
        BaseIncrement = baseIncrement;
        QuoteIncrement = quoteIncrement;
        BaseMinSize = baseMinSize;
        Status = status;
    }
}
