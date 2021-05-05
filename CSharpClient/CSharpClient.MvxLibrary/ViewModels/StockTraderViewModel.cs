using MvvmCross.Commands;
using MvvmCross.ViewModels;
using CsharpClient.IbApiLibrary;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using MvvmCross.Navigation;
using CSharpClient.MvxLibrary.Models;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Collections;

namespace CSharpClient.MvxLibrary.ViewModels
{

    public class StockTraderViewModel : MvxViewModel<NavigationArgs>
    {
        // Class variables
        private IbClient _ibClient;
        private readonly IMvxNavigationService _navigationService;
        private Thread _dataUpdateThread;
        private bool _stopDataUpdateThread = false;

        // Contstructor
        public StockTraderViewModel(IMvxNavigationService navigationService)
        {
            _navigationService = navigationService;
            //AddNewStockCommand = new MvxCommand(() => _navigationService.Navigate<ContractPickerViewModel, NavigationArgs>(
            //    new NavigationArgs
            //    {
            //        IbClient = _ibClient,
            //        StockList = Stocks,
            //        DataUpdataThread = _dataUpdateThread
            //    })
            //);
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

        // Methods
        private void ConnectToIb()
        {
            if (IbIsConnected)
            {
                StopUpdatePricesThread();

                _ibClient.DisconnectIbSocket();
                _ibClient = null;
                
            }
            else
            {
                _ibClient = new IbClient();
                _ibClient.ConnectToIb();

                // If there are existing stocks then start the price
                // thread once connected
                if (Stocks.Count > 0)
                {
                    StartUpdatePricesThread();
                }
            }

            RaisePropertyChanged(() => IbIsConnected);
            RaisePropertyChanged(() => IsConnectedLabel);
            RaisePropertyChanged(() => ConnectionButtonText);
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
            while (_dataUpdateThread != null && _dataUpdateThread.IsAlive)
            {
                _dataUpdateThread.Interrupt();
            }
        }

        private void StartUpdatePricesThread()
        {
            _stopDataUpdateThread = false;
            _dataUpdateThread = new Thread(new ThreadStart(UpdatePricesThread));
            _dataUpdateThread.Start();
        }

        public override void Prepare(NavigationArgs parameter)
        {
            _ibClient = parameter.IbClient;
            _dataUpdateThread = parameter.DataUpdataThread;
            Stocks = parameter.StockList;

            StreamDataFromStocksList();

            StartUpdatePricesThread();
        }

        private void UpdatePricesThread()
        {
            while (_stopDataUpdateThread is false)
            {
                // TODO: see if I can use events to update just the parts of the list that have updated
                //       rather than continually looping through all of the values and updating them.
                foreach (var stock in Stocks)
                {
                    if (_ibClient != null && _ibClient.StockData.ContainsKey(stock.ContractId))
                    {
                        stock.BidPrice = _ibClient.StockData[stock.ContractId].Data.BidPrice;
                        stock.AskPrice = _ibClient.StockData[stock.ContractId].Data.AskPrice;
                        stock.BidSize = _ibClient.StockData[stock.ContractId].Data.BidSize;
                        stock.AskSize = _ibClient.StockData[stock.ContractId].Data.AskSize;

                        stock.MarkPrice = _ibClient.StockData[stock.ContractId].Data.MarkPrice;
                        stock.TodaysLowPrice = _ibClient.StockData[stock.ContractId].Data.DailyLowPrice;
                        stock.TodaysHighPrice = _ibClient.StockData[stock.ContractId].Data.DailyHighPrice;
                    }
                }
            }
        }
    }
}
