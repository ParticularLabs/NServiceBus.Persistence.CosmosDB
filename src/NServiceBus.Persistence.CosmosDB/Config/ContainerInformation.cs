﻿namespace NServiceBus
{
    using Persistence.CosmosDB;

    /// <summary>
    /// </summary>
    public readonly struct ContainerInformation
    {
        /// <summary>
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="partitionKeyPath"></param>
        public ContainerInformation(string containerName, PartitionKeyPath partitionKeyPath)
        {
            Guard.AgainstNullAndEmpty(nameof(containerName), containerName);

            ContainerName = containerName;
            PartitionKeyPath = partitionKeyPath;
        }

        /// <summary>
        /// </summary>
        public string ContainerName { get; }

        /// <summary>
        /// </summary>
        public PartitionKeyPath PartitionKeyPath { get; }
    }
}