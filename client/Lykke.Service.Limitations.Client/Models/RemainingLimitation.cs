using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.Limitations.Client.Models
{
    /// <summary>
    /// Remaining amount for limited operation.
    /// </summary>
    public class RemainingLimitation
    {
        /// <summary>Limitation type.</summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public LimitationType LimitationType { get; set; }

        /// <summary>Limitation asset.</summary>
        public string Asset { get; set; }

        /// <summary>Remaining amount.</summary>
        public double RemainingAmount { get; set; }

        public static RemainingLimitation FromModel(AutorestClient.Models.RemainingLimitation model)
        {
            return new RemainingLimitation
            {
                LimitationType = (LimitationType)Enum.Parse(typeof(LimitationType), model.LimitationType),
                Asset = model.Asset,
                RemainingAmount = model.RemainingAmount.Value,
            };
        }
    }
}
