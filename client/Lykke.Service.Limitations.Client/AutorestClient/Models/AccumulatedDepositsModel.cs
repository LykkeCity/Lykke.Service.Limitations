// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Service.Limitations.Client.AutorestClient.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    public partial class AccumulatedDepositsModel
    {
        /// <summary>
        /// Initializes a new instance of the AccumulatedDepositsModel class.
        /// </summary>
        public AccumulatedDepositsModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the AccumulatedDepositsModel class.
        /// </summary>
        public AccumulatedDepositsModel(double amountTotal, double amount30Days, double amount1Day)
        {
            AmountTotal = amountTotal;
            Amount30Days = amount30Days;
            Amount1Day = amount1Day;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "AmountTotal")]
        public double AmountTotal { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Amount30Days")]
        public double Amount30Days { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Amount1Day")]
        public double Amount1Day { get; set; }

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
