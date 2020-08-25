using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NUnit.Framework;
using PhdReferenceImpl.DataConflation;
using PhdReferenceImpl.Models;
using Tests.ExampleImplementation;

namespace Tests
{
    [TestFixture]
    public class DataConflatorTest
    {
        [Test]
        public void TestDataConflator()
        {
            var conflatorQueue = A.Fake<IConflatorQueue<Polygon, ExampleAttributes, ExampleAttributes2>>();

            //Creat a conflator
            
            var conflator = new DataConflator<Polygon, ExampleAttributes, ExampleAttributes2, ExampleAttributes2>(ShouldBeChecked, MapAttributesA, MapAttributesB, conflatorQueue);

            //conflate the datasets
            var unmerged = conflator.Conflate(Dataset1, Dataset2).ToList();

            //check results
            Assert.AreEqual(4, unmerged.Count);
            Assert.AreEqual(new List<string> { "1", "3", "10", "30" }, unmerged.Select(f => f.Attributes.Id).ToList());

            //check that conflation queue is populated with the two features with id =2
            A.CallTo(() => conflatorQueue.AddConflationTask(
                A<Feature<Polygon, ExampleAttributes>>.That.Matches(f => f.Attributes.Id == 2),
                A<Feature<Polygon, ExampleAttributes2>>.That.Matches(f => f.Attributes.Id == "20")
            )).MustHaveHappenedOnceExactly();

        }

        private static ExampleAttributes2 MapAttributesA(ExampleAttributes a)
        => new ExampleAttributes2()
        {
            Id = a.Id.ToString(),
            Name = a.Name
        };

        private static ExampleAttributes2 MapAttributesB(ExampleAttributes2 b)
            => b;

        private static bool ShouldBeChecked(Feature<Polygon, ExampleAttributes> a, Feature<Polygon, ExampleAttributes2> b) =>
            Intersects(a.Geometry, b.Geometry);

        private static bool Intersects(IGeometry a, IGeometry b)
            => a.Intersects(b);

        private static Polygon Buffer(IGeometry p, double dist)
            => (Polygon) p.Buffer(dist);


        private static readonly List<Feature<Polygon, ExampleAttributes>> Dataset1 = new List<Feature<Polygon, ExampleAttributes>>()
        {
            new Feature<Polygon, ExampleAttributes>(){Geometry = Buffer(new Point(1, 1),0.1), Attributes = new ExampleAttributes()
            {
                Id = 1,
                Name = "1"
            }},
            new Feature<Polygon, ExampleAttributes>(){Geometry = Buffer(new Point(2, 2),0.1), Attributes = new ExampleAttributes()
            {
                Id = 2,
                Name = "2"
            }},
            new Feature<Polygon, ExampleAttributes>(){Geometry = Buffer(new Point(3, 3),0.1), Attributes = new ExampleAttributes()
            {
                Id = 3,
                Name = "3"
            }},
        };


        private static readonly List<Feature<Polygon, ExampleAttributes2>> Dataset2 = new List<Feature<Polygon, ExampleAttributes2>>()
        {
            new Feature<Polygon, ExampleAttributes2>(){Geometry = Buffer(new Point(10, 10),0.1), Attributes = new ExampleAttributes2()
            {
                Id = "10",
                Name = "10",
                Description = "Feature 10"
            }},
            new Feature<Polygon, ExampleAttributes2>(){Geometry = Buffer(new Point(2.1, 2.1),0.2), Attributes = new ExampleAttributes2()
            {
                Id = "20",
                Name = "20",
                Description = "Feature 2 mapped by someone else"
            }},
            new Feature<Polygon, ExampleAttributes2>(){Geometry = Buffer(new Point(30, 0),0.1), Attributes = new ExampleAttributes2()
            {
                Id = "30",
                Name = "30",
                Description = "Feature 30"
            }},
        };
    }
}