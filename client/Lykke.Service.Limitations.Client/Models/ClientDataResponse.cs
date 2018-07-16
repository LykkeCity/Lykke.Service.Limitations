using System.Collections.Generic;

namespace Lykke.Service.Limitations.Client.Models
{
    /// <summary>
    /// Client operations container.
    /// </summary>
    public class ClientDataResponse
    {
        /// <summary>Remaining limitations for chosen preiod.</summary>
        public List<RemainingLimitation> RemainingLimits { get; set; }

        /// <summary>Collection of client cashin/out operations.</summary>
        public List<CashOperation> CashOperations { get; set; }

        /// <summary>Collection of client cash transfer operations.</summary>
        public List<CashOperation> CashTransferOperations { get; set; }

        /// <summary>Collection of client approved operation attempts.</summary>
        public List<CurrencyOperationAttempt> OperationAttempts { get; set; }
    }
}
