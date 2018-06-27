namespace Lykke.Service.Limitations.Core.Domain
{
    public class LimitationCheckResult
    {
        public bool IsValid { get; set; }

        public string FailMessage { get; set; }
    }
}
