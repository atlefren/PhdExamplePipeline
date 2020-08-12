using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoAPI.Geometries;
using PhdReferenceImpl.ChangeDetector;
using PhdReferenceImpl.EventSourceApi;
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
        private readonly IEventSourceApi<Feature<TGeometry, TAttributes>, FeatureDiff> _eventSourceApi;
        private readonly IFeatureDiffer<TGeometry, TAttributes> _featureDiffer;
        private readonly IMessageBus<FeatureDiff> _messageBus;

        public EventSourceConverter(
            IChangeDetector<TGeometry, TAttributes> changeDetector,
            IEventSourceApi<Feature<TGeometry, TAttributes>, FeatureDiff> eventSourceApi,
            IFeatureDiffer<TGeometry, TAttributes> featureDiffer,
            IMessageBus<FeatureDiff> messageBus)
        {
            _changeDetector = changeDetector;
            _eventSourceApi = eventSourceApi;
            _featureDiffer = featureDiffer;
            _messageBus = messageBus;
        }

        public async Task UpdateDataset(Guid datasetId, IEnumerable<Feature<TGeometry, TAttributes>> newFeatures)
        {
            //Get current version (version n) of the dataset
            var oldFeatures = await _eventSourceApi.GetDatasetAtLatestVersion(datasetId);

            //Use the new and old features to generate a list of pairs with corresponding action
            var changes = await  _changeDetector.FindChanges(oldFeatures, newFeatures);

            //Create a diff for each pair that is changed, created, or deleted
            var events = _featureDiffer.GetDiffs(changes).ToList();
            
            await _eventSourceApi.SaveEvents(events);

            _messageBus.Publish(datasetId, events);
        }
    }
}
