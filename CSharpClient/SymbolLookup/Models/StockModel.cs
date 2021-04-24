using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SymbolLookup.Models
{
    public class StockModel : INotifyPropertyChanged
    {
        private int _contractId;
        private string _symbol;
        private string _securityType;
        private string _exchange;
        private string _currency;
        private string _primaryExchange;

        public int ContractId 
        { 
            get => _contractId;
            set
            {
                _contractId = value;
                OnPropertyChanged("ContractId");
            } 
        }

        public string Symbol 
        {
            get => _symbol;
            set
            {
                _symbol = value;
                OnPropertyChanged("Symbol");
            }
        }

        public string SecurityType 
        {
            get => _securityType;
            set
            {
                _securityType = value;
                OnPropertyChanged("SecurityType");
            }
        }

        public string Exchange 
        {
            get => _exchange;
            set
            {
                _exchange = value;
                OnPropertyChanged("Exchange");
            }
        }

        public string Currency 
        {
            get => _currency;
            set
            {
                _currency = value;
                OnPropertyChanged("Currency");
            }
        }

        public string PrimaryExchange 
        {
            get => _primaryExchange;
            set
            {
                _primaryExchange = value;
                OnPropertyChanged("PrimaryExchange");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
