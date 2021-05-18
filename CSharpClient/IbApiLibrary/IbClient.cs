using IbApiLibrary.Interfaces;
using IbApiLibrary.Models;
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

namespace IbApiLibrary
{
    public class IbClient
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetLogger("MainLog");
        private readonly string _account;
        private readonly int _port;
        private readonly int _connectionId;
        private static IConfiguration _config;
        private static EWrapperImplementation _ibConnection;
        private static EClientSocket _clientSocket;
        private static EReaderSignal _readerSignal;
        private static EReader _reader;  //Create a reader to consume messages from the TWS. The EReader will consume the incoming messages and put them in a queue

        public IbClient()
        {
            InitializeConfiguration();

            _ibConnection = new EWrapperImplementation();
            _clientSocket = _ibConnection._clientSocket;
            _readerSignal = _ibConnection._signal;
            _account = _config.GetValue<string>("AccountNumber");
            _port = _config.GetValue<int>("IbPort");
            _connectionId = _config.GetValue<int>("IbConnectionId");
        }

        public bool IsConnected { get; private set; }
        public Dictionary<int, StockDataModel> StockData { get { return _ibConnection.StockData; } }

        public void PlaceTrailingStopOrder(IStockContractModel stockContract, string buyOrSell, double orderQuantity, double trailingAmount, double trailingStopPrice, double limitPriceOffset, int ocaType = 2, string ocaGroupName = "")
        {
            Contract contract = new Contract
            {
                ConId = stockContract.ContractId,
                Symbol = stockContract.Symbol,
                SecIdType = stockContract.SecurityType,
                Exchange = stockContract.Exchange,
                PrimaryExch = stockContract.PrimaryExchange,
                Currency = stockContract.Currency
            };

            Order order = new Order
            {
                Account = _account,
                Action = buyOrSell.ToUpper(),
                OrderType = "TRAIL LIMIT",
                TotalQuantity = orderQuantity,
                TrailStopPrice = trailingStopPrice,
                LmtPriceOffset = limitPriceOffset,
                AuxPrice = trailingAmount,
                OcaType = ocaType,
                OcaGroup = ocaGroupName,
                Transmit = false
            };

            int orderId = _ibConnection._orderId++;
            _clientSocket.placeOrder(orderId, contract, order);
            _logger.Info("Placed TRAIL LMT order for {Account}: {Symbol} {Exchange} {Action} {Quantity} {StopPrice} {TrailAmount} {LmtOffset} {OrderId}",
                order.Account,
                stockContract.Symbol, 
                stockContract.Exchange,
                order.Action,
                order.TotalQuantity,
                order.TrailStopPrice,
                order.AuxPrice,
                order.LmtPriceOffset,
                orderId);
        }

        public void CancelWorkingOrder(int orderId)
        {
            _clientSocket.cancelOrder(orderId);
        }

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
                stocks = _ibConnection._symbolLookupStocks;

                // after retreiving the stocks from the Wrapper clear the Wrapper list
                _ibConnection._symbolLookupStocks = new List<StockContractModel>();
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

            
            // entry may exist if loaded from positions upon connection
            if (_ibConnection.StockData.ContainsKey(stock.ContractId) is false)
            {
                // initialize the new contract in StockData dict
                _ibConnection.StockData.Add(stock.ContractId, new StockDataModel { StockContract = stock, Data = new DataModel() });
            }
            
            // request the data from TWS
            _clientSocket.reqMktData(stock.ContractId, ibContract, "221,233", false, false, null);
        }

        public void ConnectToIb()
        {
            string host = "127.0.0.1";

            _clientSocket.eConnect(host, _port, _connectionId);

            // after connecting to ib start the reader
            InitializeEReader();
            // then start the listening thread
            CreateSignalThread();
            WaitForIbToConnect();

            IsConnected = true;

            _logger.Info("Connected to IB TWS host: {host}, port: {ibPort}, ConnectionId: {ibConnectionId}", host, ibPort, ibConnectionId);
        }

        public void DisconnectIbSocket()
        {
            _clientSocket.eDisconnect();
            IsConnected = false;
            _logger.Info("Disconnected from IB TWS");
        }

        public void RequestAccountUpdates()
        {
            _clientSocket.reqAccountUpdates(true, _config.GetValue<string>("AccountNumber"));
            
            while (_ibConnection.AccountDataFinishedDownloading is false) { }
        }

        private static void RequestOpenOrders()
        {
            _clientSocket.reqOpenOrders();
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


            while (_ibConnection._orderId <= 0)
            {
                if (DateTime.Now > timeoutTime)
                {
                    throw new TimeoutException($"Could not connect to IB within {timeoutSeconds} seconds.");
                }
            }
        }

        private string ToStringWithNulls(object value)
        {
            return value == null ? "" : value.ToString();
        }

        private static object GetPropertyValue(object sourceObject, string propertyName)
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
