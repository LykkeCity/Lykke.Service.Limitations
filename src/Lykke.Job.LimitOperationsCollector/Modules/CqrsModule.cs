using System;
using System.Collections.Generic;
using Autofac;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Job.LimitOperationsCollector.Settings;
using Lykke.Job.LimitOperationsCollector.Cqrs;
using Lykke.Messaging.Serialization;
using Lykke.SettingsReader;

namespace Lykke.Job.LimitOperationsCollector.Modules
{
    public class CqrsModule : Module
    {
        private readonly ILog _log;
        private readonly IReloadingManager<AppSettings> _appSettings;

        public CqrsModule(ILog log, IReloadingManager<AppSettings> appSettings)
        {
            _log = log;
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory { Uri = _appSettings.CurrentValue.LimitOperationsCollectorJob.Rabbit.ConnectionString };

            var messagingEngine = new MessagingEngine(_log,
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {"RabbitMq", new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName, rabbitMqSettings.Password, "None", "RabbitMq")}
                }),
                new RabbitMqTransportFactory());

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();
            builder.RegisterType<FiatTransfersProjection>();
            builder.Register(ctx =>
            {
                return new CqrsEngine(_log,
                    ctx.Resolve<IDependencyResolver>(),
                    messagingEngine,
                    new DefaultEndpointProvider(),
                    true,

                    Register.DefaultEndpointResolver(
                        new RabbitMqConventionEndpointResolver(
                            "RabbitMq",
                            SerializationFormat.ProtoBuf,
                            environment: "lykke",
                            exclusiveQueuePostfix: "k8s")),

                    Register.BoundedContext("limitations")
                        .ListeningEvents(typeof(TransferCreatedEvent)).From("me-cy").On("events")
                        .ListeningEvents(typeof(TransferCreatedEvent)).From("me-vu").On("events")
                        .WithProjection(typeof(FiatTransfersProjection), "me-cy")
                        .WithProjection(typeof(FiatTransfersProjection), "me-vu"));

            }).As<ICqrsEngine>().AutoActivate().SingleInstance();
        }
    }

    internal class AutofacDependencyResolver : IDependencyResolver
    {
        private readonly IComponentContext _context;

        public AutofacDependencyResolver(IComponentContext kernel)
        {
            _context = kernel ?? throw new ArgumentNullException("kernel");
        }

        public object GetService(Type type)
        {
            return _context.Resolve(type);
        }

        public bool HasService(Type type)
        {
            return _context.IsRegistered(type);
        }
    }
}
