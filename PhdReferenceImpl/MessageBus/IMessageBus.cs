using System;
using System.Collections.Generic;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.MessageBus
{
public interface IMessageBus<TEventData>
{
    void Subscribe(Guid datasetId, Action<Event<TEventData>> callback);

    void Publish(Guid datasetId, IEnumerable<Event<TEventData>> events);

    void Publish(Guid datasetId, Event<TEventData> @event);
}
}