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

    public partial class LimitCheckRequestModel
    {
        /// <summary>
        /// Initializes a new instance of the LimitCheckRequestModel class.
        /// </summary>
        public LimitCheckRequestModel()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the LimitCheckRequestModel class.
        /// </summary>
        /// <param name="operationType">Possible values include: 'CardCashIn',
        /// 'CardCashOut', 'CryptoCashIn', 'CryptoCashOut', 'SwiftTransfer',
        /// 'SwiftTransferOut', 'TotalCashIn', 'TotalCashOut'</param>
        public LimitCheckRequestModel(string clientId, string asset, double amount, CurrencyOperationType operationType)
        {
            ClientId = clientId;
            Asset = asset;
            Amount = amount;
            OperationType = operationType;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "ClientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Asset")]
        public string Asset { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "Amount")]
        public double Amount { get; set; }

        /// <summary>
        /// Gets or sets possible values include: 'CardCashIn', 'CardCashOut',
        /// 'CryptoCashIn', 'CryptoCashOut', 'SwiftTransfer',
        /// 'SwiftTransferOut', 'TotalCashIn', 'TotalCashOut'
        /// </summary>
        [JsonProperty(PropertyName = "OperationType")]
        public CurrencyOperationType OperationType { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (ClientId == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "ClientId");
            }
            if (Asset == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Asset");
            }
        }
    }
}
