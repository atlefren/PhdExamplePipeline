using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.EventStoreApi
{
    public interface IEventStoreApi<TData, TDiff>
    {
        Task SaveEvent(Guid datasetId, Event<TDiff> @event);
        Task<IEnumerable<Aggregate<TData>>> GetAggregatesAtLatestVersion(Guid datasetId);
        Task<Aggregate<TData>> GetAggregateAtLatestVersion(Guid datasetId, Guid aggregateId);
    }
}
