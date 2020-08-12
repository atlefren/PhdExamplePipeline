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
using PhdReferenceImpl.ReadProjectionHandler;

namespace PhdReferenceImpl.EventSourcePipeline
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
    public class EventSourcePipeline<TGeometry, TAttributes>
        where TGeometry : IGeometry
    {
        private readonly IChangeDetector<TGeometry, TAttributes> _changeDetector;
        private readonly IEventSourceApi<TGeometry, TAttributes> _eventSourceApi;
        private readonly IFeatureDiffer<TGeometry, TAttributes> _featureDiffer;
        private readonly IReadProjectionHandler _readProjectionHandler;
        private readonly IMessageBus _messageBus;

        public EventSourcePipeline(
            IChangeDetector<TGeometry, TAttributes> changeDetector,
            IEventSourceApi<TGeometry, TAttributes> eventSourceApi,
            IFeatureDiffer<TGeometry, TAttributes> featureDiffer,
            IMessageBus messageBus,
            IReadProjectionHandler readProjectionHandler)
        {
            _changeDetector = changeDetector;
            _eventSourceApi = eventSourceApi;
            _featureDiffer = featureDiffer;
            _messageBus = messageBus;
            _readProjectionHandler = readProjectionHandler;
        }

        public async Task UpdateDataset(Dataset<TGeometry, TAttributes> dataset)
        {
            //Get current version (version n) of the dataset
            var oldFeatures = await _eventSourceApi.GetDatasetAtLatestVersion(dataset.DatasetId);

            //Use the new and old features to generate a list of pairs with corresponding action
            var changes = await  _changeDetector.FindChanges(oldFeatures,dataset);

            //Create a diff for each pair that is changed, created, or deleted
            var diffs =  _featureDiffer.GetDiffs(changes).ToList();

            await _eventSourceApi.SaveEvents(diffs);

            _messageBus.Publish(dataset.DatasetId, ToEvents(diffs));
        }

        public async Task CreateReadProjection(Guid datasetId)
        {
            await _readProjectionHandler.EnsureTable(datasetId);
            _messageBus.Subscribe(datasetId, (@event => { _readProjectionHandler.Update(datasetId, @event); }));
        }

        private static IEnumerable<Event> ToEvents(IEnumerable<FeatureDiff<TGeometry, TAttributes>> diffs)
            => diffs.Select(d => new Event()
            {
                AggregateId = d.AggregateId,
                Version = d.Version,
                Operation = d.Operation
            });
    }
}
