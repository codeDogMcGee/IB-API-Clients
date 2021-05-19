using System;
using System.Collections.Generic;
using System.Text;

namespace IbApiLibrary.Models
{
    public class OrderStatusModel
    {
        public int OrderId { get; set; }
        public string Status { get; set; }
        public double Filled { get; set; }
        public double Remaining { get; set; }
        public double AvgFillPrice { get; set; }
        public int PermId { get; set; }
        public int ParentId { get; set; }
        public double LastFillPrice { get; set; }
        public int ClientId { get; set; }
        public string WhyHeld { get; set; }
        public double MktCapPrice { get; set; }
    }
}
