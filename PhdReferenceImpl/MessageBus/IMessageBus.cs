using System;
using System.Collections.Generic;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.MessageBus
{
    public interface IMessageBus
    {
        void Subscribe(Guid datasetId, Action<Event> callback);

        void Publish(Guid datasetId, IEnumerable<Event> events);
    }
}