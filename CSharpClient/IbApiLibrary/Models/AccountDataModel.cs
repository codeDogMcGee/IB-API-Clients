namespace IbApiLibrary.Models
{
    public class AccountDataModel
    {
        public string AccountId { get; set; }
        public string AvailableFunds { get; set; }
        public string Currency { get; set; }
        public string InitialMarginReq { get; set; }
        public string GrossPositionsValue { get; set; }
        public string NetLiquidationValue { get; set; }
        public string RealizedPnL { get; set; }
        public string UnrealizedPnL { get; set; }
    }
}
