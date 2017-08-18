using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlowMatters.H5SS.Tests
{
    [TestFixture]
    class TestWriteCompressed : BaseHDF5WriteTest
    {
        public TestWriteCompressed()
        {
            BaseFilename = "Compressed.h5";
        }

        [Test]
        public void TestCreateDoubleDataset()
        {
            double[,] data = new double[500, 1000];
            for (var row = 0; row < 500; row++)
            {
                for (var col = 0; col < 1000; col++)
                {
                    data[row, col] = row;
                }
            }

            string key = "CompressedDoubleData";

            TestBeforeAndAfter(
                () =>
                {
                    file.CreateDataset(key, data,null,null,true);
                },
                () =>
                {
                    Assert.IsTrue(file.DataSets.ContainsKey(key));
                    var ds = file.DataSets[key];
                    Assert.AreEqual(ds.Shape, new uint[] { 500, 1000 });

                    double[,] retrieved = (double[,])ds.Get();
                    Assert.AreEqual(data, retrieved);
                });
        }

        [Test]
        public void TestCreateStringDataset()
        {
            string[] data = new string[600];
            for (int i = 0; i < 600; i++)
            {
                data[i] = DateTimeFormatInfo.CurrentInfo.MonthNames[i/50];
            }
            var key = "StringDataset";

            TestBeforeAndAfter(
                () =>
                {
                    file.CreateDataset(key, data,null,null,true);
                },
                () =>
                {
                    Assert.IsTrue(file.DataSets.ContainsKey(key));
                    var ds = file.DataSets[key];
                    Assert.AreEqual(new uint[] { 600 }, ds.Shape);
                    string[] retrieved = (string[])ds.Get();
                    Assert.AreEqual(data, retrieved);
                }
            );
        }
    }
}
