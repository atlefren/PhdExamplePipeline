using System.Xml.Xsl;
using NetTopologySuite.Geometries;
using NUnit.Framework;
using PhdReferenceImpl.FeatureDiffer;
using PhdReferenceImpl.Models;
using Tests.ExampleImplementation;

namespace Tests
{
    [TestFixture]
    public class FeatureDiffPatchTest
    {
        private readonly FeatureDiffPatch<Point, ExampleAttributes> _featureDiffPatch = new FeatureDiffPatch<Point, ExampleAttributes>();

        private readonly Feature<Point, ExampleAttributes> _v1 = new Feature<Point, ExampleAttributes>()
        {
            Attributes = new ExampleAttributes()
            {
                Id = 1,
                Name = "v1"
            },
            Geometry = new Point(1,2)
        };
        private readonly Feature<Point, ExampleAttributes> _v2 = new Feature<Point, ExampleAttributes>()
        {
            Attributes = new ExampleAttributes()
            {
                Id = 1,
                Name = "v2"
            },
            Geometry = new Point(1, 1)
        };

        [Test]
        public void TestModify()
        {
            var patch = _featureDiffPatch.Diff(_v1, _v2).Serialize();
            var patched = _featureDiffPatch.Patch(_v1, FeatureDiff.Deserialize(patch));
            CheckEqual(_v2, patched);
        }

        [Test]
        public void TestDelete()
        {
            var patch = _featureDiffPatch.Diff(_v1, null).Serialize();
            var patched = _featureDiffPatch.Patch(_v1, FeatureDiff.Deserialize(patch));
            Assert.IsNull(patched.Geometry);
            Assert.IsNull(patched.Attributes);

        }

        [Test]
        public void TestCreate()
        {
            var patch = _featureDiffPatch.Diff(null, _v1).Serialize();
            var patched = _featureDiffPatch.Patch(null, FeatureDiff.Deserialize(patch));
            CheckEqual(_v1, patched);
        }

        public void CheckEqual(Feature<Point, ExampleAttributes> expected, Feature<Point, ExampleAttributes> actual)
        {
            Assert.AreEqual(expected.Geometry, actual.Geometry);
            Assert.AreEqual(expected.Attributes.Id, actual.Attributes.Id);
            Assert.AreEqual(expected.Attributes.Name, actual.Attributes.Name);
        }

    }
}