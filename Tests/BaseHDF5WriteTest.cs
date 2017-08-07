using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlowMatters.Source.HDF5IO.h5ss.Tests
{
    public class BaseHDF5WriteTest
    {
        protected HDF5File file;
        //        string fn = @"..\..\..\hdf5io-source\TestData\Created.h5";
        protected string BaseFilename { private get; set; } = @"Created.h5";
        protected string FullFilename { get; private set; }

        [SetUp]
        public void Setup()
        {
            FullFilename = $"{TestContext.CurrentContext.TestDirectory}\\{BaseFilename}";
            if (File.Exists(FullFilename))
                File.Delete(FullFilename);

            file = new HDF5File(FullFilename, HDF5FileMode.ReadWrite);
            Assert.IsTrue(File.Exists(FullFilename), String.Format("Expected {0} created", FullFilename));
        }

        [TearDown]
        public void TearDown()
        {
            file.Close();
            File.Delete(FullFilename);
        }

        public void ReopenForRead()
        {
            file.Close();
            file = new HDF5File(FullFilename,HDF5FileMode.ReadOnly);
        }

        protected void TestBeforeAndAfter(Action modification, Action confirmation)
        {
            modification();
            confirmation();
            ReopenForRead();
            confirmation();
        }
    }
}
