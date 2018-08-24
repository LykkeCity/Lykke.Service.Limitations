using Lykke.Job.LimitOperationsCollector.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using JetBrains.Annotations;
using Lykke.Sdk;
using Microsoft.ApplicationInsights.Extensibility;

namespace Lykke.Job.LimitOperationsCollector
{
    public class Startup
    {
        private readonly LykkeSwaggerOptions _swaggerOptions = new LykkeSwaggerOptions
        {
            ApiTitle = "LimitOperationsCollector API",
            ApiVersion = "v1"
        };

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildServiceProvider<AppSettings>(options =>
            {
                options.SwaggerOptions = _swaggerOptions;

                options.Logs = logs =>
                {
                    logs.AzureTableName = "LimitOperationsCollectorLog";
                    logs.AzureTableConnectionStringResolver = x => x.LimitOperationsCollectorJob.LogsConnString;
                };
            });
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseLykkeConfiguration(options =>
            {
                options.SwaggerOptions = _swaggerOptions;
            });

#if DEBUG
            TelemetryConfiguration.Active.DisableTelemetry = true;
#endif
        }
    }
}
