using System.Collections.Generic;
using Autofac;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Cqrs.Middleware.Logging;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Job.LimitOperationsCollector.Settings;
using Lykke.Job.LimitOperationsCollector.Cqrs;
using Lykke.Messaging.Contract;
using Lykke.Messaging.Serialization;
using Lykke.Service.Limitations.Client;
using Lykke.Service.Limitations.Client.Events;
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
            builder.RegisterType<FiatTransfersProjection>().PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);

            var msgPackResolver = new RabbitMqConventionEndpointResolver(
                "RabbitMq",
                SerializationFormat.MessagePack,
                environment: "lykke",
                exclusiveQueuePostfix: "k8s");

            builder.Register(ctx =>
            {
                var engine = new CqrsEngine(ctx.Resolve<ILogFactory>(),
                    ctx.Resolve<IDependencyResolver>(),
                    ctx.Resolve<IMessagingEngine>(),
                    new DefaultEndpointProvider(),
                    true,

                    Register.EventInterceptors(new DefaultEventLoggingInterceptor(ctx.Resolve<ILogFactory>())),

                    Register.DefaultEndpointResolver(
                        new RabbitMqConventionEndpointResolver(
                            "RabbitMq",
                            SerializationFormat.ProtoBuf,
                            environment: "lykke",
                            exclusiveQueuePostfix: "k8s")),

                    Register.BoundedContext(LimitationsBoundedContext.Name)
                        .PublishingEvents(
                            typeof(ClientDepositEvent),
                            typeof(ClientWithdrawEvent)
                        ).With("events")
                        .WithEndpointResolver(msgPackResolver),

                    Register.BoundedContext("limit-operations-collector")
                        .ListeningEvents(typeof(TransferCreatedEvent)).From("me-cy").On("events")
                        .ListeningEvents(typeof(TransferCreatedEvent)).From("me-vu").On("events")
                        .ListeningEvents(typeof(TransferCreatedEvent)).From("me").On("events")
                        .WithProjection(typeof(FiatTransfersProjection), "me-cy")
                        .WithProjection(typeof(FiatTransfersProjection), "me-vu")
                        .WithProjection(typeof(FiatTransfersProjection), "me"));

                engine.StartPublishers();
                return engine;

            })
            .As<ICqrsEngine>()
            .AutoActivate()
            .SingleInstance();
        }
    }
}
