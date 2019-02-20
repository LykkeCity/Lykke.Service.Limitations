using System.Collections.Generic;
using Autofac;
using Lykke.Common.Log;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;
using Lykke.Service.Assets.Contract.Events;
using Lykke.Service.Limitations.Projections;
using Lykke.Service.Limitations.Settings;
using Lykke.SettingsReader;

namespace Lykke.Service.Limitations.Modules
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

            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>().SingleInstance();
            builder.RegisterType<AssetsProjection>();

            builder.Register(ctx =>  new MessagingEngine(ctx.Resolve<ILogFactory>(),
                new TransportResolver(new Dictionary<string, TransportInfo>
                {
                    {"RabbitMq", new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName, rabbitMqSettings.Password, "None", "RabbitMq")}
                }),
                new RabbitMqTransportFactory(ctx.Resolve<ILogFactory>()))).As<IMessagingEngine>().SingleInstance();

            builder.Register(ctx =>
            {
                var engine = new CqrsEngine(
                    ctx.Resolve<ILogFactory>(),
                    ctx.Resolve<IDependencyResolver>(),
                    ctx.Resolve<IMessagingEngine>(),
                    new DefaultEndpointProvider(),
                    true,

                    Register.DefaultEndpointResolver(
                        new RabbitMqConventionEndpointResolver(
                            "RabbitMq",
                            SerializationFormat.MessagePack,
                            environment: "lykke",
                            exclusiveQueuePostfix: "k8s")),

                    Register.BoundedContext("limitations")
                        .ListeningEvents(typeof(AssetCreatedEvent), typeof(AssetUpdatedEvent)).From("assets").On("events")
                        .WithProjection(typeof(AssetsProjection), "assets"));

                engine.StartPublishers();

                return engine;
            })
            .As<ICqrsEngine>()
            .AutoActivate()
            .SingleInstance();
        }
    }
}
