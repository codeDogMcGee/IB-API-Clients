using MvvmCross.Commands;
using MvvmCross.ViewModels;
using CsharpClient.IbApiLibrary;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using MvvmCross.Navigation;
using CSharpClient.MvxLibrary.Models;

namespace CSharpClient.MvxLibrary.ViewModels
{
    public class StockTraderViewModel : MvxViewModel<NavigationArgs>
    {
        // Class variables
        private IbClient _ibClient;
        private readonly IMvxNavigationService _navigationService;
        private int _stockRowIndex = 0;

        // Contstructor
        public StockTraderViewModel(IMvxNavigationService navigationService)
        {
            _navigationService = navigationService;
            AddNewStockCommand = new MvxCommand(() => _navigationService.Navigate<ContractPickerViewModel, NavigationArgs>(
                new NavigationArgs
                {
                    IbClient = _ibClient,
                    IbClientIsConnected = IbIsConnected,
                    StockRowList = StockRows
                })
            );

            ConnectIbCommand = new MvxCommand(ConnectToIb);
        }

        // Properties
        public IMvxCommand ConnectIbCommand { get; private set; }
        public IMvxCommand AddNewStockCommand { get; private set; }

        public string IsConnectedLabel { get => IbIsConnected ? "Connected" : "Disconnected"; }
        public string ConnectionButtonText { get => IbIsConnected ? "Disconnect IB" : "Connect IB"; }


        // Full Properties used in bindings
        private bool _ibIsConnected;
        public bool IbIsConnected
        {
            get { return _ibIsConnected; }
            set {SetProperty(ref _ibIsConnected, value);}
        }

        private ObservableCollection<StockLineItemModel> _stocksRows = new ObservableCollection<StockLineItemModel>();
 
        public ObservableCollection<StockLineItemModel> StockRows
        {
            get { return _stocksRows; }
            set { SetProperty(ref _stocksRows, value); }
        }

        // methods
        private void ConnectToIb()
        {
            if (IbIsConnected)
            {
                _ibClient.DisconnectIbSocket();
                _ibClient = null;

                IbIsConnected = false;
            }
            else
            {
                _ibClient = new IbClient();
                _ibClient.ConnectToIb();

                IbIsConnected = true;

            }
            RaisePropertyChanged(() => IsConnectedLabel);
            RaisePropertyChanged(() => ConnectionButtonText);
        }

        private int StockRowIndex()
        {
            int returnValue = _stockRowIndex;  
            _stockRowIndex += 1;
            return returnValue;
        }

        public override void Prepare(NavigationArgs parameter)
        {
            _ibClient = parameter.IbClient;
            IbIsConnected = parameter.IbClientIsConnected;
            StockRows = parameter.StockRowList;

            StockLineItemModel stock = new StockLineItemModel
            {
                Id = StockRowIndex(),
                Symbol = parameter.LastUserSelectedContract.Symbol,
                Exchange = parameter.LastUserSelectedContract.Exchange,
                DailyLowPrice = 20.77, // these values are hard coded for testing
                DailyHighPrice = 30.99,
                RAmount = 0.00 
            };

            StockRows.Add(stock);
        }
    }
}
