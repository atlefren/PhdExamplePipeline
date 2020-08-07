using System;
using System.Collections.Generic;
using GeoAPI.Geometries;

namespace PhdReferenceImpl.Models
{
    public class Dataset<TGeometry, TAttributes> where TGeometry : IGeometry
    {
        public Guid DatasetId;
        public List<Feature<TGeometry, TAttributes>> Features;
    }
}
