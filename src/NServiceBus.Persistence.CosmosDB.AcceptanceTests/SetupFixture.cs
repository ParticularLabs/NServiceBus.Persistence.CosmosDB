﻿namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Fluent;
    using NUnit.Framework;
    using Persistence.CosmosDB;

    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            var connectionStringEnvironmentVariableName = "CosmosDBPersistence_ConnectionString";
            var connectionString = GetEnvironmentVariable(connectionStringEnvironmentVariableName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception($"Oh no! We couldn't find an environment variable '{connectionStringEnvironmentVariableName}' with Cosmos DB connection string.");
            }

            ContainerName = $"{DateTime.UtcNow.Ticks}_{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}";

            var builder = new CosmosClientBuilder(connectionString);
            builder.AddCustomHandlers(new LoggingHandler());

            CosmosDbClient = builder.Build();

            await CosmosDBPersistenceInstaller.CreateContainerIfNotExists(new InstallerSettings
            {
                Client = CosmosDbClient,
                ContainerName = ContainerName,
                DatabaseName = DatabaseName,
                Disabled = false,
                PartitionKeyPath = PartitionPathKey
            });

            var database = CosmosDbClient.GetDatabase(DatabaseName);
            Container = database.GetContainer(ContainerName);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await Container.DeleteContainerStreamAsync();
            CosmosDbClient.Dispose();
        }

        static string GetEnvironmentVariable(string variable)
        {
            var candidate = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User);
            return string.IsNullOrWhiteSpace(candidate) ? Environment.GetEnvironmentVariable(variable) : candidate;
        }

        public const string DatabaseName = "CosmosDBPersistence";
        public const string PartitionPathKey = "/deep/down";
        public static string ContainerName;
        public static CosmosClient CosmosDbClient;
        public static Container Container;
        static double totalRequestCharges;

        class LoggingHandler : RequestHandler
        {
            public override async Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
            {
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                var requestCharge = response.Headers["x-ms-request-charge"];
                await TestContext.Progress.WriteLineAsync($"Charged RUs:{requestCharge} for {request.Method.Method} {request.RequestUri} IsBatch:{request.Headers["x-ms-cosmos-is-batch-request"]}");
                totalRequestCharges += Convert.ToDouble(requestCharge);

                await TestContext.Progress.WriteLineAsync($"Total charged RUs: {totalRequestCharges}");

                if ((int)response.StatusCode == 429)
                {
                    await TestContext.Progress.WriteLineAsync("Request throttled.");
                }

                return response;
            }
        }
    }
}