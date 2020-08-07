using System.Threading.Tasks;
using GeoAPI.Geometries;
using PhdReferenceImpl.ChangeDetector;
using PhdReferenceImpl.EventSourceApi;
using PhdReferenceImpl.FeatureDiffer;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.EventSourcePipeline
{
    public class EventSourcePipeline<TGeometry, TAttributes>
        where TGeometry : IGeometry
    {
        private readonly IChangeDetector<TGeometry, TAttributes> _changeDetector;
        private readonly IEventSourceApi<TGeometry, TAttributes> _eventSourceApi;
        private readonly IFeatureDiffer<TGeometry, TAttributes> _featureDiffer;

        public EventSourcePipeline(
            IChangeDetector<TGeometry, TAttributes> changeDetector,
            IEventSourceApi<TGeometry, TAttributes> eventSourceApi,
            IFeatureDiffer<TGeometry, TAttributes> featureDiffer)
        {
            _changeDetector = changeDetector;
            _eventSourceApi = eventSourceApi;
            _featureDiffer = featureDiffer;
        }

        /*
         * Gets a new version (n+1) of a dataset as a collection of features,
         * transforms them to events, and saves them to an event store.
         */
        public async Task UpdateDataset(Dataset<TGeometry, TAttributes> dataset)
        {
            //Get current version (version n) of the dataset
            var oldFeatures = await _eventSourceApi.GetDatasetAtLatestVersion(dataset.DatasetId);

            //Use the new and old features to generate a list of pairs with corresponding action
            var changes = await  _changeDetector.FindChanges(oldFeatures,dataset);

            //Create a diff for each pair that is changed, created, or deleted
            var diffs =  _featureDiffer.GetDiffs(changes);

            //save the new events
            await _eventSourceApi.SaveEvents(diffs);
        }
    }
}
