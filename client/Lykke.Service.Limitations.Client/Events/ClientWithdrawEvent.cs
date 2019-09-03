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
        /// Total withdraw amount for the month including <see cref="Amount"/>
        /// </summary>
        public double TotalMonth { get; set; }
        /// <summary>
        /// Total withdraw amount including <see cref="Amount"/>
        /// </summary>
        public double Total { get; set; }
        /// <summary>
        /// Current withdraw amount
        /// </summary>
        public double Amount { get; set; }
    }
}
