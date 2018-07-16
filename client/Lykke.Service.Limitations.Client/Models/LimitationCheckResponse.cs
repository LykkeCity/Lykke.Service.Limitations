namespace Lykke.Service.Limitations.Client.Models
{
    /// <summary>
    /// Limitation check response.
    /// </summary>
    public class LimitationCheckResponse
    {
        /// <summary>Check result flag.</summary>
        public bool IsValid { get; set; }

        /// <summary>Error message in case of failed check.</summary>
        public string FailMessage { get; set; }
    }
}
