using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using GeoAPI.Geometries;
using GeoAPI.IO;
using NetTopologySuite.IO;
using PhdReferenceImpl.Database;
using PhdReferenceImpl.EventStoreApi;
using PhdReferenceImpl.MessageBus;
using PhdReferenceImpl.Models;
using PhdReferenceImpl.Util;

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
        private readonly IEventStoreApi<Feature<TInputGeometry, TInputAttributes>, FeatureDiff> _eventStoreApi;

        private const string AggregateIdColumnName = "AggregateId";
        private const string GeometryColumnName = "Geometry";

        private readonly Func<Feature<TInputGeometry, TInputAttributes>, Feature<TOutputGeometry, TOutputAttributes>>
            _transformFeature;

        private readonly Func<Guid, Feature<TInputGeometry, TInputAttributes>, Task<bool>> _shouldKeepFeature;

        public ReadProjectionWriter(
            IMessageBus<FeatureDiff> messageBus,
            IDatabaseEngine databaseEngine,
            IEventStoreApi<Feature<TInputGeometry, TInputAttributes>, FeatureDiff> eventStoreApi, 
            Func<Feature<TInputGeometry, TInputAttributes>, Feature<TOutputGeometry, TOutputAttributes>> transformFeature,
            Func<Guid, Feature<TInputGeometry, TInputAttributes>, Task<bool>> shouldKeepFeature = null
            )
        {
            _messageBus = messageBus;
            _databaseEngine = databaseEngine;
            _eventStoreApi = eventStoreApi;
            _transformFeature = transformFeature;
            _shouldKeepFeature = shouldKeepFeature;
        }

        public async Task CreateReadProjection(Guid datasetId)
        {
            var tableName = GetTableName(datasetId);
            await _databaseEngine.CreateTable(tableName, GetColumns());
            await InsertExistingFeatures(datasetId, tableName);
            
            _messageBus.Subscribe(datasetId, @event =>
            {
                Update(datasetId, tableName, @event);
            });
        }

        private Task Update(Guid datasetId, string tableName, Event<FeatureDiff> @event)
            => @event.Operation switch
            {
                Operation.Delete => Delete(datasetId, tableName, @event),
                Operation.Create => Create(datasetId, tableName, @event),
                Operation.Modify => Modify(datasetId, tableName, @event),
                _ => throw new Exception("Unsupported operation")
            };

        private Task Delete(Guid datasetId, string tableName, Event<FeatureDiff> @event)
            => _databaseEngine.Delete(tableName, @event.AggregateId);

        private Task Create(Guid datasetId, string tableName, Event<FeatureDiff> @event)
            => Upsert(datasetId, tableName, @event);

        private Task Modify(Guid datasetId, string tableName, Event<FeatureDiff> @event)
            => Upsert(datasetId, tableName, @event);

        private async Task InsertExistingFeatures(Guid datasetId, string tableName)
        {
            var aggregates = await _eventStoreApi.GetAggregatesAtLatestVersion(datasetId);
            await Task.WhenAll(aggregates.Select(aggregate => Upsert(datasetId, tableName, aggregate)));
        }

        private async Task Upsert(Guid datasetId, string tableName, Event<FeatureDiff> @event)
            => await Upsert(datasetId, tableName, await _eventStoreApi.GetAggregateAtLatestVersion(datasetId, @event.AggregateId));
        
private async Task Upsert(
    Guid datasetId,
    string tableName,
    Aggregate<Feature<TInputGeometry, TInputAttributes>> aggregate)
{
    if (!await ShouldKeep(datasetId, aggregate.Data)) {
        return;
    }

    var transformedFeature = _transformFeature(aggregate.Data);
    var rowData = GetRowData(aggregate.Id, transformedFeature);
    await _databaseEngine.Upsert(tableName, rowData);
}

private async Task<bool> ShouldKeep(Guid datasetId, Feature<TInputGeometry, TInputAttributes> feature)
    => _shouldKeepFeature == null || (await _shouldKeepFeature(datasetId, feature));

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
                new Column(){Name = GeometryColumnName, Type = ReflectionHelpers.GetGeometryType<TOutputGeometry>()}
            }.Concat(ReflectionHelpers.GetAttributeColumns<TOutputAttributes>()).ToList();

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
