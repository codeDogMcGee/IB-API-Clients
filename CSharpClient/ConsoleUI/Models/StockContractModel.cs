using System;

namespace CSharpClient.ConsoleUI.Models
{
    public class StockContractModel
    {
        public int ContractId { get; set; }
        public string Symbol { get; set; }
        public string SecurityType { get; set; }
        public string Exchange { get; set; } = "SMART";
        public string Currency { get; set; }
        public string PrimaryExchange { get; set; }

        public override string ToString()
        {
            // in String.Format brackets the first number is the index and the second is the position, negative means left align
            return String.Format("{0,-15}{1,-15}{2,-7}{3,-15}{4}", Symbol, PrimaryExchange, Currency, ContractId, SecurityType);
        }
    }
}
