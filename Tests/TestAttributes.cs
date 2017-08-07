using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlowMatters.Source.HDF5IO.h5ss.Tests
{
    [TestFixture]
    class TestAttributes : BaseHDF5WriteTest
    {
        public TestAttributes()
        {
            BaseFilename = "AttributesTest.h5";
        }

        [Test]
        public void CreateRootAttributes()
        {
            TestAttributesOnTarget(()=>file);
        }

        private void TestAttributesOnTarget(Func<HDF5Object> getTarget)
        {
            long created = new DateTime(2017, 7, 3).Ticks;
            TestBeforeAndAfter(
                () =>
                {
                    var target = getTarget();
                    target.Attributes.Create("SchemaVersion", "1.0");
                    target.Attributes.Create("Created", created);
                },
                () =>
                {
                    var target = getTarget();
                    Assert.IsTrue(target.Attributes.Contains("SchemaVersion"));
                    Assert.IsTrue(target.Attributes.Contains("Created"));

                    Assert.AreEqual("1.0", (string) target.Attributes["SchemaVersion"]);
                    Assert.AreEqual(created, (long) target.Attributes["Created"]);
                });
        }

        [Test]
        public void IdentifyAttributes( )
        {
            long[] longs = Enumerable.Range(0, 500).Select(i => (long) i).ToArray();
            file.CreateDataset("Test Data", longs);
            var target = file.DataSets["Test Data"];

            long created = new DateTime(2017, 7, 3).Ticks;
            TestBeforeAndAfter(
                () =>
                {
                    target.Attributes.Create("SchemaVersion", "1.0");
                    target.Attributes.Create("Created", created);
                },
                () =>
                {
                    target = file.DataSets["Test Data"];
                    Assert.IsTrue(target.Attributes.Contains("SchemaVersion"));
                    Assert.IsTrue(target.Attributes.Contains("Created"));

                    Assert.IsFalse(target.Attributes.Contains("NonExistentAttribute"));
                });

        }

        [Test]
        public void CreateGroupAttributes()
        {
            file.CreateGroup("Test Group");
            TestAttributesOnTarget(()=>file.Groups["Test Group"]);
        }

        [Test]
        public void CreateDatasetAttributes()
        {
            long[] longs = Enumerable.Range(0, 500).Select(i => (long)i).ToArray();
            file.CreateDataset("Test Data",longs);
            TestAttributesOnTarget(()=>file.DataSets["Test Data"]);
        }
    }
}
