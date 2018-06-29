using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Services;
using System.Collections.Generic;

namespace Lykke.Service.Limitations.Services
{
    public class HealthService : IHealthService
    {
        public string GetHealthViolationMessage()
        {
            // TODO: Check gathered health statistics, and return appropriate health violation message, or NULL if job hasn't critical errors
            return null;
        }

        public IReadOnlyCollection<HealthIssue> GetHealthIssues()
        {
            var issues = new List<HealthIssue>();

            // TODO: Check gathered health statistics, and add appropriate health issues message to issues

            return issues;
        }
    }
}
