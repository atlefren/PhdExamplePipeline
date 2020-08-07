using System.Collections.Generic;
using GeoAPI.Geometries;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.FeatureDiffer
{
    public interface IFeatureDiffer<TGeometry, TAttributes> where TGeometry : IGeometry
    {
        IEnumerable<FeatureDiff<TGeometry, TAttributes>> GetDiffs(IEnumerable<FeaturePair<TGeometry, TAttributes>> pairs);
    }
}