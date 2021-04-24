using CsharpClient.IbApiLibrary.Models;
using MvvmCross.Platforms.Wpf.Views;
using System.Collections.ObjectModel;

namespace CSharpClient.StockContractPicker.Views
{
    /// <summary>
    /// Interaction logic for ContractPickerView.xaml
    /// </summary>
    public partial class ContractPickerView : MvxWpfView
    {
        public ContractPickerView()
        {
            InitializeComponent();

            ObservableCollection<StockContractModel> Stocks = new ObservableCollection<StockContractModel>
            {
                new StockContractModel{ContractId = 101, Symbol = "AAPL", SecurityType="STK", Currency="USD", PrimaryExchange="Exchange"}
            };

        }
    }
}
