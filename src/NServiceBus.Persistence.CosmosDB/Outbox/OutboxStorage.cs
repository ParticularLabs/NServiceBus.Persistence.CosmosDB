﻿namespace NServiceBus.Persistence.CosmosDB.Outbox
{
    using System;
    using Features;
    using Newtonsoft.Json;

    class OutboxStorage : Feature
    {
        OutboxStorage()
        {
            Defaults(s => s.EnableFeatureByDefault<SynchronizedStorage>());
            DependsOn<SynchronizedStorage>();
            DependsOn<Outbox>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var serializer = new JsonSerializer {ContractResolver = new CosmosDBContractResolver()};

            var ttlInSeconds = context.Settings.Get<int>(SettingsKeys.OutboxTimeToLiveInSeconds);

            context.Container.ConfigureComponent(builder => new OutboxPersister(builder.Build<ContainerHolder>(), serializer, ttlInSeconds), DependencyLifecycle.SingleInstance);
            context.Pipeline.Register(new LogicalOutboxBehavior(serializer), "Behavior that mimics the outbox as part of the logical stage.");
        }
    }
}