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
                if (type == "Sell")
                {
                    foreach (dynamic bid in limits["Bids"])
                    {
                        OrderBookItem item = new OrderBookItem();
                        item.Exchange = lineNumber;
                        item.Amount = decimal.Parse(bid["Order"]["Amount"].ToString(), NumberStyles.Float);
                        item.Price = decimal.Parse(bid["Order"]["Price"].ToString(), NumberStyles.Float);
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
            decimal remainingAmount = amount;
            List<OrderBookItem> optimalTrades = new List<OrderBookItem>();
            foreach (OrderBookItem item in sortedList)
            {
                if (item.Amount  >= remainingAmount)
                {
                    OrderBookItem partialAmountItem = item;
                    partialAmountItem.Amount = remainingAmount;
                    optimalTrades.Add(item);
                    break;
                } else
                {
                    optimalTrades.Add(item);
                    remainingAmount -= item.Amount;
                }
            }
            return optimalTrades;
        }
    }
}
