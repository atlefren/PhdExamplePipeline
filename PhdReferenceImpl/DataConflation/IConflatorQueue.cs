using GeoAPI.Geometries;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.DataConflation
{
    public interface IConflatorQueue<TGeometry, TAttributesA, TAttributesB> where TGeometry: IGeometry
    {
        public void AddConflationTask(Feature<TGeometry, TAttributesA> featureA,
            Feature<TGeometry, TAttributesB> featureb);
    }
}
