using System;

namespace Lykke.Service.Limitations.Core.Domain
{
    public class LimitSettings
    {
        public int LowCashOutTimeoutMins { get; set; }
        public int LowCashOutLimit { get; set; }       
    }
}
