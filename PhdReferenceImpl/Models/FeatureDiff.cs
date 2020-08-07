using System;
using GeoAPI.Geometries;

namespace PhdReferenceImpl.Models
{
    public class FeatureDiff<TGeometry, TAttributes> where TGeometry : IGeometry
    {
        public string Attributes { get; set; }
        public byte[] Geometry { get; set; }
        public Guid Guid { get; set; }
        public long Version { get; set; }
    }
}
