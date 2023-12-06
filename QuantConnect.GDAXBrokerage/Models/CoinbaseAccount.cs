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

namespace QuantConnect.GDAX.Models;

public class CoinbaseAccountResponse : CoinbaseResponse
{
    /// <summary>
    /// Business data result
    /// </summary>
    [JsonProperty("accounts")]
    public CoinbaseAccount[] Accounts { get; set; }
}

public readonly struct CoinbaseAccount
{
    [JsonProperty("uuid")]
    public string Uuid { get; }

    [JsonProperty("name")]
    public string Name { get; }

    [JsonProperty("currency")]
    public string Currency { get; }

    [JsonProperty("available_balance")]
    public AvailableBalance AvailableBalance { get; }

    [JsonProperty("default")]
    public bool Default { get; }

    [JsonProperty("active")]
    public bool Active { get; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; }

    [JsonProperty("deleted_at")]
    public DateTime? DeletedAt { get; }

    [JsonProperty("type")]
    public string Type { get; }

    [JsonProperty("ready")]
    public bool Ready { get; }

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

public readonly struct AvailableBalance  
{
    [JsonProperty("value")]
    public decimal Value { get; }

    [JsonProperty("currency")]
    public string Currency { get; }

    [JsonConstructor]
    public AvailableBalance(decimal value, string currency)
    {
        Value = value;
        Currency = currency;
    }
}
