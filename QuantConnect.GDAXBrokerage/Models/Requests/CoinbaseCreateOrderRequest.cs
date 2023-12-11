using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QuantConnect.CoinbaseBrokerage.Models.Enums;
using System;

namespace QuantConnect.CoinbaseBrokerage.Models.Requests;

/// <summary>
/// Coinbase place order api request
/// </summary>
public class CoinbaseCreateOrderRequest
{
    [JsonProperty("client_order_id")]
    public Guid ClientOrderId { get; set; }

    [JsonProperty("product_id")]
    public string ProductId { get; set; }

    [JsonProperty("side")]
    [JsonConverter(typeof(StringEnumConverter))]
    public OrderSide Side { get; set; }

    [JsonProperty("order_configuration")]
    public OrderConfiguration OrderConfiguration { get; set; }

    /// <summary>
    /// Self trade prevention ID, to prevent an order crossing against the same user
    /// </summary>
    [JsonProperty("self_trade_prevention_id")]
    public Guid? SelfTradePreventionId { get; set; } = null;
}