using System;
using System.Collections.Generic;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.MessageBus
{
    /*
     * A simple implementation of a messaging service (pubsub) for demonstration purposes
     */
    public class MessageBus<TDiff> : IMessageBus<TDiff>
    {
        private readonly Dictionary<Guid, List<Action<Event<TDiff>>>> _subscribers = new Dictionary<Guid, List<Action<Event<TDiff>>>>();

        public void Subscribe(Guid datasetId, Action<Event<TDiff>> callback)
        {
            if (!_subscribers.ContainsKey(datasetId))
            {
                _subscribers[datasetId] = new List<Action<Event<TDiff>>>();
            }
            _subscribers[datasetId].Add(callback);
        }

        public void Publish(Guid datasetId, IEnumerable<Event<TDiff>> events)
        {
            foreach (var @event in events)
            {
                Publish(datasetId, @event);
            }
        }

        public void Publish(Guid datasetId, Event<TDiff> @event)
        {
            if (_subscribers.ContainsKey(datasetId))
            {
                foreach (var callback in _subscribers[datasetId])
                {
                    callback(@event);
                }
            }
        }
    }
}