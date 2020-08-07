using System.Collections.Generic;
using System.Threading.Tasks;
using GeoAPI.Geometries;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.ChangeDetector
{
    public interface IChangeDetector<TGeometry, TAttributes> where TGeometry : IGeometry
    {
        public Task<IEnumerable<FeaturePair<TGeometry, TAttributes>>> FindChanges(
            IEnumerable<Aggregate<Feature<TGeometry, TAttributes>>> existingVersion, 
            Dataset<TGeometry, TAttributes> newVersion
        );
    }
}
