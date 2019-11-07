namespace Lykke.Service.Limitations.Client.Events
{
    /// <summary>
    /// Operation removed event
    /// </summary>
    public class ClientOperationRemovedEvent
    {
        /// <summary>
        /// Client id
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// Id of removed operation
        /// </summary>
        public string OperationId { get; set; }
    }
}
