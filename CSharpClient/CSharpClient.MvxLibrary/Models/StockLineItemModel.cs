using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpClient.MvxLibrary.Models
{
    public class StockLineItemModel
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string Exchange { get; set; }
        public double DailyLowPrice { get; set; }
        public double DailyHighPrice { get; set; }
        public double RAmount { get; set; }
    }
}
