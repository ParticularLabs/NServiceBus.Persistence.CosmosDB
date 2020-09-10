﻿namespace NServiceBus.Persistence.CosmosDB
{
    using System.Threading.Tasks;
    using Features;
    using Microsoft.Azure.Cosmos;
    using Newtonsoft.Json;
    using Sagas;
    using CosmosDB;

    class CosmosDbSagaPersistence : Feature
    {
        internal CosmosDbSagaPersistence()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<SynchronizedStorage>();
                s.SetDefault<ISagaIdGenerator>(new SagaIdGenerator());
            });
            DependsOn<Sagas>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            // var cosmosClient = context.Settings.Get<CosmosClient>(SettingsKeys.CosmosClient);
            // var databaseName = context.Settings.Get<string>(SettingsKeys.DatabaseName);
            // var sagaMetadataCollection = context.Settings.Get<SagaMetadataCollection>();
            // var partitionAwareConfiguration = context.Settings.Get<PartitionAwareConfiguration>();

            //context.RegisterStartupTask(new InitializeContainers(cosmosClient, databaseName, sagaMetadataCollection, partitionAwareConfiguration));

            context.Container.ConfigureComponent(builder => new SagaPersister(serializerSettings), DependencyLifecycle.SingleInstance);
        }

        class InitializeContainers : FeatureStartupTask
        {
            CosmosClient cosmosClient;
            SagaMetadataCollection sagaMetadataCollection;
            string databaseName;
            PartitionAwareConfiguration partitionAwareConfiguration;

            public InitializeContainers(CosmosClient cosmosClient, string databaseName, SagaMetadataCollection sagaMetadataCollection, PartitionAwareConfiguration partitionAwareConfiguration)
            {
                this.partitionAwareConfiguration = partitionAwareConfiguration;
                this.databaseName = databaseName;
                this.sagaMetadataCollection = sagaMetadataCollection;
                this.cosmosClient = cosmosClient;
            }

            protected override Task OnStart(IMessageSession session)
            {
                return cosmosClient.PopulateContainers(databaseName, sagaMetadataCollection, partitionAwareConfiguration);
            }

            protected override Task OnStop(IMessageSession session)
            {
                // for now here
                cosmosClient.Dispose();
                return Task.CompletedTask;
            }
        }
    }
}