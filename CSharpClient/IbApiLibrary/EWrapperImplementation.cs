using IbApiLibrary.Interfaces;
using IbApiLibrary.Models;
using IBApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IbApiLibrary
{
    public class EWrapperImplementation : EWrapper
    {
        // Fields
        private static readonly NLog.Logger _logger = NLog.LogManager.GetLogger("MainLog");
        private static readonly NLog.Logger _fillsLogger = NLog.LogManager.GetLogger("FillsLog");

        internal readonly EReaderSignal _signal;
        internal readonly EClientSocket _clientSocket;

        internal int _orderId = -1;

        internal readonly Dictionary<string, int> _reqIdMap = new Dictionary<string, int> 
        {
            { "GetAllExecutions", 20001 },
            { "GetMatchingStockSymbolsFromIB", 20002 }
        };

        internal readonly Dictionary<int, StockDataModel> StockData = new Dictionary<int, StockDataModel>();

        internal readonly AccountDataModel _accountData = new AccountDataModel();
        internal Dictionary<string, ExecutionsModel> _executions = new Dictionary<string, ExecutionsModel>();
        internal Dictionary<int, OrderModel> _openOrders = new Dictionary<int, OrderModel>();
        internal List<StockContractModel> _symbolLookupStocks = new List<StockContractModel>();
        private readonly int _clientId;

        // Constructor
        public EWrapperImplementation(int clientId)
        {
            _signal = new EReaderMonitorSignal();
            _clientSocket = new EClientSocket(this, _signal);
            _clientId = clientId;
        }

        // Properties
        internal DateTime LastAccountUpdateTime { get; private set; } = new DateTime(1, 1, 1);
        internal bool AccountDataFinishedDownloading { get; private set; } = false;
        internal bool ReceivingExecutionsInProgress { get; set; } = false;
        internal bool ReceivingOpenOrdersInProgress { get; set; } = false;

        // Methods
        public void orderStatus(int orderId, string status, double filled, double remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice)
        {
            if (clientId == _clientId)
            {
                if (_openOrders.ContainsKey(orderId))
                {
                    // Update the open order's status

                    if (status == "Cancelled" || status == "Closed" || status == "Filled")
                    {
                        _openOrders.Remove(orderId);
                    }
                    else
                    {
                        OrderStatusModel orderStatus = _openOrders[orderId].Status;
                        orderStatus.Status = status;
                        orderStatus.Filled = filled;
                        orderStatus.Remaining = remaining;
                        orderStatus.AvgFillPrice = avgFillPrice;
                        orderStatus.PermId = permId;
                        orderStatus.ParentId = parentId;
                        orderStatus.LastFillPrice = lastFillPrice;
                        orderStatus.ClientId = clientId;
                        orderStatus.WhyHeld = whyHeld;
                        orderStatus.MktCapPrice = mktCapPrice;
                    }
                }
            }
        }

        public void openOrder(int orderId, Contract contract, Order order, OrderState orderState)
        {
            OrderStatusModel orderStatus = new OrderStatusModel
            {
                Status = orderState.Status
            };

            OrderModel newOrder = new OrderModel
            {
                Contract = contract,
                Order = order,
                Status = orderStatus
            };

            if (_openOrders.ContainsKey(orderId) is false)
            {
                _openOrders.Add(orderId, newOrder);
            }    
        }

        public void openOrderEnd()
        {
            ReceivingOpenOrdersInProgress = false;
        }

        public void updateAccountValue(string key, string value, string currency, string accountName)
        {
            // Account level statistics
            AccountDataFinishedDownloading = false;

            List<string> possibleCurrencyValues = new List<string> { "BASE", "USD" };

            // currency value can be null, BASE, or USD only
            if ( currency != null && !possibleCurrencyValues.Contains(currency) )
            {
                throw new Exception($"updateAccountValue(): Unhandled account currency exception in API Wrapper: {currency}");
            }

            switch (key)
            {
                case "AccountCode":
                    _accountData.AccountId = value;
                    break;
                case "AvailableFunds":
                    _accountData.AvailableFunds = value;
                    break;
                case "Currency":
                    _accountData.Currency = value;
                    break;
                case "InitMarginReq":
                    _accountData.InitialMarginReq = value;
                    break;
                case "GrossPositionValue":
                    _accountData.GrossPositionsValue = value;
                    break;
                case "NetLiquidation":
                    _accountData.NetLiquidationValue = value;
                    break;
                case "RealizedPnL":
                    _accountData.RealizedPnL = value;
                    break;
                case "UnrealizedPnL":
                    _accountData.UnrealizedPnL = value;
                    break;
                default:
                    // ignore if not being handled
                    break;
            }
        }

        public void updatePortfolio(Contract contract, double position, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, string accountName)
        {
            // Individual position level statistics
            if (StockData.ContainsKey(contract.ConId))
            {
                StockData[contract.ConId].Data.Position = position;
                StockData[contract.ConId].Data.AverageCost = averageCost;
                StockData[contract.ConId].Data.UnrealizedPnL = unrealizedPNL;
                StockData[contract.ConId].Data.RealizedPnL = realizedPNL;
                StockData[contract.ConId].Data.AccountValueMarkPrice = marketPrice;
            }
            else
            {
                DataModel data = new DataModel
                {
                    Position = position,
                    AverageCost = averageCost,
                    UnrealizedPnL = unrealizedPNL,
                    RealizedPnL = realizedPNL,
                    AccountValueMarkPrice = marketPrice
                };

                IStockContractModel stockContract = new StockContractModel
                {
                    ContractId = contract.ConId,
                    Currency = contract.Currency,
                    Exchange = string.IsNullOrWhiteSpace(contract.Exchange) ? "SMART" : contract.Exchange,
                    PrimaryExchange = contract.PrimaryExch,
                    SecurityType = contract.SecType,
                    Symbol = contract.Symbol
                };

                StockData[contract.ConId] = new StockDataModel { StockContract = stockContract, Data = data };
            }


        }

        public void updateAccountTime(string timestamp)
        {
            if (!string.IsNullOrWhiteSpace(timestamp))
            {
                LastAccountUpdateTime = DateTime.Parse(timestamp);
            }
        }

        public void accountDownloadEnd(string account)
        {
            AccountDataFinishedDownloading = true;
        }

        public void execDetails(int reqId, Contract contract, Execution execution)
        {
            if (reqId == _reqIdMap["GetAllExecutions"] || execution.ClientId == _clientId)
            {
                ExecutionsModel trade = new ExecutionsModel
                {
                    Execution = execution,
                    Contract = contract,
                    CommissionReport = new CommissionReport()
                };

                _executions.Add(execution.ExecId, trade);
            }
        }

        public void execDetailsEnd(int reqId)
        {
            if (reqId == _reqIdMap["GetAllExecutions"])
            {
                ReceivingExecutionsInProgress = false;
            }
        }

        public void commissionReport(CommissionReport commissionReport)
        {
            if (_executions.Keys.Contains<string>(commissionReport.ExecId))
            {
                // get the current trade that has an empty commission report
                ExecutionsModel trade = _executions[commissionReport.ExecId];

                // update the trade with the commission report
                trade.CommissionReport = commissionReport;

                // reinsert the trade into the dictionary
                _executions[commissionReport.ExecId] = trade;

                // Add a header row to the fills log
                if (_executions.Count == 1)
                {
                    _fillsLogger.Debug("Symbol,Side,CumulativeQty,AveragePrice,Commission,RealizedPnL,AccountNumber,SecurityType,Currency,Exchange,ClientId,ExecutionId,OrderId");
                }

                _fillsLogger.Debug("{Symbol},{Side},{CumulativeQty},{AveragePrice},{Commission},{RealizedPnL},{AccountNumber},{SecurityType},{Currency},{Exchange},{ClientId},{ExecutionId},{OrderId}",
                    trade.Contract.Symbol,
                    trade.Execution.Side,
                    trade.Execution.CumQty,
                    trade.Execution.AvgPrice,
                    trade.CommissionReport.Commission,
                    trade.CommissionReport.RealizedPNL,
                    trade.Execution.AcctNumber,
                    trade.Contract.SecType,
                    trade.CommissionReport.Currency,
                    trade.Execution.Exchange,
                    trade.Execution.ClientId,
                    trade.Execution.ExecId,
                    trade.Execution.OrderId
                );
            }

        }

        public void symbolSamples(int reqId, ContractDescription[] contractDescriptions)
        {
            // more detail available at https://interactivebrokers.github.io/tws-api/matching_symbols.html
            // this method starts out pretty simple

            if (reqId == _reqIdMap["GetMatchingStockSymbolsFromIB"])
            {
                foreach (var contractDescription in contractDescriptions)
                {
                    if (contractDescription.Contract.SecType == "STK")
                    {
                        StockContractModel stock = new StockContractModel
                        {
                            ContractId = contractDescription.Contract.ConId,
                            Symbol = contractDescription.Contract.Symbol,
                            SecurityType = contractDescription.Contract.SecType,
                            Exchange = contractDescription.Contract.Exchange,
                            Currency = contractDescription.Contract.Currency,
                            PrimaryExchange = contractDescription.Contract.PrimaryExch
                        };

                        _symbolLookupStocks.Add(stock);
                    }
                }
            }

           
        }

        public void error(Exception e)
        {
            _logger.Error(e);
            throw e;
        }

        public void error(string str)
        {
            _logger.Error("TWS ERROR: ErrorMsg={errorMsg}", str);
        }

        public void error(int id, int errorCode, string errorMsg)
        {
            List<int> warningCodes = new List<int> { 399, 202 };

            if (id == -1)
            {
                _logger.Info($"TWS Connection Info: {errorCode} {errorMsg}");
            }
            else if (warningCodes.Contains(errorCode))
            {
                _logger.Warn("{ErrorCode} {ErrorId} {ErrorMessage}", errorCode, id, errorMsg);
            }
            else
            {
                _logger.Error("TWS ERROR: id={id}, ErrorCode={errorCode}, ErrorMsg={errorMsg}", id, errorCode, errorMsg);
            }

        }

        public void connectionClosed()
        {
            Console.WriteLine("Connection closed.\n");
        }

        public void connectAck()
        {
            Console.WriteLine("Connection Ack.\n");
        }

        public void currentTime(long time)
        {
            Console.WriteLine("Current Time: " + time + "\n");
        }

        public void tickPrice(int tickerId, int field, double price, TickAttrib attribs)
        {
            //Console.WriteLine("Tick Price. Ticker Id:" + tickerId + ", Field: " + field + ", Price: " + price + ", CanAutoExecute: " + attribs.CanAutoExecute +
            //    ", PastLimit: " + attribs.PastLimit + ", PreOpen: " + attribs.PreOpen);

            // reference https://interactivebrokers.github.io/tws-api/tick_types.html

            switch (field)
            {
                case 1:
                    StockData[tickerId].Data.BidPrice = price;
                    break;
                case 2:
                    StockData[tickerId].Data.AskPrice = price;
                    break;
                case 4:
                    StockData[tickerId].Data.LastPrice = price;
                    break;
                case 6:
                    StockData[tickerId].Data.DailyHighPrice = price;
                    break;
                case 7:
                    StockData[tickerId].Data.DailyLowPrice = price;
                    break;
                case 9:
                    StockData[tickerId].Data.PreviousClosePrice = price;
                    break;
                case 14:
                    StockData[tickerId].Data.OpenPrice = price;
                    break;
                case 37:
                    StockData[tickerId].Data.MarkPrice = price;
                    break;
                default:
                    // ignore if not being handled
                    break;
            }

        }

        public void tickSize(int tickerId, int field, int size)
        {
            switch (field)
            {
                case 0:
                    StockData[tickerId].Data.BidSize = size;
                    break;
                case 3:
                    StockData[tickerId].Data.AskSize = size;
                    break;
                case 5:
                    StockData[tickerId].Data.LastSize = size;
                    break;
                case 8:
                    StockData[tickerId].Data.DailyVolume = size;
                    break;
                default:
                    // ignore if not being handled
                    break;
            }
        }

        public void nextValidId(int orderId)
        {
            _orderId = orderId;
        }

        public void managedAccounts(string accountsList)
        {
            _logger.Debug("Managed Accounds:{accountsList}", accountsList);
        }


        public void tickString(int tickerId, int tickType, string value)
        {
            //Console.WriteLine("Tick string. Ticker Id:" + tickerId + ", Type: " + tickType + ", Value: " + value);
            // ignore for now
        }

        public void tickGeneric(int tickerId, int field, double value)
        {
            if (field == 49 && value > 0)
            {
                throw new Exception($"StockHaltedException: {tickerId} {value}");
            }

            // otherwise ignore

        }

        public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions)
        {
            _logger.Debug($"Tick Request Params: TickerId={tickerId}, MinTick={minTick}, BBOExchange={bboExchange}, SpapshotPermissions={snapshotPermissions}");

        }

        public void marketDataType(int reqId, int marketDataType)
        {
            _logger.Debug($"Market Data Type: ReqId={reqId} Type={marketDataType}", reqId, marketDataType);

        }

        public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate)
        {
            throw new NotImplementedException();
        }

        public void deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract)
        {
            throw new NotImplementedException();
        }

        public void tickOptionComputation(int tickerId, int field, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice)
        {
            throw new NotImplementedException();
        }

        public void tickSnapshotEnd(int tickerId)
        {
            throw new NotImplementedException();
        }

        public void accountSummary(int reqId, string account, string tag, string value, string currency)
        {
            throw new NotImplementedException();
        }

        public void accountSummaryEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void bondContractDetails(int reqId, ContractDetails contract)
        {
            throw new NotImplementedException();
        }

        public void contractDetails(int reqId, ContractDetails contractDetails)
        {
            throw new NotImplementedException();
        }

        public void contractDetailsEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void fundamentalData(int reqId, string data)
        {
            throw new NotImplementedException();
        }

        public void historicalData(int reqId, Bar bar)
        {
            throw new NotImplementedException();
        }

        public void historicalDataUpdate(int reqId, Bar bar)
        {
            throw new NotImplementedException();
        }

        public void historicalDataEnd(int reqId, string start, string end)
        {
            throw new NotImplementedException();
        }

        public void updateMktDepth(int tickerId, int position, int operation, int side, double price, int size)
        {
            throw new NotImplementedException();
        }

        public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, int size, bool isSmartDepth)
        {
            throw new NotImplementedException();
        }

        public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange)
        {
            throw new NotImplementedException();
        }

        public void position(string account, Contract contract, double pos, double avgCost)
        {
            throw new NotImplementedException();
        }

        public void positionEnd()
        {
            throw new NotImplementedException();
        }

        public void realtimeBar(int reqId, long date, double open, double high, double low, double close, long volume, double WAP, int count)
        {
            throw new NotImplementedException();
        }

        public void scannerParameters(string xml)
        {
            throw new NotImplementedException();
        }

        public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr)
        {
            throw new NotImplementedException();
        }

        public void scannerDataEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void receiveFA(int faDataType, string faXmlData)
        {
            throw new NotImplementedException();
        }

        public void verifyMessageAPI(string apiData)
        {
            throw new NotImplementedException();
        }

        public void verifyCompleted(bool isSuccessful, string errorText)
        {
            throw new NotImplementedException();
        }

        public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge)
        {
            throw new NotImplementedException();
        }

        public void verifyAndAuthCompleted(bool isSuccessful, string errorText)
        {
            throw new NotImplementedException();
        }

        public void displayGroupList(int reqId, string groups)
        {
            throw new NotImplementedException();
        }

        public void displayGroupUpdated(int reqId, string contractInfo)
        {
            throw new NotImplementedException();
        }

        public void positionMulti(int requestId, string account, string modelCode, Contract contract, double pos, double avgCost)
        {
            throw new NotImplementedException();
        }

        public void positionMultiEnd(int requestId)
        {
            throw new NotImplementedException();
        }

        public void accountUpdateMulti(int requestId, string account, string modelCode, string key, string value, string currency)
        {
            throw new NotImplementedException();
        }

        public void accountUpdateMultiEnd(int requestId)
        {
            throw new NotImplementedException();
        }

        public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes)
        {
            throw new NotImplementedException();
        }

        public void securityDefinitionOptionParameterEnd(int reqId)
        {
            throw new NotImplementedException();
        }

        public void softDollarTiers(int reqId, SoftDollarTier[] tiers)
        {
            throw new NotImplementedException();
        }

        public void familyCodes(FamilyCode[] familyCodes)
        {
            throw new NotImplementedException();
        }

        public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions)
        {
            throw new NotImplementedException();
        }

        public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData)
        {
            throw new NotImplementedException();
        }

        public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap)
        {
            throw new NotImplementedException();
        }

        public void newsProviders(NewsProvider[] newsProviders)
        {
            throw new NotImplementedException();
        }

        public void newsArticle(int requestId, int articleType, string articleText)
        {
            throw new NotImplementedException();
        }

        public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline)
        {
            throw new NotImplementedException();
        }

        public void historicalNewsEnd(int requestId, bool hasMore)
        {
            throw new NotImplementedException();
        }

        public void headTimestamp(int reqId, string headTimestamp)
        {
            throw new NotImplementedException();
        }

        public void histogramData(int reqId, HistogramEntry[] data)
        {
            throw new NotImplementedException();
        }

        public void rerouteMktDataReq(int reqId, int conId, string exchange)
        {
            throw new NotImplementedException();
        }

        public void rerouteMktDepthReq(int reqId, int conId, string exchange)
        {
            throw new NotImplementedException();
        }

        public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements)
        {
            throw new NotImplementedException();
        }

        public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL)
        {
            throw new NotImplementedException();
        }

        public void pnlSingle(int reqId, int pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value)
        {
            throw new NotImplementedException();
        }

        public void historicalTicks(int reqId, HistoricalTick[] ticks, bool done)
        {
            throw new NotImplementedException();
        }

        public void historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done)
        {
            throw new NotImplementedException();
        }

        public void historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done)
        {
            throw new NotImplementedException();
        }

        public void tickByTickAllLast(int reqId, int tickType, long time, double price, int size, TickAttribLast tickAttriblast, string exchange, string specialConditions)
        {
            throw new NotImplementedException();
        }

        public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, int bidSize, int askSize, TickAttribBidAsk tickAttribBidAsk)
        {
            throw new NotImplementedException();
        }

        public void tickByTickMidPoint(int reqId, long time, double midPoint)
        {
            throw new NotImplementedException();
        }

        public void orderBound(long orderId, int apiClientId, int apiOrderId)
        {
            throw new NotImplementedException();
        }

        public void completedOrder(Contract contract, Order order, OrderState orderState)
        {
            throw new NotImplementedException();
        }

        public void completedOrdersEnd()
        {
            throw new NotImplementedException();
        }
    }
}
