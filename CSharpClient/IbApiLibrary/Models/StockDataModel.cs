using IbApiLibrary.Interfaces;

namespace IbApiLibrary.Models
{
    public class StockDataModel
    {
        public IStockContractModel StockContract { get; set; }
        public DataModel Data { get; set; }
    }
}
