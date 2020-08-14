using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.FeatureDiffer
{

    public class FeatureDiffer<TGeometry, TAttributes>:  IFeatureDiffer<TGeometry, TAttributes>
        where TGeometry : IGeometry
    {
        private readonly FeatureDiffPatch<TGeometry, TAttributes> _diffPatch = new FeatureDiffPatch<TGeometry, TAttributes>();
        
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