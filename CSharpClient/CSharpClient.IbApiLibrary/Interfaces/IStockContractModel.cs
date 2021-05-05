namespace CSharpClient.IbApiLibrary.Interfaces
{
    public interface IStockContractModel
    {
        int ContractId { get; set; }
        string Currency { get; set; }
        string Exchange { get; set; }
        string PrimaryExchange { get; set; }
        string SecurityType { get; set; }
        string Symbol { get; set; }
    }
}