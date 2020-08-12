using System.Collections.Generic;
using GeoAPI.Geometries;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.FeatureDiffer
{
    public interface IFeatureDiffer<TGeometry, TAttributes> where TGeometry : IGeometry
    {
        IEnumerable<Event<FeatureDiff>> GetDiffs(IEnumerable<FeaturePair<TGeometry, TAttributes>> pairs);
    }
}