using CSharpClient.IbApiLibrary.Interfaces;
using CSharpClient.IbApiLibrary.Models;
using IBApi;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace CsharpClient.IbApiLibrary
{
    public class IbClient
    {
        private static IConfiguration _config;
        private static EWrapperImplementation _ibConnection;
        private static EClientSocket _clientSocket;
        private static EReaderSignal _readerSignal;
        private static EReader _reader;  //Create a reader to consume messages from the TWS. The EReader will consume the incoming messages and put them in a queue

        public IbClient()
        {
            InitializeConfiguration();

            _ibConnection = new EWrapperImplementation();
            _clientSocket = _ibConnection.ClientSocket;
            _readerSignal = _ibConnection._signal;
         

        }

        public bool IsConnected { get; private set; }
        public Dictionary<int, StockDataModel> StockData { get { return _ibConnection.StockData; } }

        public string GetMatchingStockSymbolsFromIB(string patternToMatch)
        {
            _clientSocket.reqMatchingSymbols(
                _ibConnection._reqIdMap["GetMatchingStockSymbolsFromIB"], 
                patternToMatch);

            List<StockContractModel> stocks = new List<StockContractModel>();

            // reqMatchingSymbols doesn't tell you when it's done
            // so wait a couple seconds and then see if there are results
            while (stocks.Count == 0)
            {
                Thread.Sleep(1000);
                stocks = _ibConnection.Stocks;

                // after retreiving the stocks from the Wrapper clear the Wrapper list
                _ibConnection.Stocks = new List<StockContractModel>();
            }

            return JsonConvert.SerializeObject(stocks);
        }

        public string GetAllExecutions()
        {
            _ibConnection._receivingExecutionsInProgress = true;
            _clientSocket.reqExecutions(
                _ibConnection._reqIdMap["GetAllExecutions"],
                new ExecutionFilter());

            while (_ibConnection._receivingExecutionsInProgress) { }  // wait for the receive exeutions to finish before proceeding

            List<Dictionary<string, string>> output = new List<Dictionary<string, string>>();
            foreach (var execution in _ibConnection._executions)
            {
                output.Add(ExecutionsModelToDictionary(execution.Value));
            }

            output = output.OrderBy(e => e["Time"]).ToList();

            return JsonConvert.SerializeObject(output);
        }

        public void RequestStreamingData(IStockContractModel stock)
        {
            Contract ibContract = new Contract
            {
                ConId = stock.ContractId,
                Symbol = stock.Symbol,
                SecType = stock.SecurityType,
                Currency = stock.Currency,
                Exchange = stock.Exchange,
                PrimaryExch = stock.PrimaryExchange
            };

            // initialize the new contract in StockData dict
            _ibConnection.StockData.Add(stock.ContractId, new StockDataModel { StockContract = stock, Data = new DataModel() });
            // request the data from TWS
            _clientSocket.reqMktData(stock.ContractId, ibContract, "221,233", false, false, null);
        }

        public void ConnectToIb()
        {
            int ibPort = _config.GetValue<int>("IbPort");
            int ibConnectionId = _config.GetValue<int>("IbConnectionId");

            _clientSocket.eConnect("127.0.0.1", ibPort, ibConnectionId);

            // after connecting to ib start the reader
            InitializeEReader();
            // then start the listening thread
            CreateSignalThread();
            WaitForIbToConnect();

            IsConnected = true;
        }

        private static void InitializeEReader()
        {
            _reader = new EReader(_clientSocket, _readerSignal);
            _reader.Start();
        }

        private static void CreateSignalThread()
        {
            //Once the messages are in the queue, an additional thread can be created to fetch them
            new Thread(() =>
            {
                while (_clientSocket.IsConnected())
                {
                    _readerSignal.waitForSignal();
                    _reader.processMsgs();
                }
            })
            {
                IsBackground = true
            }.Start();
        }

        private static void WaitForIbToConnect()
        {
            // monitor the order's nextValidId reception which comes down automatically after connecting.
            int timeoutSeconds = 10;
            DateTime timeoutTime = DateTime.Now + TimeSpan.FromSeconds(timeoutSeconds);


            while (_ibConnection.NextOrderId <= 0)
            {
                if (DateTime.Now > timeoutTime)
                {
                    throw new TimeoutException($"Could not connect to IB within {timeoutSeconds} seconds.");
                }
            }
        }

        public void DisconnectIbSocket()
        {
            _clientSocket.eDisconnect();
            IsConnected = false;
        }

        private string ToStringWithNulls(object value)
        {
            return value == null ? "" : value.ToString();
        }

        public static object GetPropertyValue(object sourceObject, string propertyName)
        {
            return sourceObject.GetType().GetProperty(propertyName).GetValue(sourceObject, null);
        }

        private Dictionary<string, string> ExecutionsModelToDictionary(ExecutionsModel execution)
        {
            
            Dictionary<string, string> outputDictionary = new Dictionary<string, string>();

            // iterate the properties of the ExecutionModel type to make a dictionary of all the child properties and their values in execution
            foreach (PropertyInfo parentProperty in execution.GetType().GetProperties())
            {
                var parentPropertyType = Nullable.GetUnderlyingType(parentProperty.PropertyType) ?? parentProperty.PropertyType;
                string parentPropertyName = parentProperty.Name;

                // create an object at the first layer of attributes (parent attributes) to be able to get the value of the child attributes
                object parentPropertyObject = GetPropertyValue(execution, parentPropertyName);

                foreach (PropertyInfo childProperty in parentPropertyType.GetProperties())
                {
                    string childPropertyName = childProperty.Name;

                    object childPropertyValue = GetPropertyValue(parentPropertyObject, childPropertyName);

                    if (outputDictionary.ContainsKey(childPropertyName) is false)
                    {
                        outputDictionary.Add(childPropertyName, ToStringWithNulls(childPropertyValue));
                    }
                }
            }
            
            return outputDictionary;
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
