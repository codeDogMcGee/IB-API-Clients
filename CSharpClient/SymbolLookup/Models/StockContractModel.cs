using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SymbolLookup.Models
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
            List<(string, int)> propsMaxLength = new List<(string, int)>
            {
                (Symbol, 10),
                (PrimaryExchange, 10),
                (Currency, 5),
                (SecurityType, 5),
                (ContractId.ToString(), 12)
            };
            
            string returnString = "";
            foreach (var prop in propsMaxLength)
            {
                string propString = prop.Item1;
                if (prop.Item1.Length > prop.Item2)
                {
                    propString = prop.Item1.Substring(0, prop.Item2);
                }
                else if (prop.Item1.Length < prop.Item2)
                {
                    propString += String.Concat(Enumerable.Repeat(" ", prop.Item2 - prop.Item1.Length));
                }

                if (returnString == "")
                {
                    returnString = propString;
                }
                else
                {
                    returnString += " " + propString;
                }
            }
            return returnString;
        }
    }
}
