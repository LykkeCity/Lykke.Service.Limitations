using JetBrains.Annotations;
using Lykke.Service.Limitations.Settings;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using Lykke.Sdk;

namespace Lykke.Service.Limitations
{
    [UsedImplicitly]
    public class Startup
    {
        private readonly LykkeSwaggerOptions _swaggerOptions = new LykkeSwaggerOptions
        {
            ApiTitle = "Lykke limitations Service",
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
                    logs.AzureTableName = "LimitationsLogsTable";
                    logs.AzureTableConnectionStringResolver = settings => settings.LimitationsSettings.Log.ConnectionString;                    
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
        }
    }   
}
