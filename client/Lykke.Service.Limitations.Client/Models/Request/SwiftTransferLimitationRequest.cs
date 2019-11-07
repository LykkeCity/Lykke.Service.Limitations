using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Limitations.Client.Models.Request
{
    public class SwiftTransferLimitationRequest
    {
        [Required]
        [MinLength(1)]
        public string Asset { get; set; }

        [Required]
        public decimal MinimalWithdraw { get; set; }
    }
}
