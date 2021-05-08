using IbApiLibrary;
using MvxLibrary.Models;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Threading;

namespace MvxLibrary.ViewModels
{
    public class ContractPickerViewModel : MvxViewModel<NavigationArgs>
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetLogger("MainLog");

        private IbClient _ibClient;
        private readonly IMvxNavigationService _navigationService;
        private ObservableCollection<StockContractModel> _mainStockList;
        private Thread _dataUpdateThread;

        public ContractPickerViewModel(IMvxNavigationService navigationService)
        {
            _logger.Debug("Starting ContractPickerViewModel");



            SearchForSymbolCommand = new MvxCommand(SearchForSymbol);
            ContractExchangeCommand = new MvxCommand(ToggleUserExchangeChoice);

            _navigationService = navigationService;
            NavigateHomeCommand = new MvxCommand(() => _navigationService.Navigate<PositionViewerViewModel, NavigationArgs>(
                new NavigationArgs
                {
                    IbClient = _ibClient,
                    StockList = _mainStockList,
                    DataUpdataThread = _dataUpdateThread
                })
            ); ;
        }

        public IMvxCommand SearchForSymbolCommand { get; private set; }
        public IMvxCommand ContractExchangeCommand { get; private set; }
        public IMvxCommand NavigateHomeCommand { get; private set; }

        private bool _userExchangeIsSmart = true;
        private bool _contractRowIsSelected;
        private StockContractModel _selectedStock;


        private void SearchForSymbol()
        {
            SearchResultsStocks = new ObservableCollection<StockContractModel>();

            string stocksJson = _ibClient.GetMatchingStockSymbolsFromIB(SearchText);
            ObservableCollection<StockContractModel> stocks = JsonConvert.DeserializeObject<ObservableCollection<StockContractModel>>(stocksJson);

            foreach (StockContractModel stock in stocks)
            {
                SearchResultsStocks.Add(stock);
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
                    AddSelectedStockAndSendHome(value);
                }
            }
        }

        private void AddSelectedStockAndSendHome(StockContractModel userSelectedContract)
        {
            // Initialize the contract before sending back to home page
            userSelectedContract.Exchange = _userExchangeIsSmart ? "SMART" : userSelectedContract.PrimaryExchange;
            userSelectedContract.Id = DateTimeOffset.Now.ToUnixTimeSeconds();
            userSelectedContract.IsStreamingData = false;
    
            _mainStockList.Add(userSelectedContract);

            NavigateHomeCommand.Execute();
        }

        public override void Prepare(NavigationArgs parameter)
        {
            _ibClient = parameter.IbClient;
            _mainStockList = parameter.StockList;
            _dataUpdateThread = parameter.DataUpdataThread;

        }

        public bool CanSearchForSymbol => string.IsNullOrWhiteSpace(SearchText) is false;

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

        private string _symbolTableVisibility = "Hidden";
        public string SymbolTableVisibility
        {
            get => _symbolTableVisibility;
            set { SetProperty(ref _symbolTableVisibility, value); }
        }

        private ObservableCollection<StockContractModel> _searchResultsStocks = new ObservableCollection<StockContractModel>();
        public ObservableCollection<StockContractModel> SearchResultsStocks
        {
            get => _searchResultsStocks;
            set { SetProperty(ref _searchResultsStocks, value); }
        }

    }
}
