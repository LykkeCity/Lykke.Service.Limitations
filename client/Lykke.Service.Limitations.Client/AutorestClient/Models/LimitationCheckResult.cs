// Code generated by Microsoft (R) AutoRest Code Generator 1.2.2.0
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.

namespace Lykke.Service.Limitations.Client.AutorestClient.Models
{
    using Lykke.Service;
    using Lykke.Service.Limitations;
    using Lykke.Service.Limitations.Client;
    using Lykke.Service.Limitations.Client.AutorestClient;
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
        public LimitationCheckResult(bool? isValid = default(bool?), string failMessage = default(string))
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
        [JsonProperty(PropertyName = "isValid")]
        public bool? IsValid { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "failMessage")]
        public string FailMessage { get; set; }

    }
}
