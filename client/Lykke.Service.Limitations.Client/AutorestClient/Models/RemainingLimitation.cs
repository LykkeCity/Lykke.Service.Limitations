// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Lykke.Service.Limitations.Client.AutorestClient.Models
{
    using Microsoft.Rest;
    using Newtonsoft.Json;
    using System.Linq;

    public partial class RemainingLimitation
    {
        /// <summary>
        /// Initializes a new instance of the RemainingLimitation class.
        /// </summary>
        public RemainingLimitation()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the RemainingLimitation class.
        /// </summary>
        /// <param name="limitationType">Possible values include: 'CardCashIn',
        /// 'CardCashOut', 'CryptoCashIn', 'CryptoCashOut', 'SwiftCashIn',
        /// 'SwiftCashOut', 'OverallCashIn', 'OverallCashOut',
        /// 'CardAndSwiftCashIn'</param>
        public RemainingLimitation(LimitationType limitationType, string asset, double remainingAmount, double limitAmount)
        {
            LimitationType = limitationType;
            Asset = asset;
            RemainingAmount = remainingAmount;
            LimitAmount = limitAmount;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets possible values include: 'CardCashIn', 'CardCashOut',
        /// 'CryptoCashIn', 'CryptoCashOut', 'SwiftCashIn', 'SwiftCashOut',
        /// 'OverallCashIn', 'OverallCashOut', 'CardAndSwiftCashIn'
        /// </summary>
        [JsonProperty(PropertyName = "LimitationType")]
        public LimitationType LimitationType { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Asset")]
        public string Asset { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "RemainingAmount")]
        public double RemainingAmount { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "LimitAmount")]
        public double LimitAmount { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (Asset == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Asset");
            }
        }
    }
}
