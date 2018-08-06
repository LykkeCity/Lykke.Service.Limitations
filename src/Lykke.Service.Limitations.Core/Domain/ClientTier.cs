using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Limitations.Core.Domain
{
    public class ClientTier
    {
        public string ClientId { get; set; }
        public string TierId { get; set; }
        public string Changer { get; set; }
    }
}
