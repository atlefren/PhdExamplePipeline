using System;
using GeoAPI.Geometries;

namespace PhdReferenceImpl.Models
{
    public class FeaturePair<TGeometry, TAttributes> where TGeometry : IGeometry
    {
        public Aggregate<Feature<TGeometry, TAttributes>> ExistingAggregate;
        public Feature<TGeometry, TAttributes> NewFeature;
        public Operation Operation;
        public Guid AggregateId { get; set; }
        public long Version { get; set; }
    }
}
