using CsharpClient.IbApiLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;

namespace CSharpClient.MvxLibrary.Models
{
    public class NavigationArgs
    {
        public IbClient IbClient { get; set; }
        public ObservableCollection<StockContractModel> StockList { get; set; }
        public Thread DataUpdataThread { get; set; }
    }
}
