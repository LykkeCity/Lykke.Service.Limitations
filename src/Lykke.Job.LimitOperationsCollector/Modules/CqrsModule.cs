using System.Collections.Generic;
using Autofac;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Job.LimitOperationsCollector.Settings;
using Lykke.Job.LimitOperationsCollector.Cqrs;
using Lykke.Messaging.Contract;
using Lykke.Messaging.Serialization;
using Lykke.Service.Limitations.Services.Contracts.FxPaygate;
using Lykke.SettingsReader;

namespace Lykke.Job.LimitOperationsCollector.Modules
{
    public class CqrsModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public CqrsModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory { Uri = _appSettings.CurrentValue.SagasRabbitMq.RabbitConnectionString };

            builder.Register(ctx => new MessagingEngine(ctx.Resolve<ILogFactory>(),
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {"RabbitMq", new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName, rabbitMqSettings.Password, "None", "RabbitMq")}
                }),
                new RabbitMqTransportFactory(ctx.Resolve<ILogFactory>()))).As<IMessagingEngine>();

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();
            builder.RegisterType<FiatTransfersProjection>();
            builder.Register(ctx =>
            {
                return new CqrsEngine(ctx.Resolve<ILogFactory>(),
                    ctx.Resolve<IDependencyResolver>(),
                    ctx.Resolve<IMessagingEngine>(),
                    new DefaultEndpointProvider(),
                    true,

                    Register.DefaultEndpointResolver(
                        new RabbitMqConventionEndpointResolver(
                            "RabbitMq",
                            SerializationFormat.ProtoBuf,
                            environment: "lykke",
                            exclusiveQueuePostfix: "k8s")),

                    Register.BoundedContext("limit-operations-collector")
                        .ListeningEvents(typeof(TransferCreatedEvent)).From("me-cy").On("events")
                        .ListeningEvents(typeof(TransferCreatedEvent)).From("me-vu").On("events")
                        .WithProjection(typeof(FiatTransfersProjection), "me-cy")
                        .WithProjection(typeof(FiatTransfersProjection), "me-vu"));

            })
            .As<ICqrsEngine>()
            .SingleInstance();
        }
    }
}
