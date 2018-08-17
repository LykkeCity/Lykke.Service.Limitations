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

    public partial class SwiftTransferLimitation
    {
        /// <summary>
        /// Initializes a new instance of the SwiftTransferLimitation class.
        /// </summary>
        public SwiftTransferLimitation()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the SwiftTransferLimitation class.
        /// </summary>
        public SwiftTransferLimitation(string asset, double minimalWithdraw)
        {
            Asset = asset;
            MinimalWithdraw = minimalWithdraw;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Asset")]
        public string Asset { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "MinimalWithdraw")]
        public double MinimalWithdraw { get; set; }

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
            if (Asset != null)
            {
                if (Asset.Length < 1)
                {
                    throw new ValidationException(ValidationRules.MinLength, "Asset", 1);
                }
            }
        }
    }
}
