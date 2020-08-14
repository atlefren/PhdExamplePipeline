using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NUnit.Framework;
using PhdReferenceImpl;
using PhdReferenceImpl.EventStoreApi;
using PhdReferenceImpl.FeatureDiffer;
using PhdReferenceImpl.MessageBus;
using PhdReferenceImpl.Models;
using Tests.ExampleImplementation;

namespace Tests
{
    public class EventCreationTest
    {
        private readonly ExampleChangeDetetctor _changeDetector = new ExampleChangeDetetctor();
        private IEventStoreApi<Feature<LineString, ExampleAttributes>, FeatureDiff> _eventStoreApi;
        private IMessageBus<FeatureDiff> _messageBus;
        private EventSourceConverter<LineString, ExampleAttributes> _converter;
        
        private readonly Guid _datasetId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _eventStoreApi = A.Fake<IEventStoreApi<Feature<LineString, ExampleAttributes>, FeatureDiff>>();
            _messageBus = new MessageBus<FeatureDiff>();
            _converter = new EventSourceConverter<LineString, ExampleAttributes>(_changeDetector, _eventStoreApi, _messageBus);
        }

        [Test]
        public async Task TestWriteVersion1ToEventStore()
        {
            A.CallTo(() => _eventStoreApi.GetAggregatesAtLatestVersion(A<Guid>.That.IsEqualTo(_datasetId))).Returns(
                 new List<Aggregate<Feature<LineString, ExampleAttributes>>>()
                );

            var events = new List<Event<FeatureDiff>>();
            A.CallTo(() => _eventStoreApi.SaveEvent(A<Guid>.That.IsEqualTo(_datasetId), A<Event<FeatureDiff>>._))
                .Invokes((Guid id, Event<FeatureDiff> x) => events.Add(x));

            await _converter.UpdateDataset(_datasetId, _version1);
            Assert.AreEqual(6, events.Count());
        }

        [Test]
        public async Task TestWriteVersion2ToEventStore()
        {
            A.CallTo(() => _eventStoreApi.GetAggregatesAtLatestVersion(A<Guid>.That.IsEqualTo(_datasetId))).Returns(
                _version1.Select(f => new Aggregate<Feature<LineString, ExampleAttributes>>()
                {
                    Data = f,
                    Version = 1,
                    Id = new Guid()
                }));

            var events = new List<Event<FeatureDiff>>();
            A.CallTo(() => _eventStoreApi.SaveEvent(A<Guid>.That.IsEqualTo(_datasetId), A<Event<FeatureDiff>>._))
                .Invokes(
                    (Guid id, Event<FeatureDiff> x) => events.Add(x));

            await _converter.UpdateDataset(_datasetId, _version2);
            Assert.AreEqual(6, events.Count());
        }

        private readonly List<Feature<LineString, ExampleAttributes>> _version1 = new List<Feature<LineString, ExampleAttributes>>()
        {
            new Feature<LineString, ExampleAttributes>()
            {
                Attributes = new ExampleAttributes(){Id = 1, Name = "feature 1"},
                Geometry = ReadGeometry<LineString>("LINESTRING(1 1, 1 2, 1 3)")
            },
            new Feature<LineString, ExampleAttributes>()
            {
                Attributes = new ExampleAttributes(){Id = 2, Name = "feature 2"},
                Geometry = ReadGeometry<LineString>("LINESTRING(1 1, 2 1, 3 1)")
            },
            new Feature<LineString, ExampleAttributes>()
            {
                Attributes = new ExampleAttributes(){Id = 3, Name = "feature 3"},
                Geometry = ReadGeometry<LineString>("LINESTRING(1 1, 2 2)")
            },
            new Feature<LineString, ExampleAttributes>()
            {
                Attributes = new ExampleAttributes(){Id = 4, Name = "feature 4"},
                Geometry = ReadGeometry<LineString>("LINESTRING(4 5, 4 8)")
            },
            new Feature<LineString, ExampleAttributes>()
            {
                Attributes = new ExampleAttributes(){Id = 5, Name = "feature 5"},
                Geometry = ReadGeometry<LineString>("LINESTRING(4 5, 4 8)")
            },
            new Feature<LineString, ExampleAttributes>()
            {
                Attributes = new ExampleAttributes(){Id = 6, Name = "feature 6"},
                Geometry = ReadGeometry<LineString>("LINESTRING(2 2, 4 4, 8 8)")
            }
        };
        private readonly List<Feature<LineString, ExampleAttributes>> _version2 = new List<Feature<LineString, ExampleAttributes>>()
        {
            new Feature<LineString, ExampleAttributes>()
            {
                Attributes = new ExampleAttributes(){Id = 1, Name = "feature 1"},
                Geometry = ReadGeometry<LineString>("LINESTRING(1 1, 1 2, 1 3)")
            },
            new Feature<LineString, ExampleAttributes>()
            {
                Attributes = new ExampleAttributes(){Id = 2, Name = "feature 2"},
                Geometry = ReadGeometry<LineString>("LINESTRING(1 1, 2 1, 4 1)")
            },
            new Feature<LineString, ExampleAttributes>()
            {
                Attributes = new ExampleAttributes(){Id = 3, Name = "feature 3"},
                Geometry = ReadGeometry<LineString>("LINESTRING(1 1, 2 2)")
            },
            new Feature<LineString, ExampleAttributes>()
            {
                Attributes = new ExampleAttributes(){Id = 5, Name = "feature 5"},
                Geometry = ReadGeometry<LineString>("LINESTRING(4 5, 5 8)")
            },
            new Feature<LineString, ExampleAttributes>()
            {
                Attributes = new ExampleAttributes(){Id = 7, Name = "feature 7"},
                Geometry = ReadGeometry<LineString>("LINESTRING(5 4, 8 1, 8 2)")
            },
            new Feature<LineString, ExampleAttributes>()
            {
                Attributes = new ExampleAttributes(){Id = 8, Name = "feature 8"},
                Geometry = ReadGeometry<LineString>("LINESTRING(0 0, 2 4)")
            }
        };


        private static TGeometry ReadGeometry<TGeometry>(string wkt) where TGeometry : IGeometry
            => (TGeometry) new WKTReader().Read(wkt);
    }
}