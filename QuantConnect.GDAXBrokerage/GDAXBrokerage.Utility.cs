/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
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

using QuantConnect.CoinbaseBrokerage.Models;
using BrokerageEnums = QuantConnect.CoinbaseBrokerage.Models.Enums;

namespace QuantConnect.Brokerages.GDAX
{
    /// <summary>
    /// Utility methods for GDAX brokerage
    /// </summary>
    public partial class GDAXBrokerage
    {
        private static Orders.OrderStatus ConvertOrderStatus(CoinbaseOrder order)
        {
            if (order.CompletionPercentage > 0 && order.CompletionPercentage != 100)
            {
                return Orders.OrderStatus.PartiallyFilled;
            }
            else if (order.Status == BrokerageEnums.OrderStatus.OPEN)
            {
                return Orders.OrderStatus.Submitted;
            }
            else if (order.Status == BrokerageEnums.OrderStatus.FILLED)
            {
                return Orders.OrderStatus.Filled;
            }

            return Orders.OrderStatus.None;
        }
    }
}
