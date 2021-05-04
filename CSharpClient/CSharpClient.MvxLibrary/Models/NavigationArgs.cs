using CsharpClient.IbApiLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CSharpClient.MvxLibrary.Models
{
    public class NavigationArgs
    {
        public StockContractModel LastUserSelectedContract { get; set; }
        public bool IbClientIsConnected { get; set; }
        public IbClient IbClient { get; set; }
        public ObservableCollection<StockLineItemModel> StockRowList { get; set; }
    }
}
