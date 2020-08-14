using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoAPI.Geometries;
using PhdReferenceImpl.ChangeDetector;
using PhdReferenceImpl.EventStoreApi;
using PhdReferenceImpl.FeatureDiffer;
using PhdReferenceImpl.MessageBus;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl
{
    /*
     * Example pipeline for converting bulk-updated geospatial vector datasets to an
     * event sourced architecture.
     *
     * The UpdateDataset method takes a list of features, representing a new version
     * of a dataset and converts it to a list of events.
     *
     * The CreateReadProjection method creates a database table for a read projection,
     * and keeps this updated by listening for events.
     */
    public class EventSourceConverter<TGeometry, TAttributes>
        where TGeometry : IGeometry
    {
        private readonly IChangeDetector<TGeometry, TAttributes> _changeDetector;
        private readonly IEventStoreApi<Feature<TGeometry, TAttributes>, FeatureDiff> _eventStoreApi;
        
        private readonly IMessageBus<FeatureDiff> _messageBus;

        private readonly FeatureDiffPatch<TGeometry, TAttributes> _diffPatch = new FeatureDiffPatch<TGeometry, TAttributes>();

        public EventSourceConverter(
            IChangeDetector<TGeometry, TAttributes> changeDetector,
            IEventStoreApi<Feature<TGeometry, TAttributes>, FeatureDiff> eventStoreApi,
            IMessageBus<FeatureDiff> messageBus)
        {
            _changeDetector = changeDetector;
            _eventStoreApi = eventStoreApi;
            _messageBus = messageBus;
        }

        public async Task UpdateDataset(Guid datasetId, IEnumerable<Feature<TGeometry, TAttributes>> newFeatures)
        {
            //Get current version (version n) of the dataset
            var oldFeatures = await _eventStoreApi.GetAggregatesAtLatestVersion(datasetId);

            //Use the new and old features to generate a list of pairs with corresponding action
            var changes = await _changeDetector.FindChanges(oldFeatures, newFeatures);

            //Create a diff for each pair that is changed, created, or deleted
            var events = GetDiffs(changes).ToList();

            await Task.WhenAll(events.Select(@event => StoreEvent(datasetId, @event)).ToArray());
        }

        private async Task StoreEvent(Guid datasetId, Event<FeatureDiff> @event)
        {
            await _eventStoreApi.SaveEvent(datasetId, @event);
            _messageBus.Publish(datasetId, @event);
        }

        public IEnumerable<Event<FeatureDiff>> GetDiffs(IEnumerable<FeaturePair<TGeometry, TAttributes>> pairs)
            => pairs
                .Where(IsNotNoop)
                .Select(CreateDiff);

        private Event<FeatureDiff> CreateDiff(FeaturePair<TGeometry, TAttributes> pair)
            => new Event<FeatureDiff>()
            {
                AggregateId = pair.AggregateId,
                Version = pair.Version,
                EventData = _diffPatch.Diff(pair.ExistingAggregate?.Data, pair.NewFeature)
            };

        private static bool IsNotNoop(FeaturePair<TGeometry, TAttributes> pair)
            => pair.Operation != Operation.NoOperation;
    }
}
