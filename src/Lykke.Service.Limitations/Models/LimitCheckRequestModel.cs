using Lykke.Service.Limitations.Core.Domain;
using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Limitations.Models
{
    public class LimitCheckRequestModel
    {
        [Required]
        public string ClientId { get; set; }

        [Required]
        public string Asset { get; set; }

        [Required]
        public double Amount { get; set; }

        [Required]
        public CurrencyOperationType OperationType { get; set; }
    }
}
