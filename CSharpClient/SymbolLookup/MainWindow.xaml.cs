//using CSharpClient.IbApiLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SymbolLookup.Models;
using System.ComponentModel;
using CsharpClient.IbApiLibrary;

namespace SymbolLookup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IbClient _ibClient;
        private bool _isConnected = false;

        private BindingList<StockContractModel> _stocks = new BindingList<StockContractModel>();

        private static readonly DependencyProperty _stocksListProperty = DependencyProperty.Register("Stocks", typeof(List<string>), typeof(Window));

        private List<string> Stocks
        {
            get { return (List<string>)GetValue(_stocksListProperty); }
            set { SetValue(_stocksListProperty, value); }
        }


        public MainWindow()
        {
            InitializeComponent();

            ContractListBox.ItemsSource = _stocks;
        }

        private void ConnectIbButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected)
            {
                _ibClient.DisconnectIbSocket();

                _ibClient = null;

                _isConnected = false;

                IbConnectionLabel.Text = "Disconnected";
                IbConnectionLabel.Foreground = new SolidColorBrush(Colors.Red);

                SubmitSearchButton.IsEnabled = false;
            }
            else
            {
                _ibClient = new IbClient();

                _ibClient.ConnectToIb();

                _isConnected = true;

                IbConnectionLabel.Text = "Connected";
                IbConnectionLabel.Foreground = new SolidColorBrush(Colors.Green);

                SubmitSearchButton.IsEnabled = true;
            }

        }

        private void SubmitSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_stocks.Count > 0)
            {
                _stocks = new BindingList<StockContractModel>();
            }

            string searchText = SearchText.Text;

            string stocksJson = _ibClient.GetMatchingStockSymbolsFromIB(searchText);
            List < StockContractModel > stocks = JsonConvert.DeserializeObject<List<StockContractModel>>(stocksJson);

            foreach (StockContractModel stock in stocks)
            {
                _stocks.Add(stock);
            }
        }

        private void ContractListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectionIndex = ContractListBox.SelectedIndex;
            StockContractModel userSelection = _stocks[selectionIndex];

            bool isSmartRouting = (bool)SmartRoutingRadioButton.IsChecked;

            if (isSmartRouting)
            {
                userSelection.Exchange = "SMART";
            }
            else
            {
                userSelection.Exchange = userSelection.PrimaryExchange;
            }

            var output = new 
            { 
                userSelection.Symbol,
                userSelection.Currency,
                userSelection.Exchange,
                SecType = userSelection.SecurityType,
                PrimaryExch = userSelection.PrimaryExchange
            };

            MessageBox.Show($"You picked {userSelection.Symbol} {userSelection.PrimaryExchange} {userSelection.Currency}");

        }
    }
}
