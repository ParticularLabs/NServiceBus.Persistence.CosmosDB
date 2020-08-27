﻿namespace NServiceBus.PersistenceTesting
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Fluent;
    using NServiceBus.Outbox;
    using NServiceBus.Sagas;
    using Persistence;
    using Persistence.ComponentTests;
    using Persistence.CosmosDB;

    public partial class PersistenceTestsConfiguration
    {
        public bool SupportsDtc => false;

        public bool SupportsOutbox => false;

        public bool SupportsFinders => false;

        public bool SupportsPessimisticConcurrency => false;

        public ISagaIdGenerator SagaIdGenerator { get; } = new SagaIdGenerator();

        public ISagaPersister SagaStorage { get; private set; }

        public ISynchronizedStorage SynchronizedStorage { get; private set; }

        public ISynchronizedStorageAdapter SynchronizedStorageAdapter { get; private set; }

        public IOutboxStorage OutboxStorage { get; private set; }

        public Task Configure()
        {
            var connectionStringEnvironmentVariableName = "CosmosDBPersistence_ConnectionString";
            var connectionString = GetEnvironmentVariable(connectionStringEnvironmentVariableName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception($"Oh no! We couldn't find an environment variable '{connectionStringEnvironmentVariableName}' with Cosmos DB connection string.");
            }

            SynchronizedStorage = new SynchronizedStorageForTesting();

            var builder = new CosmosClientBuilder(connectionString);
            builder.AddCustomHandlers(new LoggingHandler());

            cosmosDbClient = builder.Build();
            SagaStorage = new SagaPersister(cosmosDbClient, databaseName, containerName);

            return Task.CompletedTask;
        }

        public async Task Cleanup()
        {
            var container = cosmosDbClient.GetContainer(databaseName, containerName);
            await container.DeleteContainerAsync();
        }

        static string GetEnvironmentVariable(string variable)
        {
            var candidate = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User);
            return string.IsNullOrWhiteSpace(candidate) ? Environment.GetEnvironmentVariable(variable) : candidate;
        }

        class LoggingHandler : RequestHandler
        {
            ILog logger = LogManager.GetLogger<LoggingHandler>();

            public override async Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
            {
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if ((int)response.StatusCode == 429)
                {
                    logger.Info("Request throttled");
                }

                return response;
            }
        }

        const string databaseName = "CosmosDBPersistence";
        readonly string containerName = $"Test_{DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture)}";
        CosmosClient cosmosDbClient;
    }
}