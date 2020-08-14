using System;
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
    public class FeatureDiffPatch<TGeometry, TAttributes> : IDiffer<Feature<TGeometry, TAttributes>, FeatureDiff> where TGeometry: IGeometry
    {
        private readonly JsonDiffPatch _jsonDiffPatch = new JsonDiffPatch();

    public FeatureDiff Diff(Feature<TGeometry, TAttributes> v1, Feature<TGeometry, TAttributes> v2)
        =>  new FeatureDiff() {
                AttributeDiff = DiffAttributes(GetAttributes(v1), GetAttributes(v2)),
                GeometryDiff = DiffGeometry(GetGeometry(v1), GetGeometry(v2))
            };

    public Feature<TGeometry, TAttributes> Patch(Feature<TGeometry, TAttributes> v1, FeatureDiff diff)
        => diff == default 
            ? default 
            : new Feature<TGeometry, TAttributes>()
            {
                    Attributes = PatchAttributes(GetAttributes(v1), diff.AttributeDiff),
                    Geometry = PatchGeometry(GetGeometry(v1), diff.GeometryDiff)
            };

        private TAttributes PatchAttributes(TAttributes v1, string diff)
            => FromJson(v1 != null
                ? _jsonDiffPatch.Patch(ToJson(v1), diff)
                : diff);

        private static TGeometry PatchGeometry(TGeometry v1, byte[] diff)
            => (TGeometry) GeometryDifferBinary.Patch(v1, diff);

        private string DiffAttributes(TAttributes v1, TAttributes v2)
            => GetOperation(v1, v1) switch
            {
                Operation.Modify => _jsonDiffPatch.Diff(
                    ToJson(v1),
                    ToJson(v2)
                ),
                Operation.Create => ToJson(v2),
                Operation.Delete => "",
                _ => throw new ArgumentOutOfRangeException()
            };

        private static byte[] DiffGeometry(TGeometry v1, TGeometry v2)
            => GeometryDifferBinary.Diff(v1, v2);

        private static Operation GetOperation<TObj>(TObj v1, TObj v2)
            => v1 == null
                ? Operation.Create
                : v2 == null
                    ? Operation.Delete
                    : Operation.Modify;

        private static string ToJson(TAttributes attributes)
            => JsonConvert.SerializeObject(attributes);

        private static TAttributes FromJson(string json)
            => JsonConvert.DeserializeObject<TAttributes>(json);

        private TAttributes GetAttributes(Feature<TGeometry, TAttributes> feature)
            => feature == default
                ? default
                : feature.Attributes;

        private static TGeometry GetGeometry(Feature<TGeometry, TAttributes> feature)
            => feature == default
                ? default
                : feature.Geometry;
    }
}
