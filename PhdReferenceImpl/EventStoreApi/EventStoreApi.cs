using PhdReferenceImpl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhdReferenceImpl.EventStore;
using PhdReferenceImpl.FeatureDiffer;

namespace PhdReferenceImpl.EventStoreApi
{
    public class EventStoreApi<TData, TDiff> : IEventStoreApi<TData, TDiff>
    {
        private readonly IDiffer<TData, TDiff> _differ;

        private readonly IEventStorage<TDiff> _eventStorage;

        public EventStoreApi(IEventStorage<TDiff> eventStorage, IDiffer<TData, TDiff> differ)
        {
            _eventStorage = eventStorage;
            _differ = differ;
        }

        public async Task<Aggregate<TData>> GetAggregateAtLatestVersion(Guid datasetId, Guid aggregateId)
            => (await _eventStorage.GetEventsForAggregate(datasetId, aggregateId))
                .OrderBy(e => e.Version)
                .Aggregate(default(Aggregate<TData>), ApplyEvent);

        private Aggregate<TData> ApplyEvent(Aggregate<TData> aggregate, Event<TDiff> @event)
            => new Aggregate<TData>()
            {
                Data = _differ.Patch(aggregate.Data, @event.EventData),
                Id = aggregate.Id,
                Version = @event.Version
            };

        public async Task<IEnumerable<Aggregate<TData>>> GetAggregatesAtLatestVersion(Guid datasetId)
            => await Task.WhenAll((await _eventStorage.GetAggregatesForDataset(datasetId)).Select(aggregateId => GetAggregateAtLatestVersion(datasetId, aggregateId)).ToArray());


        public Task SaveEvent(Guid datasetId, Event<TDiff> @event)
            => _eventStorage.StoreEvent(datasetId, @event);
    }
}