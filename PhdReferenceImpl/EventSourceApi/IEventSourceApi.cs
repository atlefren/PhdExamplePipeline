using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.EventSourceApi
{
    public interface IEventSourceApi<TData, TDiff>
    {
        Task<IEnumerable<Aggregate<TData>>> GetDatasetAtLatestVersion(Guid datasetId);
        Task<Aggregate<TData>> GetAggregateAtLatestVersion(Guid aggregateId);
        Task SaveEvents(IEnumerable<Event<TDiff>> diffs);
    }
}
