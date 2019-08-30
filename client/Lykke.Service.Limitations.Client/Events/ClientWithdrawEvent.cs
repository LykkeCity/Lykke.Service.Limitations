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
    }
}
