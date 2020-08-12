using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using GeoAPI.Geometries;
using GeoAPI.IO;
using NetTopologySuite.IO;
using PhdReferenceImpl.Database;
using PhdReferenceImpl.EventSourceApi;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.ReadProjectionHandler
{
    public class ReadProjectionHandler<TGeometry, TAttributes> : IReadProjectionHandler 
        where TGeometry: IGeometry
    {
        private readonly IDatabaseEngine _databaseEngine;
        private readonly IEventSourceApi<TGeometry, TAttributes> _eventSourceApi;

        private const string AggregateIdColumnName = "AggregateId";
        private const string GeometryColumnName = "Geometry";

        public ReadProjectionHandler(IDatabaseEngine databaseEngine, IEventSourceApi<TGeometry, TAttributes> eventSourceApi)
        {
            _databaseEngine = databaseEngine;
            _eventSourceApi = eventSourceApi;
        }

        public  Task EnsureTable(Guid datasetId) 
            => _databaseEngine.CreateTable(GetTableName(datasetId), GetColumns());
        
        public Task Update(Guid datasetId, Event @event)
            => (@event.Operation switch
            {
                Operation.Delete => Delete(datasetId, @event),
                Operation.Create => Upsert(datasetId, @event),
                Operation.Modify => Upsert(datasetId, @event),
                _ => throw new Exception()
            });

        private Task Delete(Guid datasetId, Event @event)
            => _databaseEngine.Delete(GetTableName(datasetId), @event.AggregateId);

        private async Task Upsert(Guid datasetId, Event @event)
        {
            var aggregate = await _eventSourceApi.GetAggregateAtLatestVersion(@event.AggregateId);
            await _databaseEngine.Upsert(GetTableName(datasetId), ToRow(aggregate));
        }

        private IEnumerable<Cell> ToRow(Aggregate<Feature<TGeometry, TAttributes>> aggregate)
            => new List<Cell>()
            {
                new Cell(){Key = AggregateIdColumnName, Value = aggregate.Id},
                new Cell(){Key = GeometryColumnName, Value = ToEWkb(aggregate.Data.Geometry)}
            }.Concat(ToCells(aggregate.Data.Attributes));

        private static IEnumerable<Cell> ToCells(TAttributes attributes)
            => TypeDescriptor.GetProperties(attributes).Cast<PropertyDescriptor>().Select(p => new Cell()
            {
                Key = p.Name,
                Value = p.GetValue(attributes)
            });

        private static IEnumerable<Column> GetColumns()
            => new List<Column>()
            {
                new Column(){Name = AggregateIdColumnName, Type = "guid"},
                new Column(){Name = GeometryColumnName, Type = GetGeometryType()}
            }.Concat(GetAttributeColumns()).ToList();

        private static IEnumerable<Column> GetAttributeColumns()
            => typeof(TAttributes)
                .GetProperties()
                .Select(p => new Column()
                {
                    Name = p.Name, 
                    Type = MapType(p.PropertyType)
                });

        //TODO: Should probably create a name that is suitable for the db
        private static string GetTableName(Guid datasetId)
            => datasetId.ToString(); 

        private static string GetGeometryType()
            => typeof(TGeometry).Name.ToLower();
        
        private byte[] ToEWkb(IGeometry geometry) 
            => _wkbWriter.Write(geometry);

        private static string MapType(Type type)
            => type.ToString() switch
            {
                "System.Int32" => "integer",
                "System.String" => "string",
                _ => "object"
            };
        
        private readonly WKBWriter _wkbWriter = new WKBWriter(ByteOrder.LittleEndian, true);
    }
}