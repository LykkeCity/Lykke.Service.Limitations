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

    public partial class LimitationCheckResult
    {
        /// <summary>
        /// Initializes a new instance of the LimitationCheckResult class.
        /// </summary>
        public LimitationCheckResult()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the LimitationCheckResult class.
        /// </summary>
        public LimitationCheckResult(bool isValid, string failMessage)
        {
            IsValid = isValid;
            FailMessage = failMessage;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "IsValid")]
        public bool IsValid { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "FailMessage")]
        public string FailMessage { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (FailMessage == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "FailMessage");
            }
        }
    }
}
