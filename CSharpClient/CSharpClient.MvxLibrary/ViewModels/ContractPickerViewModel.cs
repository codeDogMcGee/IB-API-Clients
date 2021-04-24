using CsharpClient.IbApiLibrary;
using CSharpClient.MvxLibrary.Models;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CSharpClient.MvxLibrary.ViewModels
{
    public class ContractPickerViewModel : MvxViewModel
    {
        private IbClient _ibClient;
        public ContractPickerViewModel()
        {
            ConnectIbCommand = new MvxCommand(ConnectToIb);
            SearchForSymbolCommand = new MvxCommand(SearchForSymbol);
            //ChooseContractCommand = new MvxCommand(ChooseContract);
        }

        public IMvxCommand ConnectIbCommand { get; set; }
        public IMvxCommand SearchForSymbolCommand { get; set; }
        //public IMvxCommand ChooseContractCommand { get; set; }

        private void ConnectToIb()
        {
            if (IsConnectedToIB)
            {
                _ibClient.DisconnectIbSocket();
                _ibClient = null;

                IsConnectedToIB = false;
            }
            else
            {
                _ibClient = new IbClient();
                _ibClient.ConnectToIb();
                
                IsConnectedToIB = true;
            }
        }

        private void SearchForSymbol()
        {
            Stocks = new ObservableCollection<StockContractModel>();

            string stocksJson = _ibClient.GetMatchingStockSymbolsFromIB(SearchText);
            List<StockContractModel> stocks = JsonConvert.DeserializeObject<List<StockContractModel>>(stocksJson);

            foreach (StockContractModel stock in stocks)
            {
                Stocks.Add(stock);
            }
        }

        private StockContractModel _selectedStock;

        public StockContractModel SelectedStock
        {
            get => _selectedStock;
            set 
            { 
                _selectedStock = value;
                if (value != null)
                {
                    DoSomethingWhenStockSelected();
                }
                
            }
        }

        private void DoSomethingWhenStockSelected()
        {
            throw new NotImplementedException("DoSomethingWhenStockSelected() not yet implemented.");
        }

        private bool _isConnectedToIB = false;

        public bool IsConnectedToIB
        {
            get => _isConnectedToIB;
            set
            {
                SetProperty(ref _isConnectedToIB, value);
            }
        }

        private bool _contractRowIsSelected;

        public bool ContractRowIsSelected
        {
            get => _contractRowIsSelected;
            set
            {
                SetProperty(ref _contractRowIsSelected, value);
            }
        }


        public string IsConnectedLabel { get => _isConnectedToIB ? "Connected" : "Disconnected"; }
        public string ConnectionButtonText { get => _isConnectedToIB ? "Disconnect IB" : "Connect IB"; }

        private string _searchText = "";

        public string SearchText
        {
            get => _searchText;
            set
            {
              SetProperty(ref _searchText, value);
                RaisePropertyChanged(() => CanSearchForSymbol);
            }
        }

        public bool CanSearchForSymbol => IsConnectedToIB is true && string.IsNullOrWhiteSpace(SearchText) is false;  // add no blank values passed

        private ObservableCollection<StockContractModel> _stocks = new ObservableCollection<StockContractModel>();

        public ObservableCollection<StockContractModel> Stocks
        {
            get => _stocks;
            set { SetProperty(ref _stocks, value); }
        }

    }
}
