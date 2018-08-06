using Lykke.Service.Limitations.Core.Domain;
using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Limitations.Models
{
    public class TierRequestModel
    {
        [Required]
        public Tier Tier { get; set; }

        [Required]
        public string Changer { get; set; }
    }
}
