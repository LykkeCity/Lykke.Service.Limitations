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
        /// Asset for <see cref="Total"/> and <see cref="Amount"/>
        /// </summary>
        public string Asset { get; set; }
        /// <summary>
        /// Total deposited amount including <see cref="Amount"/>
        /// </summary>
        public double Total { get; set; }
        /// <summary>
        /// Current deposit amount
        /// </summary>
        public double Amount { get; set; }
    }
}
