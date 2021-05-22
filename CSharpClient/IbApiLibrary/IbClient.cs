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
using System.Threading;
using System.Threading.Tasks;

namespace IbApiLibrary
{
    public class IbClient
    {
        // Fields
        private static readonly NLog.Logger _logger = NLog.LogManager.GetLogger("MainLog");
        
        private static IConfiguration _config;
        private static EWrapperImplementation _ibConnection;
        private static EClientSocket _clientSocket;
        private static EReaderSignal _readerSignal;
        private static EReader _reader;  //Create a reader to consume messages from the TWS. The EReader will consume the incoming messages and put them in a queue

        private readonly string _account;
        private readonly int _port;
        private readonly int _connectionId;

        // Constructor
        public IbClient()
        {
            InitializeConfiguration();

            _account = _config.GetValue<string>("AccountNumber");
            _port = _config.GetValue<int>("IbPort");
            _connectionId = _config.GetValue<int>("IbConnectionId");

            _ibConnection = new EWrapperImplementation(_connectionId);
            _clientSocket = _ibConnection._clientSocket;
            _readerSignal = _ibConnection._signal;

        }

        // Properties
        public bool IsConnected { get; private set; }
        public Dictionary<int, StockDataModel> StockData => _ibConnection.StockData;
        public long LastOrderSentTimestamp { get; private set; }
        public List<int> OpenOrderIds 
        {
            get
            {
                if (_ibConnection._openOrders.Keys.Count > 0)
                {
                    return _ibConnection._openOrders.Keys.ToList<int>();
                }
                else
                {
                    return new List<int>();
                }
            } 
        }

        // Public Methods
        public int IsOrderCorrectSize(int orderId, double sizeItShoudBe)
        {
            // returns -1 for false, 1 for true, and 0 for unknown
            int output = 0;

            if (_ibConnection._openOrders.ContainsKey(orderId))
            {
                double total = _ibConnection._openOrders[orderId].Order.TotalQuantity;
                //double filled = _ibConnection._openOrders[orderId].Order.FilledQuantity;

                if (Math.Round(total, 4) == Math.Round(sizeItShoudBe, 4))
                {
                    output = 1;
                }
                else
                {
                    output = -1;
                }
            }
            return output;
        }

        public async Task<int> PlaceLimitOrderAsync(IStockContractModel stockContract, string buyOrSell, double orderQuantity, double limitPrice, int ocaType = 2, string ocaGroupName = "")
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
                OrderType = "LMT",
                TotalQuantity = orderQuantity,
                LmtPrice = limitPrice,
                OcaType = ocaType,
                OcaGroup = ocaGroupName,
                Transmit = true
            };

            int orderId = _ibConnection._orderId++;
            _clientSocket.placeOrder(orderId, contract, order);

            LastOrderSentTimestamp = GetUtcTimeStamp();

            var output = await WaitForOrderAck(orderId);

            _logger.Info("Place LMT order for {Account}: Symbol={Symbol} Exchange={Exchange} Action={Action} Qty={Quantity} Price={LimitPrice} OrderId={OrderId}",
                order.Account,
                stockContract.Symbol,
                stockContract.Exchange,
                order.Action,
                order.TotalQuantity,
                order.LmtPrice,
                orderId);

            
            return output;
        }

        public async Task<int> PlaceTrailingStopOrderAsync(IStockContractModel stockContract, string buyOrSell, double orderQuantity, double trailingAmount, double trailingStopPrice, double limitPriceOffset, int ocaType = 2, string ocaGroupName = "")
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
                Transmit = true
            };

            int orderId = _ibConnection._orderId++;
            _clientSocket.placeOrder(orderId, contract, order);

            LastOrderSentTimestamp = GetUtcTimeStamp();

            var output = await WaitForOrderAck(orderId);

            _logger.Info("Place TRAIL LMT order for {Account}: Symbol={Symbol} Exchange={Exchange} Action={Action} Qty={Quantity} StopPrice={StopPrice} TrailingAmt={TrailAmount} LimitOffset={LmtOffset} OrderId={OrderId}",
                order.Account,
                stockContract.Symbol,
                stockContract.Exchange,
                order.Action,
                order.TotalQuantity,
                order.TrailStopPrice,
                order.AuxPrice,
                order.LmtPriceOffset,
                orderId);

            return output;
        }

        public int PlaceLimitOrder(IStockContractModel stockContract, string buyOrSell, double orderQuantity, double limitPrice, int ocaType = 2, string ocaGroupName = "")
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
                OrderType = "LMT",
                TotalQuantity = orderQuantity,
                LmtPrice = limitPrice,
                OcaType = ocaType,
                OcaGroup = ocaGroupName,
                Transmit = true
            };

            int orderId = _ibConnection._orderId++;
            _clientSocket.placeOrder(orderId, contract, order);
            _logger.Info("Place LMT order for {Account}: Symbol={Symbol} Exchange={Exchange} Action={Action} Qty={Quantity} Price={LimitPrice} OrderId={OrderId}",
                order.Account,
                stockContract.Symbol,
                stockContract.Exchange,
                order.Action,
                order.TotalQuantity,
                order.LmtPrice,
                orderId);

            LastOrderSentTimestamp = GetUtcTimeStamp();
            return orderId;
        }

        public int PlaceTrailingStopOrder(IStockContractModel stockContract, string buyOrSell, double orderQuantity, double trailingAmount, double trailingStopPrice, double limitPriceOffset, int ocaType = 2, string ocaGroupName = "")
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
                Transmit = true
            };

            int orderId = _ibConnection._orderId++;
            _clientSocket.placeOrder(orderId, contract, order);
            _logger.Info("Place TRAIL LMT order for {Account}: Symbol={Symbol} Exchange={Exchange} Action={Action} Qty={Quantity} StopPrice={StopPrice} TrailingAmt={TrailAmount} LimitOffset={LmtOffset} OrderId={OrderId}",
                order.Account,
                stockContract.Symbol, 
                stockContract.Exchange,
                order.Action,
                order.TotalQuantity,
                order.TrailStopPrice,
                order.AuxPrice,
                order.LmtPriceOffset,
                orderId);

            LastOrderSentTimestamp = GetUtcTimeStamp();
            return orderId;
        }

        public void ModifyTrailingStopOrderPrice(int orderId, double trailingAmount)
        {
            // get the existing order and only modify the pricing parameters, then resend
            OrderModel order = _ibConnection._openOrders[orderId];

            order.Order.AuxPrice = trailingAmount;
            order.Order.LmtPrice = 0; // Must remove LmtPrice setting since LmtPriceOffset already has a value. Can't set both.

            _clientSocket.placeOrder(orderId, order.Contract, order.Order);
            _logger.Info("Modify TRAIL LMT order for {Account}: Symbol={Symbol} Exchange={Exchange} Action={Action} Qty={Quantity} StopPrice={StopPrice} TrailingAmt={TrailAmount} LimitOffset={LmtOffset} OrderId={OrderId}",
                order.Order.Account,
                order.Contract.Symbol,
                order.Contract.Exchange,
                order.Order.Action,
                order.Order.TotalQuantity,
                order.Order.TrailStopPrice,
                order.Order.AuxPrice,
                order.Order.LmtPriceOffset,
                orderId);

            LastOrderSentTimestamp = GetUtcTimeStamp();
        }

        public void ModifyTrailingStopOrderQuantity(int orderId, double newQuantity)
        {
            // get the existing order and only modify the pricing parameters, then resend
            OrderModel order = _ibConnection._openOrders[orderId];

            order.Order.TotalQuantity = newQuantity;
            order.Order.LmtPrice = 0; // Must remove LmtPrice setting since LmtPriceOffset already has a value. Can't set both.

            _clientSocket.placeOrder(orderId, order.Contract, order.Order);
            _logger.Info("Modify TRAIL LMT order for {Account}: Symbol={Symbol} Exchange={Exchange} Action={Action} Qty={Quantity} StopPrice={StopPrice} TrailingAmt={TrailAmount} LimitOffset={LmtOffset} OrderId={OrderId}",
                order.Order.Account,
                order.Contract.Symbol,
                order.Contract.Exchange,
                order.Order.Action,
                order.Order.TotalQuantity,
                order.Order.TrailStopPrice,
                order.Order.AuxPrice,
                order.Order.LmtPriceOffset,
                orderId);

            LastOrderSentTimestamp = GetUtcTimeStamp();
        }

        public int CancelOrderById(int orderId)
        {
            _clientSocket.cancelOrder(orderId);
            _logger.Info("Canceled order {OrderId}", orderId);
            return -1;
        }

        public async Task<int> CancelOrderByIdAsync(int orderId)
        {
            _clientSocket.cancelOrder(orderId);
            var output = await WaitForOrderCancelAck(orderId);
            _logger.Info("Canceled order {OrderId}", orderId);
            return output;
        }

        public async void CancelAllOrdersAsync()
        {
            List<int> canceledOrders = new List<int>();

            // first send all of the cancels to the api
            foreach (int orderId in _ibConnection._openOrders.Keys)
            {
                _clientSocket.cancelOrder(orderId);
                canceledOrders.Add(orderId);
            }

            // then wait for the cancel confirmations
            foreach (int canceledOrder in canceledOrders)
            {
                _ = await WaitForOrderCancelAck(canceledOrder);
                _logger.Info("Canceled order {OrderId}", canceledOrder);
            }
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
            _ibConnection.ReceivingExecutionsInProgress = true;
            _clientSocket.reqExecutions(
                _ibConnection._reqIdMap["GetAllExecutions"],
                new ExecutionFilter());

            while (_ibConnection.ReceivingExecutionsInProgress) { }  // wait for the receive exeutions to finish before proceeding

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

            _logger.Info("Connected to IB TWS host: {host}, port: {ibPort}, ConnectionId: {ibConnectionId}", host, _port, _connectionId);
        }

        public void DisconnectIbSocket()
        {
            _clientSocket.eDisconnect();
            IsConnected = false;
            _logger.Info("Disconnected from IB TWS");
        }

        public void RequestAccountUpdates()
        {
            string accountNumber = _config.GetValue<string>("AccountNumber");
            _clientSocket.reqAccountUpdates(true, accountNumber);
            _logger.Debug("Requesting account updates for {Account}", accountNumber);
            while (_ibConnection.AccountDataFinishedDownloading is false) { }
        }

        public void RequestOpenOrders()
        {
            _ibConnection.ReceivingOpenOrdersInProgress = true;
            _clientSocket.reqOpenOrders();
            _logger.Debug("Requesting open orders.");
            while (_ibConnection.ReceivingOpenOrdersInProgress) { }
        }

        public long GetUtcTimeStamp()
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
            return dateTimeOffset.ToUnixTimeSeconds();
        }

        // Private Methods
        private Task<int> WaitForOrderAck(int orderId)
        {
            int timeoutSeconds = 3;

            long startTime = GetUtcTimeStamp();
            long timeoutTimestamp = startTime + timeoutSeconds;

            while (_ibConnection._openOrders.Count == 0 || _ibConnection._openOrders.ContainsKey(orderId) is false)
            {
                long nowTimestamp = GetUtcTimeStamp();
                if (nowTimestamp > timeoutTimestamp)
                {
                    _logger.Error("IB WaitForOrderAck for {SecondsWaiting} seconds", nowTimestamp);
                    Thread.Sleep(1000);
                }
            }

            return Task.FromResult(orderId);
        }

        private Task<int> WaitForOrderCancelAck(int orderId)
        {
            int timeoutSeconds = 3;

            long startTime = GetUtcTimeStamp();
            long timeoutTimestamp = startTime + timeoutSeconds;

            while (_ibConnection._openOrders.ContainsKey(orderId) is true)
            {
                long nowTimestamp = GetUtcTimeStamp();
                if (nowTimestamp > timeoutTimestamp)
                {
                    _logger.Error("IB WaitForOrderCancelAck for {SecondsWaiting} seconds", nowTimestamp);
                    Thread.Sleep(1000);
                }
            }

            return Task.FromResult(-1);
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
