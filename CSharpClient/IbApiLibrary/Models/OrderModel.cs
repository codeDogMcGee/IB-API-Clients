using IBApi;

namespace IbApiLibrary.Models
{
    public class OrderModel
    {
        public Contract Contract { get; set; }
        public Order Order { get; set; }
        public OrderState OrderState { get; set; }
    }
}
