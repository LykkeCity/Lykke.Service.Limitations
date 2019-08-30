using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.Limitations.Client.Models
{
    /// <summary>
    /// Approved operation attempt.
    /// </summary>
    public class CurrencyOperationAttempt
    {
        /// <summary>Client id</summary>
        public string ClientId { get; set; }

        /// <summary>Operation amount</summary>
        public double Amount { get; set; }

        /// <summary>Operation asset</summary>
        public string Asset { get; set; }

        /// <summary>Operation type</summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public CurrencyOperationType OperationType { get; set; }
    }
}
