using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeoAPI.Geometries;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.EventSourceApi
{
    public interface IEventSourceApi<TGeometry, TAttributes> where TGeometry : IGeometry
    {
        Task<IEnumerable<Aggregate<Feature<TGeometry, TAttributes>>>> GetDatasetAtLatestVersion(Guid datasetId);
        Task<Aggregate<Feature<TGeometry, TAttributes>>> GetAggregateAtLatestVersion(Guid aggregateId);
        Task SaveEvents(IEnumerable<FeatureDiff<TGeometry, TAttributes>> diffs);
    }
}
