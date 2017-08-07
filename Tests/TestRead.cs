using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace FlowMatters.H5SS.Tests
{
    [TestFixture]
    class TestRead
    {
        private HDF5File file;

        [SetUp]
        public void Setup()
        {
            string testDir = TestContext.CurrentContext.TestDirectory;
            string fn = testDir+@"\..\..\TestData\SimpleTest.h5";
            Assert.IsTrue(File.Exists(fn), String.Format("Expected {0} accessible from {1}", fn, testDir));
            file = new HDF5File(fn);
        }

        [TearDown]
        public void TearDown()
        {
            file.Close();
        }

        [Test]
        public void FindGroups()
        {
            IDictionary<string, HDF5Group> groups = file.Groups;

            Assert.AreEqual(1, groups.Count,
                String.Format("Expected 2 groups, but had {0}:{1}", groups.Count, String.Join(",", groups.Keys)));
            Assert.IsTrue(groups.ContainsKey("GroupA"));
            //Assert.IsTrue(groups.ContainsKey("."));
        }

        [Test]
        public void ConfirmStringDataset()
        {
            IDictionary<string, HDF5DataSet> datasets = file.Groups["GroupA"].DataSets;
            Assert.IsTrue(datasets.ContainsKey("DataA_String"));
            HDF5DataSet d1 = datasets["DataA_String"];
            Assert.AreEqual(1, d1.Shape.Length, "Expected DataA_String to be one dimensional");
            Assert.AreEqual(300, d1.Shape[0]);

            Assert.AreEqual(HDF5DataType.String, d1.DataType);

            String[] vals = (String[]) d1.Get();
            Assert.AreEqual(300,vals.Length);
            Assert.AreEqual("000000",vals[0]);
            Assert.AreEqual("000299", vals[299]);
        }

        [Test]
        public void ConfirmDoubleDataset()
        {
            IDictionary<string, HDF5DataSet> datasets = file.Groups["GroupA"].DataSets;
            Assert.IsTrue(datasets.ContainsKey("DataA1"));

            HDF5DataSet d1 = datasets["DataA1"];
            Assert.AreEqual(2, d1.Shape.Length, "Expected DataA1 to be Two dimensional");
            Assert.AreEqual(10, d1.Shape[0]);
            Assert.AreEqual(1000, d1.Shape[1]);

            Assert.AreEqual(HDF5DataType.Double,d1.DataType);

            double[,] vals = (double[,]) d1.Get();
            Assert.AreEqual(10, vals.GetLength(0));
            Assert.AreEqual(1000, vals.GetLength(1));
            Assert.AreEqual(0.0, vals[0, 0]);
            Assert.AreEqual(1000.0, vals[1, 0]);
            Assert.AreEqual(9999.0, vals[9, 999]);
        }
    }
}
