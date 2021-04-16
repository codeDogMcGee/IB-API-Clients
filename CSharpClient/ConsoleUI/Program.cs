using DataAccessLibrary;
using DataAccessLibrary.Models;
using IBApi;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ConsoleUI
{
    class Program
    {
        private static EWrapperImplementation _ibConnection;
        private static IConfiguration _config;
        private static CsvFileDataAccess _csv;

        public static int Main(string[] args)
        {
            InitializeConfiguration();

            _csv = new CsvFileDataAccess(_config.GetValue<string>("TradesCsv")); // TradesCsv is from appsettings.json

            _ibConnection = new EWrapperImplementation();

            EClientSocket clientSocket = _ibConnection.ClientSocket;
            EReaderSignal readerSignal = _ibConnection.Signal;

            int ibPort = _config.GetValue<int>("IbPort");
            int ibConnectionId = _config.GetValue<int>("IbConnectionId");

            clientSocket.eConnect("127.0.0.1", ibPort, ibConnectionId);
       
            //Create a reader to consume messages from the TWS. The EReader will consume the incoming messages and put them in a queue
            var reader = new EReader(clientSocket, readerSignal);
            reader.Start();
       
            //Once the messages are in the queue, an additional thread can be created to fetch them
            new Thread(() => 
            { 
                while (clientSocket.IsConnected()) 
                { 
                    readerSignal.waitForSignal(); 
                    reader.processMsgs(); 
                }   
            }) 
            { 
                IsBackground = true 
            }.Start();
            
            // One (although primitive) way of knowing if we can proceed is by monitoring the order's nextValidId reception which comes down automatically after connecting.
            while (_ibConnection.NextOrderId <= 0) { }

            runIbMethods(clientSocket);

            Console.WriteLine("Disconnecting...");
            
            clientSocket.eDisconnect();
            return 0;
        }
      
        private static void runIbMethods(EClientSocket client)
        {
            GetTrades(client);

            Thread.Sleep(3000); //  need to use execDetailsEnd() to signal end of data

            WriteAllTradesToCsv(_ibConnection.Trades);

            Console.WriteLine("Done Processing");

            Thread.Sleep(10000);
        }

        private static void GetTrades(EClientSocket client)
        {
            client.reqExecutions(10001, new ExecutionFilter());
        }

        private static void WriteAllTradesToCsv(Dictionary<string, TradesModel> trades)
        {
            _csv.WriteTradesToCsv(trades);
        }

        private static void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            _config = builder.Build();
        }
    }
}
