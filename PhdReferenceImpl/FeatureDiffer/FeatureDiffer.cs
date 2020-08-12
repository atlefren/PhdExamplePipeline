using System;
using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;
using GeomDiff;
using JsonDiffPatchDotNet;
using Newtonsoft.Json;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.FeatureDiffer
{
    /*
     * Implementation of a diffing mechanism for geospatial vector features.
     * Uses a combination of GeomDiff for geometries and JsonDiffPatch for attributes.
     *
     * For more information on GeomDiff and this combination, see
     * A. F. Sveen, ‘GeomDiff — an algorithm for differential geospatial vector data comparison’,
     * Open Geospatial Data, Software and Standards, vol. 5, no. 1, pp. 1–11, Jul. 2020, doi: 10.1186/s40965-020-00076-4. 
     */
    public class FeatureDiffer<TGeometry, TAttributes>:  IFeatureDiffer<TGeometry, TAttributes>
        where TGeometry : IGeometry
    {
        private readonly JsonDiffPatch _diffPatch = new JsonDiffPatch();
        
        public IEnumerable<Event<FeatureDiff>> GetDiffs(IEnumerable<FeaturePair<TGeometry, TAttributes>> pairs)
            => pairs
                .Where(IsNotNoop)
                .Select(CreateDiff);
        
        private Event<FeatureDiff> CreateDiff(FeaturePair<TGeometry, TAttributes> pair)
            => new Event<FeatureDiff>()
            {
                AggregateId = pair.Guid,
                Version = pair.Version,
                EventData = new FeatureDiff()
                {
                    AttributeDiff = DiffAttributes(pair),
                    GeometryDiff = DiffGeometry(pair)
                }
            };

        private string DiffAttributes(FeaturePair<TGeometry, TAttributes> pair)
            => GetOperation(pair) switch
                {
                    Operation.Modify => _diffPatch.Diff(
                        ToJson(pair.ExistingAggregate.Data.Attributes),
                        ToJson(pair.NewFeature.Attributes)
                    ),
                    Operation.Create => ToJson(pair.NewFeature.Attributes),
                    Operation.Delete => "",
                    _ => throw new ArgumentOutOfRangeException()
            };

        private static Operation GetOperation(FeaturePair<TGeometry, TAttributes> pair)
            => pair.ExistingAggregate == null 
                ? Operation.Create 
                : pair.NewFeature == null 
                    ? Operation.Delete 
                    : Operation.Modify;
        
        private static byte[] DiffGeometry(FeaturePair<TGeometry, TAttributes> pair)
            => GeometryDifferBinary.Diff(
                pair.ExistingAggregate != null ? pair.ExistingAggregate.Data.Geometry : default, 
                pair.NewFeature != null ? pair.NewFeature.Geometry : default
            );
        
        private static string ToJson(TAttributes attributes)
            => JsonConvert.SerializeObject(attributes);

        private static bool IsNotNoop(FeaturePair<TGeometry, TAttributes> pair)
            => pair.Operation != Operation.NoOp;

    }
}