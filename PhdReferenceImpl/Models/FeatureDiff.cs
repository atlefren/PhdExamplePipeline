
using GeoAPI.Geometries;

namespace PhdReferenceImpl.Models
{
    public class FeatureDiff<TGeometry, TAttributes> : Event where TGeometry : IGeometry 
    {
        public string Attributes { get; set; }
        public byte[] Geometry { get; set; }
       
    }
}
