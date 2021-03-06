﻿namespace NServiceBus.PersistenceTesting.Outbox
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using NUnit.Framework;

    [TestFixtureSource(typeof(PersistenceTestsConfiguration), "OutboxVariants")]
    class ExtendedOutboxStorageTests
    {
        public ExtendedOutboxStorageTests(TestVariant param)
        {
            this.param = param;
        }

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            configuration = new PersistenceTestsConfiguration(param) { OutboxTimeToLiveInSeconds = 1 };
            await configuration.Configure();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await configuration.Cleanup();
        }

        [Test]
        public async Task Should_expire_dispatched_messages()
        {
            configuration.RequiresOutboxSupport();

            var storage = configuration.OutboxStorage;
            var ctx = configuration.GetContextBagForOutbox();

            var messageId = Guid.NewGuid().ToString();
            await storage.Get(messageId, ctx);

            var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });
            using (var transaction = await storage.BeginTransaction(ctx))
            {
                await storage.Store(messageToStore, transaction, ctx);

                await transaction.Commit();
            }

            await storage.SetAsDispatched(messageId, configuration.GetContextBagForOutbox());

            OutboxMessage message = null;
            for (int i = 1; i < 10; i++)
            {
                message = await storage.Get(messageId, configuration.GetContextBagForOutbox());
                if (message != null)
                {
                    await Task.Delay(i * 550);
                }
            }

            Assert.That(message, Is.Null, "The outbox record was not expired.");
        }

        IPersistenceTestsConfiguration configuration;
        TestVariant param;
    }
}