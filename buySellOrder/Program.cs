using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace buySellOrder
{
    public class OrderBookItem
    {
        public int Exchange { get; set; }
        public string OrderType { get; set; }
        public decimal Amount { get; set; }
        public decimal Price { get; set; }
        public decimal BalanceOnExchange { get; set; }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            // error if amount is not int or negative

            // error if type not buy or sell
            Console.Write("Enter 'Buy' or 'Sell': ");
            string type = Console.ReadLine();
            Console.Write("Enter BTC amount: ");
            string number = Console.ReadLine();

            foreach (OrderBookItem trade in bestExecution(number, type))
            {
                Console.WriteLine(type + " " + trade.Amount.ToString() + " on exchange " + trade.Exchange);

            }
        }
        public static List<OrderBookItem> bestExecution(string number, string type)
        {
            decimal amount;
            bool success = decimal.TryParse(number, out amount);
            if (!success || amount <= 0)
            {
                throw new Exception("Amount is negative or in a wrong format");
            }
            if (type != "Buy" && type != "Sell")
            {
                throw new Exception("Type has to be one of the following: 'Buy', 'Sell'");
            }
            // error if amount is not int or negative

            // error if type not buy or sell

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"order_books_data.txt");
            string[] lines = System.IO.File.ReadAllLines(path);
            int lineNumber = 0;
            List<OrderBookItem> orders = new List<OrderBookItem>();
            foreach (string line in lines)
            {
                // the first column of line is a timestamp, and the second is the json with data
                string[] parts = line.Split();
                string limitsStr = parts[1];
                dynamic limits = JsonConvert.DeserializeObject(limitsStr);
                Random rnd = new Random();

                // for Sell and Buy orders, we generate a random BTC balance between 0 and 10 - the balance is always given in BTC
                decimal balanceOnExchange = decimal.Parse(rnd.Next(1000).ToString()) * Convert.ToDecimal(0.01);
                if (type == "Sell")
                {
                    foreach (dynamic bid in limits["Bids"])
                    {
                        OrderBookItem item = new OrderBookItem();
                        item.Exchange = lineNumber;
                        item.Amount = decimal.Parse(bid["Order"]["Amount"].ToString(), NumberStyles.Float);
                        item.Price = decimal.Parse(bid["Order"]["Price"].ToString(), NumberStyles.Float);
                        item.OrderType = type;
                        item.BalanceOnExchange = balanceOnExchange;
                        orders.Add(item);
                    }
                }
                else
                {
                    foreach (dynamic ask in limits["Asks"])
                    {
                        OrderBookItem item = new OrderBookItem();
                        item.Exchange = lineNumber;
                        item.Amount = decimal.Parse(ask["Order"]["Amount"].ToString(), NumberStyles.Float);
                        item.Price = decimal.Parse(ask["Order"]["Price"].ToString(), NumberStyles.Float);
                        item.OrderType = type;
                        item.BalanceOnExchange = balanceOnExchange;
                        orders.Add(item);
                    }

                }
                lineNumber += 1;
            }
            List<OrderBookItem> sortedList = new List<OrderBookItem>();
            if (type == "Sell") {
                // if selling, we want the price to be as high as possible
                sortedList = orders.OrderByDescending(o => o.Price).ToList();
            } else
            { // if buying, we aim for lower prices
                sortedList = orders.OrderBy(o => o.Price).ToList();
            }

            return getItemsFromOrderBooks(amount, sortedList);
     
        }


        public static List<OrderBookItem> getItemsFromOrderBooks(decimal amount, List<OrderBookItem> sortedList)
        {
            // remaining amount - in BTC
            decimal remainingAmount = amount;
            List<OrderBookItem> optimalTrades = new List<OrderBookItem>();
            foreach (OrderBookItem item in sortedList)
            {
                // if some item from the current exchange was already executed, we need to update the balance for the current one as well
                // this could be done later, but we have to iterate only through optimalTrades if we do it before
                decimal lastBalanceOnExchange = item.BalanceOnExchange;
                foreach (OrderBookItem alreadyAddedItem in optimalTrades)
                {
                    if (alreadyAddedItem.Exchange == item.Exchange)
                    {
                        item.BalanceOnExchange = alreadyAddedItem.BalanceOnExchange;
                    }
                }

                if (item.BalanceOnExchange == 0) // skip if balance is zero
                {
                    continue;
                }
                if (item.Amount  >= remainingAmount)
                { // this is the last order to be filled (it's big enough so that the whole remaining amount can be bought/sold)
                    OrderBookItem partialAmountItem = item;
                    if (item.BalanceOnExchange >= item.Amount)
                    { // there is enough money on account to complete current order
                        partialAmountItem.Amount = remainingAmount;
                        remainingAmount = 0;
                        optimalTrades.Add(partialAmountItem);
                        break; // this is the ideal scenario, so on the end of it, the remaining amount should be 0
                    } else
                    { // there isn't enough money, so we just partially fill the order
                        partialAmountItem.Amount = item.BalanceOnExchange;
                        remainingAmount -= item.BalanceOnExchange; 
                        partialAmountItem.BalanceOnExchange = 0; // update balance
                        optimalTrades.Add(partialAmountItem);
                    }
                    
                }
                else // the amount to be bought/sold is bigger than the optimal order, so we will have to execute at least another one after that - we take the whole amount on it
                {
                    if (item.BalanceOnExchange >= item.Amount)
                    { // we have more on the exchange than we want to buy or sell - no problem
                        item.BalanceOnExchange -= item.Amount; // update balance
                        optimalTrades.Add(item);
                        remainingAmount -= item.Amount;
                    } else// we have less on the exchange than we want to buy or sell - we only take what we can
                    {
                        item.Amount = item.BalanceOnExchange;
                        item.BalanceOnExchange = 0; // update balance
                        optimalTrades.Add(item);
                        remainingAmount -= item.BalanceOnExchange;
                    }
                        
                }
            }
            return optimalTrades;
        }
    }
}
