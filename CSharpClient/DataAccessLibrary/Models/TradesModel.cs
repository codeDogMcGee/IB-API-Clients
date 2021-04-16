using IBApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLibrary.Models
{
    public class TradesModel
    {
        public Execution Execution { get; set; }
        public CommissionReport CommissionReport { get; set; }
        public Contract Contract { get; set; }
    }
}
