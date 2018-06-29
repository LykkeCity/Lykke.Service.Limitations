using System.Collections.Generic;
using Lykke.Service.Limitations.Core.Domain;

namespace Lykke.Service.Limitations.Core.Services
{
    public interface IHealthService
    {
        string GetHealthViolationMessage();

        IReadOnlyCollection<HealthIssue> GetHealthIssues();
    }
}
