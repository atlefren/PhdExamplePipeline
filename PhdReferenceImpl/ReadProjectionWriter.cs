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
using PhdReferenceImpl.MessageBus;
using PhdReferenceImpl.Models;
using PhdReferenceImpl.ReadProjectionHandler;

namespace PhdReferenceImpl
{
    /*
     * Listen to new events from a dataset on a MessageBus, and update a read projection.
     */
    public class ReadProjectionWriter<TInputGeometry, TInputAttributes, TOutputGeometry, TOutputAttributes>
        where TInputGeometry : IGeometry
        where TOutputGeometry : IGeometry
    {
        private readonly IMessageBus<FeatureDiff> _messageBus;
        private readonly IDatabaseEngine _databaseEngine;
        private readonly IEventSourceApi<Feature<TInputGeometry, TInputAttributes>, FeatureDiff> _eventSourceApi;

        private const string AggregateIdColumnName = "AggregateId";
        private const string GeometryColumnName = "Geometry";

        private readonly Func<Feature<TInputGeometry, TInputAttributes>, Feature<TOutputGeometry, TOutputAttributes>>
            _transformFeature;

        public ReadProjectionWriter(
            IMessageBus<FeatureDiff> messageBus,
            IDatabaseEngine databaseEngine,
            IEventSourceApi<Feature<TInputGeometry, TInputAttributes>, FeatureDiff> eventSourceApi, 
            Func<Feature<TInputGeometry, TInputAttributes>, Feature<TOutputGeometry, TOutputAttributes>> transformFeature)
        {
            _messageBus = messageBus;
            _databaseEngine = databaseEngine;
            _eventSourceApi = eventSourceApi;
            _transformFeature = transformFeature;
        }

        public async Task CreateReadProjection(Guid datasetId)
        {
            var tableName = GetTableName(datasetId);
            await _databaseEngine.CreateTable(tableName, GetColumns());
            _messageBus.Subscribe(datasetId, (@event => { Update(tableName, @event); }));
        }

        private Task Update(string tableName, Event<FeatureDiff> @event)
            => @event.Operation switch
            {
                Operation.Delete => Delete(tableName, @event),
                Operation.Create => Upsert(tableName, @event),
                Operation.Modify => Upsert(tableName, @event),
                _ => throw new Exception("Unsupported operation")
            };

        private Task Delete(string tableName, Event<FeatureDiff> @event)
            => _databaseEngine.Delete(tableName, @event.AggregateId);

        private async Task Upsert(string tableName, Event<FeatureDiff> @event)
        {
            var aggregate = await _eventSourceApi.GetAggregateAtLatestVersion(@event.AggregateId);
            var a  = aggregate.Data;
            await _databaseEngine.Upsert(tableName, GetRowData(aggregate.Id, _transformFeature(aggregate.Data)));
        }

        private static string GetTableName(Guid datasetId)
            => datasetId.ToString();

        private IEnumerable<Cell> GetRowData(Guid aggregateId, Feature<TOutputGeometry, TOutputAttributes> feature)
            => new List<Cell>()
            {
                new Cell(){Key = AggregateIdColumnName, Value = aggregateId},
                new Cell(){Key = GeometryColumnName, Value = ToEWkb(feature.Geometry)}
            }.Concat(MapAttributes(feature.Attributes));

        private static IEnumerable<Column> GetColumns()
            => new List<Column>()
            {
                new Column(){Name = AggregateIdColumnName, Type = "guid"},
                new Column(){Name = GeometryColumnName, Type = Helpers.GetGeometryType<TOutputGeometry>()}
            }.Concat(Helpers.GetAttributeColumns<TOutputAttributes>()).ToList();

        private static IEnumerable<Cell> MapAttributes(TOutputAttributes attributes)
            => TypeDescriptor.GetProperties(attributes).Cast<PropertyDescriptor>().Select(p => new Cell()
            {
                Key = p.Name,
                Value = p.GetValue(attributes)
            });

        private byte[] ToEWkb(IGeometry geometry)
            => geometry != null
                ? _wkbWriter.Write(geometry)
                : null;

        private readonly WKBWriter _wkbWriter = new WKBWriter(ByteOrder.LittleEndian, true);
    }
}
