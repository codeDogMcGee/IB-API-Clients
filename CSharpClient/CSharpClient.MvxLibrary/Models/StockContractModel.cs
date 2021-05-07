using CSharpClient.IbApiLibrary.Interfaces;
using System.ComponentModel;

namespace CSharpClient.MvxLibrary.Models
{
    public class StockContractModel : IStockContractModel, INotifyPropertyChanged
    {
        /// <summary>
        /// INotifyPropertyChanged is used here because this class is used in an 
        /// ObservableCollection that is bound to the UI, so each time one of 
        /// these properties changes the MV, and therefore the UI, needs to be notified.
        /// </summary>

        public long Id { get; set; }
        public int ContractId { get; set; }
        public string Symbol { get; set; }
        public string SecurityType { get; set; }
        public string Exchange { get; set; }
        public string Currency { get; set; }
        public string PrimaryExchange { get; set; }
        public bool IsStreamingData { get; set; }

        // full properties
        // these properties have dynamic values that need to be presented to the views

        private double _position;
        public double Position
        {
            get { return _position; }
            set
            {
                if (value == _position) return;
                _position = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Position"));
            }
        }

        public double TotalPnL { get { return UnrealizedPnL + RealizedPnL; } }

        private double _unrealizedPnL;
        public double UnrealizedPnL
        {
            get { return _unrealizedPnL; }
            set
            {
                if (value == _unrealizedPnL) return;
                _unrealizedPnL = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UnrealizedPnL"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TotalPnL"));
            }
        }

        private double _realizedPnL;
        public double RealizedPnL
        {
            get { return _realizedPnL; }
            set
            {
                if (value == _realizedPnL) return;
                _realizedPnL = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RealizedPnL"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TotalPnL"));
            }
        }

        private double _lastPrice;
        public double LastPrice
        {
            get { return _lastPrice; }
            set
            {
                if (value == _lastPrice) return;
                _lastPrice = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LastPrice"));
            }
        }
        
        //private double _accountValueMarkPrice;
        //public double AccountValueMarkPrice
        //{
        //    get { return _accountValueMarkPrice; }
        //    set
        //    {
        //        if (value == _accountValueMarkPrice) return;
        //        _accountValueMarkPrice = value;
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AccountValueMarkPrice"));
        //    }
        //}

        private double _markPrice;
        public double MarkPrice
        {
            get { return _markPrice; }
            set
            {
                if (value == _markPrice) return;
                _markPrice = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MarkPrice"));
            }
        }

        private double _todaysLowPrice;
        public double TodaysLowPrice
        {
            get { return _todaysLowPrice; }
            set
            {
                if (value == _todaysLowPrice) return;
                _todaysLowPrice = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TodaysLowPrice"));
            }
        }

        private double _todaysHighPrice;
        public double TodaysHighPrice
        {
            get { return _todaysHighPrice; }
            set
            {
                if (value == _todaysHighPrice) return;
                _todaysHighPrice = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TodaysHighPrice"));
            }
        }

        private double _todaysVolume;
        public double TodaysVolume
        {
            get { return _todaysVolume; }
            set
            {
                if (value == _todaysVolume) return;
                _todaysVolume = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TodaysVolume"));
            }
        }


        private double _bidPrice;
        public double BidPrice
        {
            get { return _bidPrice; }
            set
            {
                if (value == _bidPrice) return;
                _bidPrice = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BidPrice"));
            }
        }

        private double _askPrice;
        public double AskPrice
        {
            get { return _askPrice; }
            set
            {
                if (value == _askPrice) return;
                _askPrice = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AskPrice"));
            }
        }


        private int _bidSize;
        public int BidSize
        {
            get { return _bidSize; }
            set
            {
                if (value == _bidSize) return;
                _bidSize = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BidSize"));
            }
        }

        private int _askSize;
        public int AskSize
        {
            get { return _askSize; }
            set
            {
                if (value == _askSize) return;
                _askSize = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AskSize"));
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
    }
}
