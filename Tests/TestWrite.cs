using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlowMatters.Source.HDF5IO.h5ss.Tests
{
    [TestFixture]
    public class TestWrite : BaseHDF5WriteTest
    {
        public TestWrite()
        {
            BaseFilename = "Created.h5";
        }

        [Test]
        public void TestCreateGroup()
        {
            TestBeforeAndAfter(
                () => {
                    file.CreateGroup("NewGroup");
                },
                () => {
                    Assert.IsTrue(file.Groups.ContainsKey("NewGroup"));
                });
        }

        [Test]
        public void TestCreateDoubleDataset()
        {
            double[,] data = new double[,]
            {
                {1,2,3},
                {4,5,6},
                {7,8,9},
                {10,11,12}
            };
            string key = "DoubleData";

            TestBeforeAndAfter(
                () =>
                {
                    file.CreateDataset(key, data);
                },
                () =>
                {
                    Assert.IsTrue(file.DataSets.ContainsKey(key));
                    var ds = file.DataSets[key];
                    Assert.AreEqual(ds.Shape,new uint[] {4,3});

                    double[,] retrieved = (double[,]) ds.Get();
                    Assert.AreEqual(data,retrieved);
                });
        }

        [Test]
        public void TestCreateStringDataset()
        {
            string[] data = new string[]
            {
                "One",
                "Two",
                "Three",
                "Four",
                "Five",
                "Eleven"
            };
            var key = "StringDataset";

            TestBeforeAndAfter(
                () =>
                {
                    file.CreateDataset(key,data);
                },
                () =>
                {
                    Assert.IsTrue(file.DataSets.ContainsKey(key));
                    var ds = file.DataSets[key];
                    Assert.AreEqual(new uint[] { 6 }, ds.Shape);
                    string[] retrieved = (string[]) ds.Get();
                    Assert.AreEqual(data,retrieved);
                }
            );
        }

        [Test]
        public void CreateDateDataset()
        {
            var dates = Enumerable.Range(1900, 100).Select(y => new DateTime(y, 1, 1).Ticks).ToArray();
            var key = "DatesAsTicks";

            TestBeforeAndAfter(
                () =>
                {
                    file.CreateDataset(key,dates);
                },
                () =>
                {
                    Assert.IsTrue(file.DataSets.ContainsKey(key));
                    var ds = file.DataSets[key];
                    Assert.AreEqual(new uint[] { 100 }, ds.Shape);
                    long[] retrieved = (long[])ds.Get();
                    Assert.AreEqual(dates, retrieved);
                });

        }

        [Test]
        public void TestAppend()
        {
            long[] longs = Enumerable.Range(0, 500).Select(i => (long) i).ToArray();
            double[] doubles = Enumerable.Range(1000, 20000).Select(i => (double) i).ToArray();
            string kLong = "long dataset", kDouble="double dataset";

            TestBeforeAndAfter(
                () =>
                {
                    file.CreateDataset(kLong,longs);
                    file.Close();
                    file = new HDF5File(FullFilename,HDF5FileMode.ReadWrite);
                    file.CreateDataset(kDouble, doubles);
                },
                () =>
                {
                    Assert.IsTrue(file.DataSets.ContainsKey(kLong));
                    var ds = file.DataSets[kLong];
                    Assert.AreEqual(new uint[] { 500 }, ds.Shape);
                    long[] retrievedLongs = (long[])ds.Get();
                    Assert.AreEqual(longs, retrievedLongs);

                    Assert.IsTrue(file.DataSets.ContainsKey(kDouble));
                    ds = file.DataSets[kDouble];
                    Assert.AreEqual(new uint[] { 20000 }, ds.Shape);
                    double[] retrievedDoubles = (double[])ds.Get();
                    Assert.AreEqual(doubles, retrievedDoubles);

                }
                );
        }
    }
}
