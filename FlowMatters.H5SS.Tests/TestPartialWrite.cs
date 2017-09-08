using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlowMatters.H5SS.Tests
{
    [TestFixture]
    class TestPartialWrite : BaseHDF5WriteTest
    {
        [Test]
        public void TestCreateDoubleDataset()
        {
            double[] allData = ARangeDouble(0, 100);
            double[] firstBlock = ARangeDouble(0, 50);
            double[] secondBlock = ARangeDouble(50, 50);

            string key = "DoubleData";
            ulong[] shape = {100};

            TestBeforeAndAfter(
                () =>
                {
                    var ds = file.CreateDataset(key, shape, typeof(double));
                    ulong[] offset = {0};
                    ds.Put(firstBlock, offset);
                    offset[0] = 50;
                    ds.Put(secondBlock, offset);
                },
                () =>
                {
                    Assert.IsTrue(file.DataSets.ContainsKey(key));
                    var ds = file.DataSets[key];
                    Assert.AreEqual(ds.Shape, new uint[] { 100 });

                    double[] retrieved = (double[])ds.Get();
                    Assert.AreEqual(allData, retrieved);
                });
        }

        private static double[] ARangeDouble(int start, int count)
        {
            return new List<double>(Enumerable.Range(start, count).Select(i => (double) i)).ToArray();
        }
    }
}
