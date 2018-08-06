// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Service.Limitations.Client.AutorestClient.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class AccumulatedAmountsModel
    {
        /// <summary>
        /// Initializes a new instance of the AccumulatedAmountsModel class.
        /// </summary>
        public AccumulatedAmountsModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the AccumulatedAmountsModel class.
        /// </summary>
        public AccumulatedAmountsModel(double depositTotalSwift, double deposit30DaysSwift, double deposit1DaySwift, double depositTotalCards, double deposit30DaysCards, double deposit1DayCards, double depositTotalFiat, double deposit30DaysFiat, double deposit1DayFiat, double depositTotalNonFiat, double deposit30DaysNonFiat, double deposit1DayNonFiat, double withdrawalTotalSwift, double withdrawal30DaysSwift, double withdrawal1DaySwift, double withdrawalTotalCards, double withdrawal30DaysCards, double withdrawal1DayCards, double withdrawalTotalFiat, double withdrawal30DaysFiat, double withdrawal1DayFiat, double withdrawalTotalNonFiat, double withdrawal30DaysNonFiat, double withdrawal1DayNonFiat)
        {
            DepositTotalSwift = depositTotalSwift;
            Deposit30DaysSwift = deposit30DaysSwift;
            Deposit1DaySwift = deposit1DaySwift;
            DepositTotalCards = depositTotalCards;
            Deposit30DaysCards = deposit30DaysCards;
            Deposit1DayCards = deposit1DayCards;
            DepositTotalFiat = depositTotalFiat;
            Deposit30DaysFiat = deposit30DaysFiat;
            Deposit1DayFiat = deposit1DayFiat;
            DepositTotalNonFiat = depositTotalNonFiat;
            Deposit30DaysNonFiat = deposit30DaysNonFiat;
            Deposit1DayNonFiat = deposit1DayNonFiat;
            WithdrawalTotalSwift = withdrawalTotalSwift;
            Withdrawal30DaysSwift = withdrawal30DaysSwift;
            Withdrawal1DaySwift = withdrawal1DaySwift;
            WithdrawalTotalCards = withdrawalTotalCards;
            Withdrawal30DaysCards = withdrawal30DaysCards;
            Withdrawal1DayCards = withdrawal1DayCards;
            WithdrawalTotalFiat = withdrawalTotalFiat;
            Withdrawal30DaysFiat = withdrawal30DaysFiat;
            Withdrawal1DayFiat = withdrawal1DayFiat;
            WithdrawalTotalNonFiat = withdrawalTotalNonFiat;
            Withdrawal30DaysNonFiat = withdrawal30DaysNonFiat;
            Withdrawal1DayNonFiat = withdrawal1DayNonFiat;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "DepositTotalSwift")]
        public double DepositTotalSwift { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Deposit30DaysSwift")]
        public double Deposit30DaysSwift { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Deposit1DaySwift")]
        public double Deposit1DaySwift { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "DepositTotalCards")]
        public double DepositTotalCards { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Deposit30DaysCards")]
        public double Deposit30DaysCards { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Deposit1DayCards")]
        public double Deposit1DayCards { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "DepositTotalFiat")]
        public double DepositTotalFiat { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Deposit30DaysFiat")]
        public double Deposit30DaysFiat { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Deposit1DayFiat")]
        public double Deposit1DayFiat { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "DepositTotalNonFiat")]
        public double DepositTotalNonFiat { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Deposit30DaysNonFiat")]
        public double Deposit30DaysNonFiat { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Deposit1DayNonFiat")]
        public double Deposit1DayNonFiat { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "WithdrawalTotalSwift")]
        public double WithdrawalTotalSwift { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Withdrawal30DaysSwift")]
        public double Withdrawal30DaysSwift { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Withdrawal1DaySwift")]
        public double Withdrawal1DaySwift { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "WithdrawalTotalCards")]
        public double WithdrawalTotalCards { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Withdrawal30DaysCards")]
        public double Withdrawal30DaysCards { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Withdrawal1DayCards")]
        public double Withdrawal1DayCards { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "WithdrawalTotalFiat")]
        public double WithdrawalTotalFiat { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Withdrawal30DaysFiat")]
        public double Withdrawal30DaysFiat { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Withdrawal1DayFiat")]
        public double Withdrawal1DayFiat { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "WithdrawalTotalNonFiat")]
        public double WithdrawalTotalNonFiat { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Withdrawal30DaysNonFiat")]
        public double Withdrawal30DaysNonFiat { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Withdrawal1DayNonFiat")]
        public double Withdrawal1DayNonFiat { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="Microsoft.Rest.ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            //Nothing to validate
        }
    }
}
