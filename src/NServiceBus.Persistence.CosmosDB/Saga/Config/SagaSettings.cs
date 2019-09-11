﻿namespace NServiceBus.Persistence.CosmosDB
{
    using Settings;

    /// <summary>
    /// Configuration options for Saga persistence.
    /// </summary>
    public class SagaSettings
    {
        SettingsHolder settings;

        internal SagaSettings(SettingsHolder settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Connection string to use for sagas storage.
        /// </summary>
        public void ConnectionString(string connectionString)
        {
            Guard.AgainstNullAndEmpty(nameof(connectionString), connectionString);
            
            // TODO: should we assume a single connection string rather than multiple connection strings (per persistence type?)
            settings.Set(WellKnownConfigurationKeys.SagasConnectionString, connectionString);
        }

        /// <summary>
        /// Default collection name to be used
        /// </summary>
        public void DefaultCollectionName(string collectionName)
        {
            Guard.AgainstNullAndEmpty(nameof(collectionName), collectionName);

            // TODO: extract into well known configuration keys
            settings.Set("CosmosDB.Sagas.DefaultCollectionName", collectionName);
        }
    }
}