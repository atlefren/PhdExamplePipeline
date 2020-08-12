using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NUnit.Framework;
using PhdReferenceImpl.Database;
using PhdReferenceImpl.EventSourceApi;
using PhdReferenceImpl.Example;
using PhdReferenceImpl.Models;
using PhdReferenceImpl.ReadProjectionHandler;

namespace Tests
{
    /*
    [TestFixture]
    public class ReadProjectionHandlerTest
    {
        private IDatabaseEngine _databaseEngine;
        private IEventSourceApi<LineString, ExampleAttributes> _eventSourceApi;

        private readonly Guid _datasetId = Guid.NewGuid();

        [SetUp]
        public void SetUp()
        {
            _databaseEngine = A.Fake<IDatabaseEngine>();
            _eventSourceApi = A.Fake<IEventSourceApi<LineString, ExampleAttributes>>();
        }

        [Test]
        public async Task TestCreateTable()
        {
            var handler = new ReadProjectionHandler<LineString, ExampleAttributes, LineString, ExampleAttributes>(_databaseEngine, _eventSourceApi);

            await handler.EnsureTable(_datasetId);

            A.CallTo(() => _databaseEngine.CreateTable(
                    A<string>.That.IsEqualTo(_datasetId.ToString()), 
                    A<List<Column>>.That.Matches((columns => CheckColumns(columns))))
                )
                .MustHaveHappenedOnceExactly();

        }

        private static bool CheckColumns(IReadOnlyList<Column> cols)
        {
            Assert.AreEqual(4, cols.Count);
            
            Assert.AreEqual("AggregateId", cols[0].Name);
            Assert.AreEqual("guid", cols[0].Type);
            Assert.AreEqual("Geometry", cols[1].Name);
            Assert.AreEqual("linestring", cols[1].Type);
            Assert.AreEqual("Id", cols[2].Name);
            Assert.AreEqual("integer", cols[2].Type);
            Assert.AreEqual("Name", cols[3].Name);
            Assert.AreEqual("string", cols[3].Type);
            return true;
        }

        [Test]
        public async Task TestDelete()
        {
            var handler = new ReadProjectionHandler<LineString, ExampleAttributes, LineString, ExampleAttributes>(_databaseEngine, _eventSourceApi);
            var @event = new Event()
            {
                AggregateId = Guid.NewGuid(),
                Operation = Operation.Delete,
                Version = 1
            };

            await handler.Update(_datasetId, @event);

            A.CallTo(() => _databaseEngine.Delete(_datasetId.ToString(), @event.AggregateId))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task TestCreate()
        {
            var handler = new ReadProjectionHandler<LineString, ExampleAttributes, LineString, ExampleAttributes>(_databaseEngine, _eventSourceApi);
            var @event = new Event()
            {
                AggregateId = Guid.NewGuid(),
                Operation = Operation.Create,
                Version = 1
            };
            var feature = new Feature<LineString, ExampleAttributes>()
            {
                Geometry = GetLineString(),
                Attributes = new ExampleAttributes()
                {
                    Id = 1,
                    Name = "feature 1"
                }
            };
            A.CallTo(() => _eventSourceApi.GetAggregateAtLatestVersion(A<Guid>.That.IsEqualTo(@event.AggregateId)))
                .Returns(new Aggregate<Feature<LineString, ExampleAttributes>>()
                {
                    Id = @event.AggregateId,
                    Version = @event.Version,
                    Data = feature
                
                });

            await handler.Update(_datasetId, @event);

            A.CallTo(() => _databaseEngine.Upsert(_datasetId.ToString(),
                    A<IEnumerable<Cell>>.That.Matches((cells => CheckCells(cells, @event.AggregateId, feature))))
                    )
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task TestModify()
        {
            var handler = new ReadProjectionHandler<LineString, ExampleAttributes, LineString, ExampleAttributes>(_databaseEngine, _eventSourceApi);
            var @event = new Event()
            {
                AggregateId = Guid.NewGuid(),
                Operation = Operation.Modify,
                Version = 1
            };

            var feature = new Feature<LineString, ExampleAttributes>()
            {
                Geometry = GetLineString(),
                Attributes = new ExampleAttributes()
                {
                    Id = 1,
                    Name = "feature 1"
                }
            };
            A.CallTo(() => _eventSourceApi.GetAggregateAtLatestVersion(A<Guid>.That.IsEqualTo(@event.AggregateId)))
                .Returns(new Aggregate<Feature<LineString, ExampleAttributes>>()
                {
                    Id = @event.AggregateId,
                    Version = @event.Version,
                    Data = feature
                });

            await handler.Update(_datasetId, @event);

            A.CallTo(() => _databaseEngine.Upsert(_datasetId.ToString(),
                    A<IEnumerable<Cell>>.That.Matches((cells => CheckCells(cells, @event.AggregateId, feature))))
                )
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task TestModifyWithTransform()
        {
            var transform = A.Fake<Func<Feature<LineString, ExampleAttributes>, Feature<LineString, ExampleAttributes>>>();
            
            var handler = new ReadProjectionHandler<LineString, ExampleAttributes, LineString, ExampleAttributes>(_databaseEngine, _eventSourceApi, transform);
            var @event = new Event()
            {
                AggregateId = Guid.NewGuid(),
                Operation = Operation.Modify,
                Version = 1
            };
            var feature = new Feature<LineString, ExampleAttributes>()
            {
                Geometry = GetLineString(),
                Attributes = new ExampleAttributes()
                {
                    Id = 1,
                    Name = "feature 1"
                }
            };

            var transformedFeature = new Feature<LineString, ExampleAttributes>()
            {
                Geometry = GetLineString("LINESTRING(1 1, 2 2)"),
                Attributes = new ExampleAttributes()
                {
                    Id = 1,
                    Name = "name transformed"
                }
            };

            A.CallTo(() => transform(A<Feature<LineString, ExampleAttributes>>._))
                .Returns(transformedFeature);

            A.CallTo(() => _eventSourceApi.GetAggregateAtLatestVersion(A<Guid>.That.IsEqualTo(@event.AggregateId)))
                .Returns(new Aggregate<Feature<LineString, ExampleAttributes>>()
                {
                    Id = @event.AggregateId,
                    Version = @event.Version,
                    Data = feature
                });

            await handler.Update(_datasetId, @event);

            A.CallTo(() => _databaseEngine.Upsert(_datasetId.ToString(),
                    A<IEnumerable<Cell>>.That.Matches((cells => CheckCells(cells, @event.AggregateId, transformedFeature))))
                )
                .MustHaveHappenedOnceExactly();
        }



        private static LineString GetLineString(string wkt = "LINESTRING(1 1, 2 2, 3 3)")
        {
            var reader = new WKTReader();
            var ls = (LineString) reader.Read(wkt);
            ls.SRID = 4326;
            return ls;
        }

        private  bool CheckCells(IEnumerable<Cell> c, Guid aggregateId, Feature<LineString, ExampleAttributes> feature)
        {
            var cells = c.ToList();
            Assert.AreEqual(4, cells.Count);
            Assert.AreEqual("AggregateId", cells[0].Key);
            Assert.AreEqual(aggregateId, cells[0].Value);
            
            Assert.AreEqual("Geometry", cells[1].Key);

            var readGeom = new WKBReader().Read((byte[]) cells[1].Value);
            Assert.AreEqual(feature.Geometry, readGeom);
            Assert.AreEqual(4326, readGeom.SRID);

            Assert.AreEqual("Id", cells[2].Key);
            Assert.AreEqual(feature.Attributes.Id, cells[2].Value);

            Assert.AreEqual("Name", cells[3].Key);
            Assert.AreEqual(feature.Attributes.Name, cells[3].Value);

            return true;
        }
    

    }
    */
}