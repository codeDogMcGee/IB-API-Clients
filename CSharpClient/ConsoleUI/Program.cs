using ConsoleUI.Models;
using IbApiLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ConsoleUI
{
    class Program
    {
        static void Main(string[] args)
        {
            IbClient ibClient = new IbClient();

            ibClient.ConnectToIb();

            string executionsJson = ibClient.GetAllExecutions();

            var executions = JsonConvert.DeserializeObject<List<ExecutionModel>>(executionsJson);

            foreach (ExecutionModel execution in executions)
            {
                Console.WriteLine(execution.ToString());
            }

            ibClient.DisconnectIbSocket();

            Console.WriteLine("Done Processing");
            Console.ReadLine();
        }
    }
}
