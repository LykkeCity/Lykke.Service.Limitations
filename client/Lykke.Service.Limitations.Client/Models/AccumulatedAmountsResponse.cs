using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Limitations.Client.Models
{
    public class AccumulatedAmountsResponse
    {
        public double DepositTotalFiat { get; set; }

        public double Deposit30DaysFiat { get; set; }

        public double Deposit1DayFiat { get; set; }

        public double DepositTotalNonFiat { get; set; }

        public double Deposit30DaysNonFiat { get; set; }

        public double Deposit1DayNonFiat { get; set; }

        public double WithdrawalTotalFiat { get; set; }

        public double Withdrawal30DaysFiat { get; set; }

        public double Withdrawal1DayFiat { get; set; }

        public double WithdrawalTotalNonFiat { get; set; }

        public double Withdrawal30DaysNonFiat { get; set; }

        public double Withdrawal1DayNonFiat { get; set; }
    }
}
