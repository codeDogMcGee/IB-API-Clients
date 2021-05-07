using MvvmCross.Commands;
using MvvmCross.ViewModels;
using IbApiLibrary;
using System.Collections.ObjectModel;
using MvvmCross.Navigation;
using MvxLibrary.Models;
using System.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MvxLibrary.ViewModels
{ 

    public class StockTraderViewModel : MvxViewModel<NavigationArgs>
    {
        // Class variables
        private static readonly NLog.Logger _logger = NLog.LogManager.GetLogger("MainLog");

        private IbClient _ibClient;
        private readonly IMvxNavigationService _navigationService;
        private Thread _dataUpdateThread;
        private bool _stopDataUpdateThread = false;

        // Contstructor
        public StockTraderViewModel(IMvxNavigationService navigationService)
        {
            _logger.Debug("Starting StockTraderViewModel");

            _navigationService = navigationService;
            AddNewStockCommand = new MvxCommand(NavigateToStockPickerPage);
            ConnectIbCommand = new MvxCommand(ConnectToIb);
        }

        // Properties
        public IMvxCommand ConnectIbCommand { get; private set; }
        public IMvxCommand AddNewStockCommand { get; private set; }

        public string IsConnectedLabel { get => IbIsConnected ? "Connected" : "Disconnected"; }
        public string ConnectionButtonText { get => IbIsConnected ? "Disconnect IB" : "Connect IB"; }
        public bool IbIsConnected { get => _ibClient != null && _ibClient.IsConnected; }
        
        private ObservableCollection<StockContractModel> _stocks = new ObservableCollection<StockContractModel>();
        public ObservableCollection<StockContractModel> Stocks
        {
            get { return _stocks; }
            private set
            {
                SetProperty(ref _stocks, value);
                RaisePropertyChanged(() => Stocks);
            }
        }

        public void ShutdownHandler()
        {
            if (_ibClient != null)
            {
                ConnectToIb();
            }
        }

        // Methods
        private void ConnectToIb()
        {
            if (IbIsConnected)
            {
                StopUpdatePricesThread();

                _ibClient.DisconnectIbSocket();
                _ibClient = null;

                foreach (var stock in Stocks)
                {
                    stock.IsStreamingData = false;
                }
            }
            else
            {
                _ibClient = new IbClient();
                _ibClient.ConnectToIb();

                _ibClient.RequestAccountUpdates();


                InitializeStocksList();
                // If there are existing stocks then repopulate
                // the instance data and start the price update thread
                if (Stocks.Count > 0)
                {
                    StreamDataFromStocksList();
                    StartUpdatePricesThread();
                }
            }

            RaisePropertyChanged(() => IbIsConnected);
            RaisePropertyChanged(() => IsConnectedLabel);
            RaisePropertyChanged(() => ConnectionButtonText);
        }

        private void InitializeStocksList()
        {
            List<int> exitingIds = new List<int>();
            foreach (var stock in Stocks)
            {
                exitingIds.Add(stock.ContractId);
            }
            

            if (_ibClient.StockData.Count > 0)
            {
                foreach (var stock in _ibClient.StockData.Values)
                {
                    if (exitingIds.Contains(stock.StockContract.ContractId) is false)
                    {
                        StockContractModel newStock = new StockContractModel
                        {
                            Id = DateTimeOffset.Now.ToUnixTimeSeconds(),
                            ContractId = stock.StockContract.ContractId,
                            Symbol = stock.StockContract.Symbol,
                            SecurityType = stock.StockContract.SecurityType,
                            Exchange = stock.StockContract.Exchange,
                            PrimaryExchange = stock.StockContract.PrimaryExchange,
                            IsStreamingData = false,
                            Position = stock.Data.Position,
                            UnrealizedPnL = 0,
                            RealizedPnL = stock.Data.RealizedPnL,
                            MarkPrice = stock.Data.MarkPrice
                        };

                        Stocks.Add(newStock);
                    }
                }
            }
        }

        private void StreamDataFromStocksList()
        {
            foreach (var stock in Stocks)
            {
                if (!stock.IsStreamingData)
                {
                    _ibClient.RequestStreamingData(stock);

                    stock.IsStreamingData = true;
                }
            }
        }

        private void NavigateToStockPickerPage()
        {
            // stop the thread before navigating over so that when the Stocks
            // list gets updated the thread won't exception, then restart the
            // thread once we navigate back to this VM
            StopUpdatePricesThread();

            _navigationService.Navigate<ContractPickerViewModel, NavigationArgs>(
                new NavigationArgs
                {
                    IbClient = _ibClient,
                    StockList = Stocks,
                    DataUpdataThread = _dataUpdateThread
                });
        }

        private void StopUpdatePricesThread()
        {
            _stopDataUpdateThread = true;
            while (_dataUpdateThread != null && _dataUpdateThread.IsAlive) {}
            _logger.Debug("UpdatePricesThread stopped.");
        }

        private void StartUpdatePricesThread()
        {
            _stopDataUpdateThread = false;
            _dataUpdateThread = new Thread(new ThreadStart(UpdatePricesThread))
            {
                IsBackground = true
            };
            _dataUpdateThread.Start();
            _logger.Debug("UpdatePricesThread started.");
        }

        public override void Prepare(NavigationArgs parameter)
        {
            _ibClient = parameter.IbClient;
            _dataUpdateThread = parameter.DataUpdataThread;
            Stocks = parameter.StockList;

            //// If this doesn't get reset the pnl will be messed up
            //// when navigating back
            //foreach (var stock in Stocks)
            //{
            //    stock.UnrealizedPnL = 0;
            //}

            StreamDataFromStocksList();
            StartUpdatePricesThread();
        }


        //private List<>

        private void UpdatePricesThread()
        {
            while (_stopDataUpdateThread is false)
            {
                if (_ibClient != null)
                {
                    // TODO: see if I can use events to update just the parts of the list that have updated
                    //       rather than continually looping through all of the values and updating them.
                    foreach (var stock in Stocks)
                    {
                        if (_ibClient.StockData.ContainsKey(stock.ContractId))
                        {
                            stock.BidPrice = _ibClient.StockData[stock.ContractId].Data.BidPrice;
                            stock.AskPrice = _ibClient.StockData[stock.ContractId].Data.AskPrice;

                            stock.BidSize = _ibClient.StockData[stock.ContractId].Data.BidSize;
                            stock.AskSize = _ibClient.StockData[stock.ContractId].Data.AskSize;

                            stock.MarkPrice = _ibClient.StockData[stock.ContractId].Data.MarkPrice;
                            
                            stock.TodaysLowPrice = _ibClient.StockData[stock.ContractId].Data.DailyLowPrice;
                            stock.TodaysHighPrice = _ibClient.StockData[stock.ContractId].Data.DailyHighPrice;

                            stock.Position = _ibClient.StockData[stock.ContractId].Data.Position;

                            
                            // IB doesn't update pnl in real time to the api so use the snapshot pricing and then adjust
                            //if (stock.UnrealizedPnL != 0)
                            //{
                            double pnlDiffSinceSnapshot = (stock.MarkPrice - _ibClient.StockData[stock.ContractId].Data.AccountValueMarkPrice) * stock.Position;
                            stock.UnrealizedPnL = _ibClient.StockData[stock.ContractId].Data.UnrealizedPnL + pnlDiffSinceSnapshot;
                            //}
                            //else
                            //{
                            //    stock.UnrealizedPnL = _ibClient.StockData[stock.ContractId].Data.UnrealizedPnL;
                            //}
                        }
                    }
                    Thread.Sleep(100);
                }
            }
        }
    }
}
