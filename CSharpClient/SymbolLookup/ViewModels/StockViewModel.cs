using SymbolLookup.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SymbolLookup.ViewModels
{
    public class StockViewModel
    {
        private List<StockModel> _stocks;

        public List<StockModel> Stocks 
        { 
            get => _stocks;
            set { _stocks = value; } 
        }
    }

    //private class Updater : ICommand
}
