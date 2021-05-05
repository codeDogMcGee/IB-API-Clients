using CSharpClient.IbApiLibrary.Interfaces;

namespace CSharpClient.IbApiLibrary.Models
{
    public class StockContractModel : IStockContractModel
    {
        public int ContractId { get; set; }
        public string Symbol { get; set; }
        public string SecurityType { get; set; }
        public string Exchange { get; set; }
        public string Currency { get; set; }
        public string PrimaryExchange { get; set; }
    }
}
