using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Limitations.Client.Models.Request
{
    public class LimitationCheckRequest
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
