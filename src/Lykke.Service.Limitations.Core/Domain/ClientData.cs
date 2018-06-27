using System.Collections.Generic;

namespace Lykke.Service.Limitations.Core.Domain
{
    public class ClientData
    {
        public List<RemainingLimitation> RemainingLimits { get; set; }

        public List<CashOperation> CashOperations { get; set; }

        public List<CashOperation> CashTransferOperations { get; set; }

        public List<CurrencyOperationAttempt> OperationAttempts { get; set; }
    }
}
