using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;
using GeomDiff;
using JsonDiffPatchDotNet;
using Newtonsoft.Json;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.FeatureDiffer
{
    public class FeatureDiffer<TGeometry, TAttributes>:  IFeatureDiffer<TGeometry, TAttributes>
        where TGeometry : IGeometry
    {
        private readonly JsonDiffPatch _diffPatch = new JsonDiffPatch();
        
        public IEnumerable<FeatureDiff<TGeometry, TAttributes>> GetDiffs(IEnumerable<FeaturePair<TGeometry, TAttributes>> pairs)
            => pairs
                .Where(IsNotNoop)
                .Select(CreateDiff);
        
        private FeatureDiff<TGeometry, TAttributes> CreateDiff(FeaturePair<TGeometry, TAttributes> pair)
            => new FeatureDiff<TGeometry, TAttributes>()
            {
                Guid = pair.Guid,
                Version = pair.Version,
                Attributes = DiffAttributes(pair),
                Geometry = DiffGeometry(pair)
            };

        private string DiffAttributes(FeaturePair<TGeometry, TAttributes> pair)
        {
            if (pair.ExistingAggregate == null)
            {
                return ToJson(pair.NewFeature.Attributes);
            }

            if (pair.NewFeature == null)
            {
                return "";
            }

            return _diffPatch.Diff(ToJson(pair.ExistingAggregate.Data.Attributes), ToJson(pair.NewFeature.Attributes));
        }

        private static byte[] DiffGeometry(FeaturePair<TGeometry, TAttributes> pair)
            => GeometryDifferBinary.Diff(
                pair.ExistingAggregate != null ? pair.ExistingAggregate.Data.Geometry : default, 
                pair.NewFeature != null ? pair.NewFeature.Geometry : default
            );
        
        private static string ToJson(TAttributes attributes)
            => JsonConvert.SerializeObject(attributes);

        private static TAttributes FromJson(string attributesJson)
            => JsonConvert.DeserializeObject<TAttributes>(attributesJson);

        private static bool IsNotNoop(FeaturePair<TGeometry, TAttributes> pair)
            => pair.Operation != Operation.NoOp;

    }
}