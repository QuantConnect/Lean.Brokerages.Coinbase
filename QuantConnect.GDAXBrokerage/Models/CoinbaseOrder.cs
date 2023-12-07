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

/// <summary>
/// Business data of Coinbase order response
/// </summary>
public class CoinbaseOrderResponse : CoinbaseResponse
{
    /// <summary>
    /// A list of orders matching the query.
    /// </summary>
    [JsonProperty("orders")]
    public CoinbaseOrder[] Orders { get; set; }

    /// <summary>
    /// The sequence of the db at which this state was read.
    /// </summary>
    [JsonProperty("sequence")]
    public string Sequence { get; set; }
}

/// <summary>
/// Order info
/// </summary>
public readonly struct CoinbaseOrder
{
    /// <summary>
    /// The unique id for this order
    /// </summary>
    [JsonProperty("order_id")]
    public string OrderId { get; }

    /// <summary>
    /// The product this order was created for e.g. 'BTC-USD'
    /// </summary>
    [JsonProperty("product_id")]
    public string ProductId  { get; }

    /// <summary>
    /// The id of the User owning this Order
    /// </summary>
    [JsonProperty("user_id")]
    public string UserId { get; }

    /// <summary>
    /// Order Configuration Type
    /// </summary>
    [JsonProperty("order_configuration")]
    public OrderConfiguration OrderConfiguration { get; }

    /// <summary>
    /// Possible values: [BUY, SELL]
    /// </summary>
    [JsonProperty("side")]
    public string Side { get; }

    /// <summary>
    /// Client specified ID of order.
    /// </summary>
    [JsonProperty("client_order_id")]
    public string ClientOrderId { get; }

    /// <summary>
    /// Possible values: [OPEN, FILLED, CANCELLED, EXPIRED, FAILED, UNKNOWN_ORDER_STATUS]
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; }

    /// <summary>
    /// Possible values: 
    /// [UNKNOWN_TIME_IN_FORCE, GOOD_UNTIL_DATE_TIME, GOOD_UNTIL_CANCELLED, IMMEDIATE_OR_CANCEL, FILL_OR_KILL]
    /// </summary>
    [JsonProperty("time_in_force")]
    public string TimeInForce { get; }

    /// <summary>
    /// Timestamp for when the order was created
    /// </summary>
    [JsonProperty("created_time")]
    public DateTime CreatedTime { get; }

    /// <summary>
    /// The percent of total order amount that has been filled
    /// </summary>
    [JsonProperty("completion_percentage")]
    public decimal CompletionPercentage { get; }

    /// <summary>
    /// The portion (in base currency) of total order amount that has been filled
    /// </summary>
    [JsonProperty("filled_size")]
    public decimal FilledSize { get; }

    /// <summary>
    /// The average of all prices of fills for this order
    /// </summary>
    [JsonProperty("average_filled_price")]
    public decimal AverageFilledPrice { get; }

    /// <summary>
    /// Commission amount
    /// </summary>
    [JsonProperty("fee")]
    public string Fee { get; }

    /// <summary>
    /// Number of fills that have been posted for this order
    /// </summary>
    [JsonProperty("number_of_fills")]
    public decimal NumberOfFills { get; }

    /// <summary>
    /// The portion (in quote current) of total order amount that has been filled
    /// </summary>
    [JsonProperty("filled_value")]
    public decimal FilledValue { get; }

    /// <summary>
    /// Whether a cancel request has been initiated for the order, and not yet completed
    /// </summary>
    [JsonProperty("pending_cancel")]
    public bool PendingCancel { get; }

    /// <summary>
    /// Whether the order was placed with quote currency
    /// </summary>
    [JsonProperty("size_in_quote")]
    public bool SizeInQuote { get; }

    /// <summary>
    /// The total fees for the order
    /// </summary>
    [JsonProperty("total_fees")]
    public decimal TotalFees { get; }

    /// <summary>
    /// Whether the order size includes fees
    /// </summary>
    [JsonProperty("size_inclusive_of_fees")]
    public bool SizeInclusiveOfFees { get; }

    /// <summary>
    /// derived field: filled_value + total_fees for buy orders and filled_value - total_fees for sell orders.
    /// </summary>
    [JsonProperty("total_value_after_fees")]
    public decimal TotalValueAfterFees { get; }

    /// <summary>
    /// Possible values: [UNKNOWN_TRIGGER_STATUS, INVALID_ORDER_TYPE, STOP_PENDING, STOP_TRIGGERED]
    /// </summary>
    [JsonProperty("trigger_status")]
    public string TriggerStatus { get; }

    /// <summary>
    /// Possible values: [UNKNOWN_ORDER_TYPE, MARKET, LIMIT, STOP, STOP_LIMIT]
    /// </summary>
    [JsonProperty("order_type")]
    public string OrderType { get; }

    /// <summary>
    /// Possible values: [REJECT_REASON_UNSPECIFIED]
    /// </summary>
    [JsonProperty("reject_reason")]
    public string RejectReason { get; }

    /// <summary>
    /// True if the order is fully filled, false otherwise.
    /// </summary>
    [JsonProperty("settled")]
    public bool Settled { get; }

    /// <summary>
    /// Possible values: [SPOT, FUTURE]
    /// </summary>
    [JsonProperty("product_type")]
    public string ProductType { get; }

    /// <summary>
    /// Message stating why the order was rejected.
    /// </summary>
    [JsonProperty("reject_message")]
    public string RejectMessage { get; }

    /// <summary>
    /// Message stating why the order was canceled.
    /// </summary>
    [JsonProperty("cancel_message")]
    public string CancelMessage { get; }

    /// <summary>
    /// Possible values: [RETAIL_SIMPLE, RETAIL_ADVANCED]
    /// </summary>
    [JsonProperty("order_placement_source")]
    public string OrderPlacementSource { get; }

    /// <summary>
    /// The remaining hold amount (holdAmount - holdAmountReleased). [value is 0 if holdReleased is true]
    /// </summary>
    [JsonProperty("outstanding_hold_amount")]
    public decimal OutstandingHoldAmount { get; }

    /// <summary>
    /// True if order is of liquidation type.
    /// </summary>
    [JsonProperty("is_liquidation")]
    public bool IsLiquidation { get; }

    /// <summary>
    /// Time of the most recent fill for this order
    /// </summary>
    [JsonProperty("last_fill_time")]
    public string LastFillTime { get; }

    /// <summary>
    /// An array of the latest 5 edits per order
    /// </summary>
    [JsonProperty("edit_history")]
    public EditHistory[] EditHistory { get; }

    /// <summary>
    /// Not provided
    /// </summary>
    [JsonProperty("leverage")]
    public string Leverage { get; }

    /// <summary>
    /// Possible values: [UNKNOWN_MARGIN_TYPE]
    /// </summary>
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

/// <summary>
/// Order Configuration Type: Market, LimitGtc, LimitGtd, StopLimitGtc, StopLimitGtd 
/// </summary>
public class OrderConfiguration
{
    /// <summary>
    /// Market
    /// </summary>
    [JsonProperty("market_market_ioc")]
    public MarketIoc MarketIoc { get; set; }

    /// <summary>
    /// Limit Good till cancel
    /// </summary>
    [JsonProperty("limit_limit_gtc")]
    public LimitGtc LimitGtc { get; set; }

    /// <summary>
    /// Limit Good till day
    /// </summary>
    [JsonProperty("limit_limit_gtd")]
    public LimitGtd LimitGtd { get; set; }

    /// <summary>
    /// Stop Limit Good till cancel
    /// </summary>
    [JsonProperty("stop_limit_stop_limit_gtc")]
    public StopLimitGtc StopLimitGtc { get; set; }

    /// <summary>
    /// Stop Limit Good till day
    /// </summary>
    [JsonProperty("stop_limit_stop_limit_gtd")]
    public StopLimitGtd StopLimitGtd { get; set; }
}

/// <summary>
/// Market Order Configuration Type
/// </summary>
public class MarketIoc
{
    /// <summary>
    /// Amount of base currency to spend on order. Required for SELL orders. <see cref="CoinbaseOrder.Side"/>
    /// </summary>
    [JsonProperty("base_size")]
    public decimal BaseSize { get; set; }

    /// <summary>
    /// Amount of quote currency to spend on order. Required for BUY orders. <see cref="CoinbaseOrder.Side"/>
    /// </summary>
    [JsonProperty("quote_size")]
    public decimal QuoteSize { get; set; }
}

/// <summary>
/// LimitGtc Order Configuration Type
/// [Gtc] - Good Till Cancel
/// </summary>
public class LimitGtc
{
    /// <summary>
    /// Amount of base currency to spend on order
    /// </summary>
    [JsonProperty("base_size")]
    public decimal BaseSize { get; set; }

    /// <summary>
    /// Ceiling price for which the order should get filled.
    /// </summary>
    [JsonProperty("limit_price")]
    public decimal LimitPrice { get; set; }

    /// <summary>
    /// The post-only flag indicates that the order should only make liquidity. 
    /// If any part of the order results in taking liquidity, the order will be rejected and no part of it will execute.
    /// </summary>
    [JsonProperty("post_only")]
    public bool PostOnly { get; set; }
}

/// <summary>
/// LimitGtd Order Configuration Type
/// [Gtd] - Good till day
/// </summary>
public class LimitGtd : LimitGtc
{
    /// <summary>
    /// Time at which the order should be cancelled if it's not filled.
    /// </summary>
    [JsonProperty("end_time")]
    public DateTime EndTime { get; set; }
}

/// <summary>
/// StopLimitGtc Order Configuration Type
/// [Gtc] - Good Till Cancel
/// </summary>
public class StopLimitGtc
{
    /// <summary>
    /// Amount of base currency to spend on order
    /// </summary>
    [JsonProperty("base_size")]
    public decimal BaseSize { get; set; }

    /// <summary>
    /// Ceiling price for which the order should get filled.
    /// </summary>
    [JsonProperty("limit_price")]
    public decimal LimitPrice { get; set; }

    /// <summary>
    /// Price at which the order should trigger - if stop direction is Up, 
    /// then the order will trigger when the last trade price goes above this, 
    /// otherwise order will trigger when last trade price goes below this price.
    /// </summary>
    [JsonProperty("stop_price")]
    public decimal StopPrice { get; set; }

    /// <summary>
    /// Possible values: [STOP_DIRECTION_STOP_UP, STOP_DIRECTION_STOP_DOWN]
    /// </summary>
    [JsonProperty("stop_direction")]
    public string StopDirection { get; set; }
}

/// <summary>
/// StopLimitGtc Order Configuration Type
/// [Gtd] - Good till day
/// </summary>
public class StopLimitGtd : StopLimitGtc
{
    /// <summary>
    /// Time at which the order should be cancelled if it's not filled.
    /// </summary>
    [JsonProperty("end_time")]
    public DateTime EndTime { get; set; }
}

/// <summary>
/// Order's Edit History
/// </summary>
public readonly struct EditHistory
{
    /// <summary>
    /// New price order
    /// </summary>
    [JsonProperty("price")]
    public string Price { get; }

    /// <summary>
    /// New size order
    /// </summary>
    [JsonProperty("size")]
    public string Size { get; }

    /// <summary>
    /// Time when changes was accepted
    /// </summary>
    [JsonProperty("replace_accept_timestamps")]
    public DateTime ReplaceAcceptTimestamp { get; }

    [JsonConstructor]
    public EditHistory(string price, string size, DateTime replaceAcceptTimestamp)
    {
        Price = price;
        Size = size;
        ReplaceAcceptTimestamp = replaceAcceptTimestamp;
    }
}