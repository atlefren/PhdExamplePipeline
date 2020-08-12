using System;
using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;
using PhdReferenceImpl.Database;

namespace PhdReferenceImpl.ReadProjectionHandler
{
    public class Helpers
    {
        public static IEnumerable<Column> GetAttributeColumns<TAttributes>()
            => typeof(TAttributes)
                .GetProperties()
                .Select(p => new Column()
                {
                    Name = p.Name,
                    Type = MapType(p.PropertyType)
                });

        private static string MapType(Type type)
            => type.ToString() switch
            {
                "System.Int32" => "integer",
                "System.String" => "string",
                _ => "object"
            };

        public static string GetGeometryType<TGeometry>() where TGeometry: IGeometry
            => typeof(TGeometry).Name.ToLower();
    }
}
