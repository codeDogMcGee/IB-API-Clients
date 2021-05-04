using CsharpClient.IbApiLibrary;
using CSharpClient.MvxLibrary.Models;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CSharpClient.MvxLibrary.ViewModels
{
    public class ContractPickerViewModel : MvxViewModel<NavigationArgs>
    {
        private IbClient _ibClient;
        private readonly IMvxNavigationService _navigationService;
        private StockContractModel _selectedContract = new StockContractModel();
        private ObservableCollection<StockLineItemModel> _mainPageStocks;

        public ContractPickerViewModel(IMvxNavigationService navigationService)
        {
            SearchForSymbolCommand = new MvxCommand(SearchForSymbol);
            ContractExchangeCommand = new MvxCommand(ToggleUserExchangeChoice);

            _navigationService = navigationService;
            NavigateHomeCommand = new MvxCommand(() => _navigationService.Navigate<StockTraderViewModel, NavigationArgs>(
                new NavigationArgs
                {
                    LastUserSelectedContract = _selectedContract,
                    IbClientIsConnected = true,
                    IbClient = _ibClient,
                    StockRowList = _mainPageStocks
                })
            );
        }

        public IMvxCommand SearchForSymbolCommand { get; private set; }
        public IMvxCommand ContractExchangeCommand { get; private set; }
        public IMvxCommand NavigateHomeCommand { get; private set; }

        private bool _userExchangeIsSmart = true;
        private bool _contractRowIsSelected;
        private StockContractModel _selectedStock;


        private void SearchForSymbol()
        {
            Stocks = new ObservableCollection<StockContractModel>();

            string stocksJson = _ibClient.GetMatchingStockSymbolsFromIB(SearchText);
            List<StockContractModel> stocks = JsonConvert.DeserializeObject<List<StockContractModel>>(stocksJson);

            foreach (StockContractModel stock in stocks)
            {
                Stocks.Add(stock);
            }

            SymbolTableVisibility = "Visible";
        }

        private void ToggleUserExchangeChoice()
        {
            if (_userExchangeIsSmart)
            {
                _userExchangeIsSmart = false;
            }
            else
            {
                _userExchangeIsSmart = true;
            }
        }

        public StockContractModel SelectedStock
        {
            get => _selectedStock;
            set
            {
                _selectedStock = value;
                if (value != null)
                {
                    DoSomethingWhenStockSelected(value);
                }

            }
        }

        private void DoSomethingWhenStockSelected(StockContractModel userSelectedContract)
        {
            userSelectedContract.Exchange = _userExchangeIsSmart ? "SMART" : userSelectedContract.PrimaryExchange;
            _selectedContract = userSelectedContract;

            NavigateHomeCommand.Execute();
        }

        public override void Prepare(NavigationArgs parameter)
        {
            _ibClient = parameter.IbClient;
            _mainPageStocks = parameter.StockRowList;
        }

        public bool ContractRowIsSelected
        {
            get => _contractRowIsSelected;
            set { SetProperty(ref _contractRowIsSelected, value); }
        }

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

        public bool CanSearchForSymbol => string.IsNullOrWhiteSpace(SearchText) is false;

        private string _symbolTableVisibility = "Hidden";
        public string SymbolTableVisibility
        {
            get => _symbolTableVisibility;
            set { SetProperty(ref _symbolTableVisibility, value); }
        }

        private ObservableCollection<StockContractModel> _stocks = new ObservableCollection<StockContractModel>();

        public ObservableCollection<StockContractModel> Stocks
        {
            get => _stocks;
            set { SetProperty(ref _stocks, value); }
        }

    }
}
