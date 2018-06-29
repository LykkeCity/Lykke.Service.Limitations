using System.ComponentModel.DataAnnotations;


namespace Lykke.Service.Limitations.Core.Domain
{
    public class SwiftTransferLimitation
    {
        [Required]
        [MinLength(1)]
        public string Asset { get; set; }

        [Required]
        public decimal MinimalWithdraw { get; set; }
    }
}
