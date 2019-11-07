using System;

namespace Lykke.Service.Limitations.Client.Events
{
    /// <summary>
    /// Withdraw event
    /// </summary>
    public class ClientWithdrawEvent
    {
        /// <summary>
        /// Client id
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// Operation id
        /// </summary>
        public string OperationId { get; set; }
        /// <summary>
        /// Asset for <see cref="Amount"/>
        /// </summary>
        public string Asset { get; set; }
        /// <summary>
        /// Current withdraw amount
        /// </summary>
        public double Amount { get; set; }
        /// <summary>
        /// Asset for <see cref="BaseVolume"/>
        /// </summary>
        public string BaseAsset { get; set; }
        /// <summary>
        /// Deposit amount in <see cref="BaseAsset"/>
        /// </summary>
        public double BaseVolume { get; set; }
        /// <summary>
        /// Operation type
        /// </summary>
        public string OperationType { get; set; }
        /// <summary>
        /// Date time of the operation
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
