using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpClient.IbApiLibrary.Models
{
    public class DataModel
    {
        public double BidPrice { get; set; }
        public double AskPrice { get; set; }
        public double LastPrice { get; set; }
        public double DailyHighPrice { get; set; }
        public double DailyLowPrice { get; set; }
        public double PreviousClosePrice { get; set; }
        public double OpenPrice { get; set; }
        public double MarkPrice { get; set; }
        public int BidSize { get; set; }
        public int AskSize { get; set; }
        public int LastSize { get; set; }
        public int DailyVolume { get; set; }
    }
}
