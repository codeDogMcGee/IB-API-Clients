﻿using IBApi;

namespace IbApiLibrary.Models
{
    public class ExecutionsModel
    {
        public Execution Execution { get; set; }
        public CommissionReport CommissionReport { get; set; }
        public Contract Contract { get; set; }
    }
}
