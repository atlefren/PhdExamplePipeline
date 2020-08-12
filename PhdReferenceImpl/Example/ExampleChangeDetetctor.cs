using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using PhdReferenceImpl.ChangeDetector;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.Example
{
    /*
     * Example implementation of IChangeDetector.
     *
     * This example assumes consistent object ids, and is thus a simplification.
     */
    public class ExampleChangeDetetctor : IChangeDetector<LineString, ExampleAttributes>
    {

        public  Task<IEnumerable<FeaturePair<LineString, ExampleAttributes>>> FindChanges(IEnumerable<Aggregate<Feature<LineString, ExampleAttributes>>> existingVersion, IEnumerable<Feature<LineString, ExampleAttributes>> newVersion)
        {
            var newFeatures = newVersion.ToList();
            var existingAggregates = existingVersion.ToList();
            var oldObjectIds = existingAggregates.Select(f => f.Data.Attributes.Id).ToList();
           
            var newObjectIds = newFeatures.Select(f => f.Attributes.Id).ToList();

            return Task.FromResult(GetDeleted(existingAggregates, newObjectIds)
                .Concat(GetCreated(newFeatures, oldObjectIds))
                .Concat(GetModified(existingAggregates, newFeatures, newObjectIds, oldObjectIds)));
        }
       
        private IEnumerable<FeaturePair<LineString, ExampleAttributes>> GetModified(IEnumerable<Aggregate<Feature<LineString, ExampleAttributes>>> existingAggregates, IEnumerable<Feature<LineString, ExampleAttributes>> newFeatures, IEnumerable<int> newIds, IEnumerable<int> oldIds)
            => newIds.Where(oldIds.Contains)
                .Select(id =>
                {
                    var aggregate = existingAggregates.FirstOrDefault(f => f.Data.Attributes.Id == id);
                    if (aggregate == null)
                    {
                        throw new Exception("Could not fint previous version, although it should be there");
                    }
                    return new FeaturePair<LineString, ExampleAttributes>()
                    {
                        ExistingAggregate = aggregate,
                        NewFeature = newFeatures.FirstOrDefault(f => f.Attributes.Id == id),
                        Operation = Operation.Modify,
                        Version = aggregate.Version + 1,
                        Guid = aggregate.Id
                    };
                }).Where(p => !AreEqual(p.ExistingAggregate.Data, p.NewFeature));

        private static IEnumerable<FeaturePair<LineString, ExampleAttributes>> GetCreated(
            IEnumerable<Feature<LineString, ExampleAttributes>> newFeatures, IEnumerable<int> oldIds)
            => newFeatures
                .Where(newFeature => !oldIds.Contains(newFeature.Attributes.Id))
                .Select(createdFeature => new FeaturePair<LineString, ExampleAttributes>()
                {
                    ExistingAggregate = null,
                    NewFeature = createdFeature,
                    Guid = new Guid(),
                    Version = 1,
                    Operation = Operation.Create
                });

        private static IEnumerable<FeaturePair<LineString, ExampleAttributes>> GetDeleted(IEnumerable<Aggregate<Feature<LineString, ExampleAttributes>>> existingAggregates, IEnumerable<int> newIds)
            => existingAggregates
                .Where(aggregate => !newIds.Contains(aggregate.Data.Attributes.Id))
                .Select(deletedAggregate => new FeaturePair<LineString, ExampleAttributes>()
                {
                    ExistingAggregate = deletedAggregate,
                    NewFeature = null,
                    Guid = deletedAggregate.Id,
                    Version = deletedAggregate.Version + 1,
                    Operation = Operation.Delete
                });

        
        private static bool AreEqual(Feature<LineString, ExampleAttributes> f1, Feature<LineString, ExampleAttributes> f2)
            => f1.Geometry.EqualsExact(f2.Geometry) && f1.Attributes.Name == f2.Attributes.Name;

    }
}