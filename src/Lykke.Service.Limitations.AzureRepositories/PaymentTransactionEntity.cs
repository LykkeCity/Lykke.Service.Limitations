using Common;
using Lykke.Service.Limitations.Core.Domain;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class PaymentTransactionEntity : TableEntity, IPaymentTransaction
    {
        public int Id { get; set; }
        public string TransactionId { get; set; }
        public string ClientId { get; set; }
        public DateTime Created { get; set; }
        public string Status { get; set; }
        public string PaymentSystem { get; set; }
        public string Info { get; set; }
        public double? Rate { get; set; }
        public string AggregatorTransactionId { get; set; }
        public double Amount { get; set; }
        public string AssetId { get; set; }
        public double? DepositedAmount { get; set; }
        public string DepositedAssetId { get; set; }

        string IPaymentTransaction.Id => TransactionId ?? Id.ToString();

        PaymentStatus IPaymentTransaction.Status => GetPaymentStatus();

        CashInPaymentSystem IPaymentTransaction.PaymentSystem => GetPaymentSystem();

        private PaymentStatus GetPaymentStatus()
        {
            return Status.ParseEnum(PaymentStatus.Created);
        }

        private CashInPaymentSystem GetPaymentSystem()
        {
            return PaymentSystem.ParseEnum(CashInPaymentSystem.Unknown);
        }
    }
}
