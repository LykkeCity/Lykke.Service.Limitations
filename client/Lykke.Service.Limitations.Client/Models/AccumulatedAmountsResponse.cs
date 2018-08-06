using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Limitations.Client.Models
{
    public class AccumulatedAmountsResponse
    {
        public double DepositTotalSwift { get; set; }

        public double Deposit30DaysSwift { get; set; }

        public double Deposit1DaySwift { get; set; }

        public double DepositTotalCards { get; set; }

        public double Deposit30DaysCards { get; set; }

        public double Deposit1DayCards { get; set; }

        public double DepositTotalFiat { get; set; }

        public double Deposit30DaysFiat { get; set; }

        public double Deposit1DayFiat { get; set; }

        public double DepositTotalNonFiat { get; set; }

        public double Deposit30DaysNonFiat { get; set; }

        public double Deposit1DayNonFiat { get; set; }

        public double WithdrawalTotalSwift { get; set; }

        public double Withdrawal30DaysSwift { get; set; }

        public double Withdrawal1DaySwift { get; set; }

        public double WithdrawalTotalCards { get; set; }

        public double Withdrawal30DaysCards { get; set; }

        public double Withdrawal1DayCards { get; set; }

        public double WithdrawalTotalFiat { get; set; }

        public double Withdrawal30DaysFiat { get; set; }

        public double Withdrawal1DayFiat { get; set; }

        public double WithdrawalTotalNonFiat { get; set; }

        public double Withdrawal30DaysNonFiat { get; set; }

        public double Withdrawal1DayNonFiat { get; set; }
    }
}
