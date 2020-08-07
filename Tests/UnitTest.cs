using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using NetTopologySuite.IO;
using NUnit.Framework;
using PhdReferenceImpl.EventSourceApi;
using PhdReferenceImpl.EventSourcePipeline;
using PhdReferenceImpl.Example;
using PhdReferenceImpl.FeatureDiffer;
using PhdReferenceImpl.Models;

namespace Tests
{
    public class Tests
    {
        private readonly ExampleChangeDetetctor _changeDetector = new ExampleChangeDetetctor();
        private IEventSourceApi<LineString, ExampleAttributes> _eventSourceApi;
        private EventSourcePipeline<LineString, ExampleAttributes> _pipeline;
        private Dataset<LineString, ExampleAttributes> _version1;
        private Dataset<LineString, ExampleAttributes> _version2;

        private readonly Guid _datsetId = new Guid();

        [SetUp]
        public void Setup()
        {
            _version1 = GetVersion1(_datsetId);
            _version2 = GetVersion2(_datsetId);
            _eventSourceApi = A.Fake<IEventSourceApi<LineString, ExampleAttributes>>();
            _pipeline = new EventSourcePipeline<LineString, ExampleAttributes>(_changeDetector, _eventSourceApi, new FeatureDiffer<LineString, ExampleAttributes>());
        }

        [Test]
        public async Task TestVersion1()
        {
            A.CallTo(() => _eventSourceApi.GetDatasetAtLatestVersion(A<Guid>.That.IsEqualTo(_datsetId))).Returns(
                 new List<Aggregate<Feature<LineString, ExampleAttributes>>>()
                );

            IEnumerable<FeatureDiff<LineString, ExampleAttributes>> events = new List<FeatureDiff<LineString, ExampleAttributes>>();
            A.CallTo(() => _eventSourceApi.SaveEvents(A<IEnumerable<FeatureDiff<LineString, ExampleAttributes>>>._))
                .Invokes(
                    (IEnumerable<FeatureDiff<LineString, ExampleAttributes>> x) => events = x);

            await _pipeline.UpdateDataset(_version1);
            Assert.AreEqual(6, events.Count());
        }

        [Test]
        public async Task TestVersion2()
        {
            A.CallTo(() => _eventSourceApi.GetDatasetAtLatestVersion(A<Guid>.That.IsEqualTo(_datsetId))).Returns(
                _version1.Features.Select(f => new Aggregate<Feature<LineString, ExampleAttributes>>()
                {
                    Data = f,
                    Version = 1,
                    Id = new Guid()
                }));

            IEnumerable <FeatureDiff<LineString, ExampleAttributes>> events = new List<FeatureDiff<LineString, ExampleAttributes>>();
            
            A.CallTo(() => _eventSourceApi.SaveEvents(A<IEnumerable<FeatureDiff<LineString, ExampleAttributes>>>._))
                .Invokes(
                    (IEnumerable<FeatureDiff<LineString, ExampleAttributes>> x) => events = x);

            await _pipeline.UpdateDataset(_version2);
            Assert.AreEqual(6, events.Count());
        }


        private static Dataset<LineString, ExampleAttributes> GetVersion1(Guid id)
            => new Dataset<LineString, ExampleAttributes>()
            {
                DatasetId = id,
                Features = new List<Feature<LineString, ExampleAttributes>>()
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
                }
            };

        private static Dataset<LineString, ExampleAttributes> GetVersion2(Guid id)
           => new Dataset<LineString, ExampleAttributes>()
           {
               DatasetId = id,
               Features = new List<Feature<LineString, ExampleAttributes>>()
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
               }
           };

        private static TGeometry ReadGeometry<TGeometry>(string wkt) where TGeometry : IGeometry
            => (TGeometry) new WKTReader().Read(wkt);
    }
}