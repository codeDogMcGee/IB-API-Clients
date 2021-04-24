using CsharpClient.IbApiLibrary;
using CSharpClient.ConsoleUI.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CSharpClient.ConsoleUI
{
    class Program
    {
        static void Main(string[] args)
        {
            IbClient ibClient = new IbClient();

            ibClient.ConnectToIb();

            var stocksJson = ibClient.GetMatchingStockSymbolsFromIB("MSFT");
            var stocks = JsonConvert.DeserializeObject<List<StockContractModel>>(stocksJson);

            int userChoiceMax = 0;

            string toStringHeaders = String.Format("{0,-15}{1,-15}{2,-7}{3,-15}{4}\n ", "Symbol ", "MainExch ", "Curr ", "ConId ", "SecType ");

        // show the user the choices

        Console.WriteLine("\n\nMatching Contracts:");
            // StockContractModelHeaders
            Console.WriteLine(String.Format("{0,-5}{1}", "", toStringHeaders));

            for (int i = 0; i < stocks.Count; i++)
            {
                Console.WriteLine(String.Format("{0,-5}{1}", i + 1, stocks[i]));
                userChoiceMax = i + 1;
            }


            // let user choose a contract

            Console.WriteLine("\nChoose a contract by entering the number.");

            int userChoice = 0;
            while (userChoice < 1)
            {
                int.TryParse(Console.ReadLine(), out userChoice);

                if (userChoice >= userChoiceMax || userChoice <= 0)
                {
                    userChoice = 0;
                    Console.WriteLine($"Choose a contract between 1 and {userChoiceMax}");
                }
            }

            StockContractModel userContract = stocks[userChoice - 1]; // -1 bc zero indexed but user is 1 indexed
            Console.WriteLine($"\n\n           {toStringHeaders}\nYou chose: {userContract}\n\n");

            ibClient.DisconnectIbSocket();

            Console.WriteLine("Done Processing");
            Console.ReadLine();
        }

        static void GetExecutions(IbClient ibClient)
        {
            string executionsJson = ibClient.GetAllExecutions();

            var executions = JsonConvert.DeserializeObject<List<ExecutionModel>>(executionsJson);

            foreach (ExecutionModel execution in executions)
            {
                Console.WriteLine(execution.ToString());
            }
        }
    }
}
