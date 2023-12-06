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
using System;

namespace QuantConnect.GDAX.Models;

public class CoinbaseOrderResponse : CoinbaseResponse
{
    /// <summary>
    /// Business data result
    /// </summary>
    [JsonProperty("orders")]
    public CoinbaseOrder[] Orders { get; set; }
}

/// <summary>
/// Order info
/// </summary>
public readonly struct CoinbaseOrder
{
    [JsonProperty("order_id")]
    public string OrderId { get; }

    [JsonProperty("product_id")]
    public string ProductId  { get; }

    [JsonProperty("user_id")]
    public string UserId { get; }

    [JsonProperty("order_configuration")]
    public OrderConfiguration OrderConfiguration { get; }

    [JsonProperty("side")]
    public string Side { get; }

    [JsonProperty("client_order_id")]
    public string ClientOrderId { get; }

    [JsonProperty("status")]
    public string Status { get; }

    [JsonProperty("time_in_force")]
    public string TimeInForce { get; }

    [JsonProperty("created_time")]
    public DateTime CreatedTime { get; }

    [JsonProperty("completion_percentage")]
    public decimal CompletionPercentage { get; }

    [JsonProperty("filled_size")]
    public decimal FilledSize { get; }

    [JsonProperty("average_filled_price")]
    public decimal AverageFilledPrice { get; }

    [JsonProperty("fee")]
    public string Fee { get; }

    [JsonProperty("number_of_fills")]
    public decimal NumberOfFills { get; }

    [JsonProperty("filled_value")]
    public decimal FilledValue { get; }

    [JsonProperty("pending_cancel")]
    public bool PendingCancel { get; }

    [JsonProperty("size_in_quote")]
    public bool SizeInQuote { get; }

    [JsonProperty("total_fees")]
    public decimal TotalFees { get; }

    [JsonProperty("size_inclusive_of_fees")]
    public bool SizeInclusiveOfFees { get; }

    [JsonProperty("total_value_after_fees")]
    public decimal TotalValueAfterFees { get; }

    [JsonProperty("trigger_status")]
    public string TriggerStatus { get; }

    [JsonProperty("order_type")]
    public string OrderType { get; }

    [JsonProperty("reject_reason")]
    public string RejectReason { get; }

    [JsonProperty("settled")]
    public bool Settled { get; }

    [JsonProperty("product_type")]
    public string ProductType { get; }

    [JsonProperty("reject_message")]
    public string RejectMessage { get; }

    [JsonProperty("cancel_message")]
    public string CancelMessage { get; }

    [JsonProperty("order_placement_source")]
    public string OrderPlacementSource { get; }

    [JsonProperty("outstanding_hold_amount")]
    public decimal OutstandingHoldAmount { get; }

    [JsonProperty("is_liquidation")]
    public bool IsLiquidation { get; }

    [JsonProperty("last_fill_time")]
    public string LastFillTime { get; }

    [JsonProperty("edit_history")]
    public EditHistory[] EditHistory { get; }

    [JsonProperty("leverage")]
    public string Leverage { get; }

    [JsonProperty("margin_type")]
    public string MarginType { get; }


    [JsonConstructor]
    public CoinbaseOrder(string orderId, string productId, string userId, OrderConfiguration orderConfiguration,
        string side, string clientOrderId, string status, string timeInForce, DateTime createdTime, decimal completionPercentage,
        decimal filledSize, decimal averageFilledPrice, string fee, decimal numberOfFills, decimal filledValue, bool pendingCancel,
        bool sizeInQuote, decimal totalFees, bool sizeInclusiveOfFees, decimal totalValueAfterFees, string triggerStatus,
        string orderType, string rejectReason, bool settled, string productType, string rejectMessage, string cancelMessage,
        string orderPlacementSource, decimal outstandingHoldAmount, bool isLiquidation, string lastFillTime,
        EditHistory[] editHistory, string leverage, string marginType)
    {
        OrderId = orderId;
        ProductId = productId;
        UserId = userId;
        OrderConfiguration = orderConfiguration;
        Side = side;
        ClientOrderId = clientOrderId;
        Status = status;
        TimeInForce = timeInForce;
        CreatedTime = createdTime;
        CompletionPercentage = completionPercentage;
        FilledSize = filledSize;
        AverageFilledPrice = averageFilledPrice;
        Fee = fee;
        NumberOfFills = numberOfFills;
        FilledValue = filledValue;
        PendingCancel = pendingCancel;
        SizeInQuote = sizeInQuote;
        TotalFees = totalFees;
        SizeInclusiveOfFees = sizeInclusiveOfFees;
        TotalValueAfterFees = totalValueAfterFees;
        TriggerStatus = triggerStatus;
        OrderType = orderType;
        RejectReason = rejectReason;
        Settled = settled;
        ProductType = productType;
        RejectMessage = rejectMessage;
        CancelMessage = cancelMessage;
        OrderPlacementSource = orderPlacementSource;
        OutstandingHoldAmount = outstandingHoldAmount;
        IsLiquidation = isLiquidation;
        LastFillTime = lastFillTime;
        EditHistory = editHistory;
        Leverage = leverage;
        MarginType = marginType;
    }

}

public class OrderConfiguration
{
    [JsonProperty("limit_limit_gtc")]
    public LimitGtc LimitGtc { get; set; }

    [JsonProperty("market_market_ioc")]
    public MarketIoc MarketIoc { get; set; }
}

public class MarketIoc
{
    [JsonProperty("base_size")]
    public decimal BaseSize { get; set; }

    [JsonProperty("quote_size")]
    public decimal QuoteSize { get; set; }
}

public class LimitGtc
{
    [JsonProperty("base_size")]
    public decimal BaseSize { get; set; }

    [JsonProperty("limit_price")]
    public decimal LimitPrice { get; set; }

    [JsonProperty("post_only")]
    public bool PostOnly { get; set; }
}

public readonly struct EditHistory
{
    [JsonProperty("price")]
    public string Price { get; }

    [JsonProperty("size")]
    public string Size { get; }

    [JsonProperty("replace_accept_timestamps")]
    public string ReplaceAcceptTimestamp { get; }

    [JsonConstructor]
    public EditHistory(string price, string size, string replaceAcceptTimestamp)
    {
        Price = price;
        Size = size;
        ReplaceAcceptTimestamp = replaceAcceptTimestamp;
    }
}