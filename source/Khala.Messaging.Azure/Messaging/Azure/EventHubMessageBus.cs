﻿namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Provides the implementation of <see cref="IMessageBus"/> for Azure Event Hubs.
    /// </summary>
    public sealed class EventHubMessageBus : IMessageBus
    {
        private readonly EventMessageSender _eventSender;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubMessageBus"/> class.
        /// </summary>
        /// <param name="eventSender">An <see cref="EventMessageSender"/>.</param>
        public EventHubMessageBus(EventMessageSender eventSender)
        {
            _eventSender = eventSender ?? throw new ArgumentNullException(nameof(eventSender));
        }

#pragma warning disable SA1642 // Constructor summary documentation must begin with standard text
        /// <summary>
        /// This constructor is obsolete. Use <see cref="EventHubMessageBus(EventMessageSender)"/> instead.
        /// </summary>
        /// <param name="eventSender">An <see cref="EventMessageSender"/>.</param>
        /// <param name="serializer">An <see cref="EventDataSerializer"/> to serialize messages.</param>
        [Obsolete("Use EventHubMessageBus(EventDataSender) instead. This constructor will be removed in version 1.0.0.")]
        public EventHubMessageBus(
            EventMessageSender eventSender,
            EventDataSerializer serializer)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            _eventSender = eventSender ?? throw new ArgumentNullException(nameof(eventSender));
        }

        /// <summary>
        /// This constructor is obsolete. Use <see cref="EventHubMessageBus(EventMessageSender)"/> instead.
        /// </summary>
        /// <param name="eventHubClient">An <see cref="EventHubClient"/>.</param>
        /// <param name="serializer">An <see cref="EventDataSerializer"/> to serialize messages.</param>
        [Obsolete("Use EventHubMessageBus(EventDataSender) instead. This constructor will be removed in version 1.0.0.")]
        public EventHubMessageBus(
            EventHubClient eventHubClient,
            EventDataSerializer serializer)
            : this(new EventMessageSender(eventHubClient), serializer)
        {
        }

        /// <summary>
        /// This constructor is obsolete. Use <see cref="EventHubMessageBus(EventMessageSender)"/> instead.
        /// </summary>
        /// <param name="eventHubClient">An <see cref="EventHubClient"/>.</param>
        /// <param name="messageSerializer">An <see cref="IMessageSerializer"/> to serialize messages.</param>
        [Obsolete("Use EventHubMessageBus(EventDataSender) instead. This constructor will be removed in version 1.0.0.")]
        public EventHubMessageBus(
            EventHubClient eventHubClient,
            IMessageSerializer messageSerializer)
            : this(eventHubClient, new EventDataSerializer(messageSerializer))
        {
        }

        /// <summary>
        /// This constructor is obsolete. Use <see cref="EventHubMessageBus(EventHubClient)"/> instead.
        /// </summary>
        /// <param name="eventHubClient">An <see cref="EventHubClient"/>.</param>
        [Obsolete("Use EventHubMessageBus(EventDataSender) instead. This constructor will be removed in version 1.0.0.")]
        public EventHubMessageBus(EventHubClient eventHubClient)
            : this(eventHubClient, new EventDataSerializer())
        {
        }
#pragma warning restore SA1642 // Constructor summary documentation must begin with standard text

        /// <summary>
        /// Sends a single enveloped message to event hub.
        /// </summary>
        /// <param name="envelope">An enveloped message to be sent.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task Send(
            Envelope envelope,
            CancellationToken cancellationToken)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            string partitionKey = (envelope.Message as IPartitioned)?.PartitionKey;
            return _eventSender.Send(new[] { envelope }, partitionKey);
        }

        /// <summary>
        /// Sends multiple enveloped messages to event hub sequentially and atomically.
        /// </summary>
        /// <param name="envelopes">A seqeunce contains enveloped messages.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task Send(
            IEnumerable<Envelope> envelopes,
            CancellationToken cancellationToken)
        {
            if (envelopes == null)
            {
                throw new ArgumentNullException(nameof(envelopes));
            }

            IReadOnlyList<Envelope> envelopeList =
                envelopes as IReadOnlyList<Envelope> ?? envelopes.ToList();

            if (envelopeList.Count == 0)
            {
                return Task.CompletedTask;
            }

            for (int i = 0; i < envelopeList.Count; i++)
            {
                Envelope envelope = envelopeList[i];
                if (envelope == null)
                {
                    throw new ArgumentException(
                        $"{nameof(envelopes)} cannot contain null.",
                        nameof(envelopes));
                }
            }

            object firstMessage = envelopeList[0].Message;
            string partitionKey = (firstMessage as IPartitioned)?.PartitionKey;

            for (int i = 1; i < envelopeList.Count; i++)
            {
                Envelope envelope = envelopeList[i];
                if ((envelope.Message as IPartitioned)?.PartitionKey != partitionKey)
                {
                    throw new ArgumentException(
                        "All messages should have same parition key.",
                        nameof(envelopes));
                }
            }

            return _eventSender.Send(envelopeList, partitionKey);
        }
    }
}
