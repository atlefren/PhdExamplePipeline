using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.EventStore
{
public interface IEventStorage<TEventData>
{
    Task<IEnumerable<Guid>> GetAggregatesForDataset(Guid datasetId);
    Task<IEnumerable<Event<TEventData>>> GetEventsForAggregate(Guid datasetId, Guid aggregateId);
    Task StoreEvent(Guid datasetId, Event<TEventData> @event);
}
}