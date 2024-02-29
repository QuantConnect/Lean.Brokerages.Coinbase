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
using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Coinbase.Models;

/// <summary>
/// Business data of Coinbase account response
/// </summary>
public class CoinbaseAccountResponse : CoinbaseResponse
{
    /// <summary>
    /// Data about all accounts
    /// </summary>
    [JsonProperty("accounts")]
    public IEnumerable<CoinbaseAccount> Accounts { get; set; }

    /// <summary>
    /// Number of accounts returned
    /// </summary>
    [JsonProperty("size")]
    public int Size { get; set; }
}

/// <summary>
/// Business data of Coinbase account model
/// </summary>
public readonly struct CoinbaseAccount
{
    /// <summary>
    /// Unique identifier for account.
    /// </summary>
    [JsonProperty("uuid")]
    public string Uuid { get; }

    /// <summary>
    /// Name for the account.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; }

    /// <summary>
    /// Currency symbol for the account.
    /// </summary>
    [JsonProperty("currency")]
    public string Currency { get; }

    /// <summary>
    /// Available Balance account
    /// </summary>
    [JsonProperty("available_balance")]
    public AvailableBalance AvailableBalance { get; }

    /// <summary>
    /// Whether or not this account is the user's primary account
    /// </summary>
    [JsonProperty("default")]
    public bool Default { get; }

    /// <summary>
    /// Whether or not this account is active and okay to use.
    /// </summary>
    [JsonProperty("active")]
    public bool Active { get; }

    /// <summary>
    /// Time at which this account was created.
    /// </summary>
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Time at which this account was updated.
    /// </summary>
    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; }

    /// <summary>
    /// Time at which this account was deleted.
    /// </summary>
    [JsonProperty("deleted_at")]
    public DateTime? DeletedAt { get; }

    /// <summary>
    /// Possible values: [ACCOUNT_TYPE_CRYPTO, ACCOUNT_TYPE_FIAT, ACCOUNT_TYPE_VAULT]
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; }

    /// <summary>
    /// Whether or not this account is ready to trade.
    /// </summary>
    [JsonProperty("ready")]
    public bool Ready { get; }

    /// <summary>
    /// Available account hold balance
    /// </summary>
    [JsonProperty("hold")]
    public AvailableBalance Hold { get; }

    [JsonConstructor]
    public CoinbaseAccount(string uuid, string name, string currency, AvailableBalance availableBalance, bool @default,
        bool active, DateTime createdAt, DateTime updatedAt, DateTime? deletedAt, string type, bool ready, AvailableBalance hold)
    {
        Uuid = uuid; 
        Name = name;
        Currency = currency;
        AvailableBalance = availableBalance;
        Default = @default;
        Active = active;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        DeletedAt = deletedAt;
        Type = type;
        Ready = ready;
        Hold = hold;
    }
}

/// <summary>
/// Available balance account
/// </summary>
public readonly struct AvailableBalance  
{
    /// <summary>
    /// Amount of currency that this object represents.
    /// </summary>
    [JsonProperty("value")]
    public decimal Value { get; }

    /// <summary>
    /// Denomination of the currency.
    /// </summary>
    [JsonProperty("currency")]
    public string Currency { get; }

    [JsonConstructor]
    public AvailableBalance(decimal value, string currency)
    {
        Value = value;
        Currency = currency;
    }
}
