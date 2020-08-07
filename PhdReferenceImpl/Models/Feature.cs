using System;
using GeoAPI.Geometries;

namespace PhdReferenceImpl.Models
{
    public class Feature<TGeometry, TAttributes> where TGeometry: IGeometry
    {
       
        public TGeometry Geometry;
        public TAttributes Attributes;
    }
}
