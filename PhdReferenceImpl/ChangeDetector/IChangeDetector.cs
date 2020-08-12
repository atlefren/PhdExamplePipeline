using System.Collections.Generic;
using System.Threading.Tasks;
using GeoAPI.Geometries;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.ChangeDetector
{
    /*
     * Interface describing a class for finding matches between geospatial vector features
     * from two versions of a dataset
     *
     * An example implementation is found in Example/ExampleChangeDetetctor.cs
     */
    public interface IChangeDetector<TGeometry, TAttributes> where TGeometry : IGeometry
    {
        public Task<IEnumerable<FeaturePair<TGeometry, TAttributes>>> FindChanges(
            IEnumerable<Aggregate<Feature<TGeometry, TAttributes>>> existingVersion, 
            Dataset<TGeometry, TAttributes> newVersion
        );
    }
}
