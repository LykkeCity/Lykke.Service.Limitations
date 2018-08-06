using System;
using Autofac;

namespace Lykke.Service.Limitations.Client
{
    public static class AutofacExtensions
    {
        public static void RegisterLimitationsServiceClient(this ContainerBuilder builder, string serviceUrl)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (serviceUrl == null)
                throw new ArgumentNullException(nameof(serviceUrl));
            if (string.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serviceUrl));

            builder.RegisterInstance(new LimitationsServiceClient(serviceUrl)).As<ILimitationsServiceClient>().SingleInstance();
            builder.RegisterInstance(new SwiftLimitationServiceClient(serviceUrl)).As<ISwiftLimitationServiceClient>().SingleInstance();
            builder.RegisterInstance(new TiersServiceClient(serviceUrl)).As<ITiersServiceClient>().SingleInstance();
        }
    }
}
