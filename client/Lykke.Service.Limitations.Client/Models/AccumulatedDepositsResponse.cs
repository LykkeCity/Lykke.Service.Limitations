using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Limitations.Client.Models
{
    public class AccumulatedDepositsResponse
    {
        public double AmountTotal { get; set; }

        public double Amount30Days { get; set; }

        public double Amount1Day { get; set; }
    }
}
