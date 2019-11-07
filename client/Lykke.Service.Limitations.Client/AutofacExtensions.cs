using System;
using Autofac;
using Lykke.HttpClientGenerator.Infrastructure;

namespace Lykke.Service.Limitations.Client
{
    public static class AutofacExtensions
    {
        /// <summary>
        /// Registers limitations client
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="serviceUrl"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void RegisterLimitationsServiceClient(this ContainerBuilder builder, string serviceUrl)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (string.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serviceUrl));

            builder.RegisterInstance(
                    new LimitationsServiceClient(HttpClientGenerator.HttpClientGenerator.BuildForUrl(serviceUrl)
                        .WithAdditionalCallsWrapper(new ExceptionHandlerCallsWrapper())
                        .Create())
                )
                .As<ILimitationsServiceClient>()
                .SingleInstance();
        }

        /// <summary>
        /// Registers client account client
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="settings"></param>
        public static void RegisterLimitationsServiceClient(this ContainerBuilder builder, LimitationsServiceClientSettings settings)
        {
            builder.RegisterLimitationsServiceClient(settings?.ServiceUrl);
        }
    }
}
