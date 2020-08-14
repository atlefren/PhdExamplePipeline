using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using GeoAPI.Geometries;
using GeoAPI.IO;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NUnit.Framework;
using PhdReferenceImpl;
using PhdReferenceImpl.Database;
using PhdReferenceImpl.EventStoreApi;
using PhdReferenceImpl.MessageBus;
using PhdReferenceImpl.Models;
using Tests.ExampleImplementation;


namespace Tests
{
    [TestFixture]
    public class ReadprojectionWriterTest
    {
        private ReadProjectionWriter<Polygon, ExampleAttributes, Polygon, ExampleAttributes> _readProjectionWriter;
        private ReadProjectionWriter<Polygon, ExampleAttributes, Polygon, ExampleAttributes> _readProjectionWriterWithFilter;
        private ReadProjectionWriter<Polygon, ExampleAttributes, Point, ExampleAttributes2> _readProjectionWriterWithTransform;
        
        private IMessageBus<FeatureDiff> _messageBus;
        private IEventStoreApi<Feature<Polygon, ExampleAttributes>, FeatureDiff> _eventStoreApi;
        private IDatabaseEngine _databaseEngine;

        private readonly Guid _datasetId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _eventStoreApi = A.Fake<IEventStoreApi<Feature<Polygon, ExampleAttributes>, FeatureDiff>>();
            var messageBus = new MessageBus<FeatureDiff>();
            _messageBus = A.Fake<IMessageBus<FeatureDiff>>(x => x.Wrapping(messageBus));

            _databaseEngine = A.Fake<IDatabaseEngine>();
            _readProjectionWriter = new ReadProjectionWriter<Polygon, ExampleAttributes, Polygon, ExampleAttributes>(_messageBus, _databaseEngine, _eventStoreApi, PassTrough);
            
            _readProjectionWriterWithTransform = new ReadProjectionWriter<Polygon, ExampleAttributes, Point, ExampleAttributes2>(_messageBus, _databaseEngine, _eventStoreApi, Transform);
            _readProjectionWriterWithFilter = new ReadProjectionWriter<Polygon, ExampleAttributes, Polygon, ExampleAttributes>(_messageBus, _databaseEngine, _eventStoreApi, PassTrough, ExampleFilter);
        }

        [Test]
        public async Task TestCreateReadProjectionCreatesTable()
        {
            await _readProjectionWriter.CreateReadProjection(_datasetId);

            A.CallTo(() =>
                    _databaseEngine.CreateTable(A<string>.That.IsEqualTo(_datasetId.ToString()), A<IEnumerable<Column>>.That.Matches(c => CheckTableCreation(c))))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task TestCreateReadProjectionSubscribes()
        {
            await _readProjectionWriter.CreateReadProjection(_datasetId);

            A.CallTo(() => _messageBus.Subscribe(A<Guid>.That.IsEqualTo(_datasetId), A<Action<Event<FeatureDiff>>>._))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task TestDeleteEvent()
        {
            var @event = new Event<FeatureDiff>()
            {
                AggregateId = Guid.NewGuid(),
                Operation = Operation.Delete,
                Version = 1
            };
            await _readProjectionWriter.CreateReadProjection(_datasetId);
            _messageBus.Publish(_datasetId, new List<Event<FeatureDiff>>(){@event});

            A.CallTo(() => _databaseEngine.Delete(
                A<string>.That.IsEqualTo(_datasetId.ToString()),
                A<Guid>.That.IsEqualTo(@event.AggregateId)
            )).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task TestCreateEvent()
        {
            var @event = new Event<FeatureDiff>()
            {
                AggregateId = Guid.NewGuid(),
                Operation = Operation.Create,
                Version = 1
            };

            var feature = new Feature<Polygon, ExampleAttributes>()
            {
                Geometry = GetGeometry<Polygon>("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))"),
                Attributes = new ExampleAttributes() { Id = 1, Name = "Feature 1"}
            };

            MockEventSourceApiResponse(@event, feature);

            await _readProjectionWriter.CreateReadProjection(_datasetId);
            _messageBus.Publish(_datasetId, new List<Event<FeatureDiff>>() { @event });

            A.CallTo(() => _databaseEngine.Upsert(
                A<string>.That.IsEqualTo(_datasetId.ToString()),
                A<IEnumerable<Cell>>.That.Matches(c => CheckCellCreation(c, feature, @event.AggregateId)))).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task TestModifyEvent()
        {
            var @event = new Event<FeatureDiff>()
            {
                AggregateId = Guid.NewGuid(),
                Operation = Operation.Modify,
                Version = 1
            };

            var feature = new Feature<Polygon, ExampleAttributes>()
            {
                Geometry = GetGeometry<Polygon>("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))"),
                Attributes = new ExampleAttributes() { Id = 1, Name = "Feature 1" }
            };

            MockEventSourceApiResponse(@event, feature);

            await _readProjectionWriter.CreateReadProjection(_datasetId);
            _messageBus.Publish(_datasetId, new List<Event<FeatureDiff>>() { @event });

            A.CallTo(() => _databaseEngine.Upsert(
                A<string>.That.IsEqualTo(_datasetId.ToString()),
                A<IEnumerable<Cell>>.That.Matches(c => CheckCellCreation(c, feature, @event.AggregateId)))).MustHaveHappenedOnceExactly();
        }


        [Test]
        public async Task TestCreateEventWithTransform()
        {
            var @event = new Event<FeatureDiff>()
            {
                AggregateId = Guid.NewGuid(),
                Operation = Operation.Create,
                Version = 1
            };

            var feature = new Feature<Polygon, ExampleAttributes>()
            {
                Geometry = GetGeometry<Polygon>("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))"),
                Attributes = new ExampleAttributes() { Id = 1, Name = "Feature 1" }
            };
            var transformedFeature = new Feature<Point, ExampleAttributes2>()
            {
                Geometry = GetGeometry<Point>("POINT(0.5 0.5)"),
                Attributes = new ExampleAttributes2() { Id = "1", Name = "Feature 1", Description = "Description of Feature 1"}
            };

            MockEventSourceApiResponse(@event, feature);

            await _readProjectionWriterWithTransform.CreateReadProjection(_datasetId);
            _messageBus.Publish(_datasetId, new List<Event<FeatureDiff>>() { @event });

            A.CallTo(() => _databaseEngine.Upsert(
                A<string>.That.IsEqualTo(_datasetId.ToString()),
                A<IEnumerable<Cell>>.That.Matches(c => CheckCellCreation(c, transformedFeature, @event.AggregateId)))).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task TestCreateEventWithFilterPass()
        {
            var @event = new Event<FeatureDiff>()
            {
                AggregateId = Guid.NewGuid(),
                Operation = Operation.Create,
                Version = 1
            };

            var feature = new Feature<Polygon, ExampleAttributes>()
            {
                Geometry = GetGeometry<Polygon>("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))"),
                Attributes = new ExampleAttributes() { Id = 1, Name = "Feature 1" }
            };

            MockEventSourceApiResponse(@event, feature);

            await _readProjectionWriterWithFilter.CreateReadProjection(_datasetId);
            _messageBus.Publish(_datasetId, new List<Event<FeatureDiff>>() { @event });

            A.CallTo(() => _databaseEngine.Upsert(
                A<string>.That.IsEqualTo(_datasetId.ToString()),
                A<IEnumerable<Cell>>.That.Matches(c => CheckCellCreation(c, feature, @event.AggregateId)))).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task TestCreateEventWithFilterNoPass()
        {
            var @event = new Event<FeatureDiff>()
            {
                AggregateId = Guid.NewGuid(),
                Operation = Operation.Create,
                Version = 1
            };

            var feature = new Feature<Polygon, ExampleAttributes>()
            {
                Geometry = GetGeometry<Polygon>("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))"),
                Attributes = new ExampleAttributes() { Id = 1, Name = "reject" }
            };

            MockEventSourceApiResponse(@event, feature);

            await _readProjectionWriterWithFilter.CreateReadProjection(_datasetId);
            _messageBus.Publish(_datasetId, new List<Event<FeatureDiff>>() { @event });

            A.CallTo(() => _databaseEngine.Upsert(
                A<string>._,
                A<IEnumerable<Cell>>._)).MustNotHaveHappened();
        }


        private void MockEventSourceApiResponse(Event<FeatureDiff> @event, Feature<Polygon, ExampleAttributes> feature)
        {
            A.CallTo(() => _eventStoreApi.GetAggregateAtLatestVersion(A<Guid>.That.IsEqualTo(_datasetId), A<Guid>.That.IsEqualTo(@event.AggregateId)))
                .Returns(new Aggregate<Feature<Polygon, ExampleAttributes>>()
                {
                    Id = @event.AggregateId,
                    Version = @event.Version,
                    Data = feature
                });
        }

        private static bool CheckCellCreation<TGeometry, TAttributes>(IEnumerable<Cell> c,
            Feature<TGeometry, TAttributes> feature, Guid aggregateId) where TGeometry: IGeometry
        {
            var cells = c.ToList();
            var properties = typeof(TAttributes).GetProperties();
            Assert.AreEqual(2 + properties.Length, cells.Count);

            Assert.AreEqual("AggregateId", cells[0].Key);
            Assert.AreEqual(aggregateId, cells[0].Value);

            var geom = new WKBReader().Read((byte[])cells[1].Value);
            Assert.AreEqual("Geometry", cells[1].Key);
            Assert.AreEqual(feature.Geometry, geom);
            Assert.AreEqual(feature.Geometry.SRID, geom.SRID);

            var i = 2;
            foreach (var propertyInfo in properties)
            {
                Assert.AreEqual(propertyInfo.Name, cells[i].Key);
                Assert.AreEqual(propertyInfo.GetValue(feature.Attributes), cells[i].Value);
                i++;
            }

            return true;
        }

        private static bool CheckTableCreation(IEnumerable<Column> c)
        {
            var columns = c.ToList();
            Assert.AreEqual(4, columns.Count);
            Assert.AreEqual("AggregateId", columns[0].Name);
            Assert.AreEqual("guid", columns[0].Type);
            Assert.AreEqual("Geometry", columns[1].Name);
            Assert.AreEqual("polygon", columns[1].Type);
            Assert.AreEqual("Id", columns[2].Name);
            Assert.AreEqual("integer", columns[2].Type);
            Assert.AreEqual("Name", columns[3].Name);
            Assert.AreEqual("string", columns[3].Type);
            return true;
        }


        private static TGeometry GetGeometry<TGeometry>(string wkt = "LINESTRING(1 1, 2 2, 3 3)") where TGeometry: IGeometry
        {
            var reader = new WKTReader();
            var geometry = (TGeometry)reader.Read(wkt);
            geometry.SRID = 4326;
            return geometry;
        }


        private static Feature<Polygon, ExampleAttributes> PassTrough(Feature<Polygon, ExampleAttributes> f) 
            => f;

        private static Feature<Point, ExampleAttributes2> Transform(Feature<Polygon, ExampleAttributes> f) 
            => new Feature<Point, ExampleAttributes2>()
            {
              Geometry  = (Point) f.Geometry.Centroid,
              Attributes = new ExampleAttributes2()
              {
                  Id = f.Attributes.Id.ToString(),
                  Name = f.Attributes.Name,
                  Description = $"Description of {f.Attributes.Name}"
              }
            };

        private static Task<bool> ExampleFilter(Feature<Polygon, ExampleAttributes> feature)
        {
            return Task.FromResult(feature.Attributes.Name != "reject");
        }
    }
}