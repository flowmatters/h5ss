using System.IO;
using HDF.PInvoke;

namespace FlowMatters.H5SS
{
    public enum HDF5FileMode
    {
        ReadWrite = (int) H5F.ACC_RDWR,
        ReadOnly = (int) H5F.ACC_RDONLY,
        WriteNew = (int) H5F.ACC_TRUNC
    }

    public class HDF5File : HDF5Container
    {
        bool closed;
        private H5F.info_t _bhInfo;

        public HDF5File(string fn, HDF5FileMode mode = HDF5FileMode.ReadOnly)
        {
            Filename = fn;
            if (mode == HDF5FileMode.WriteNew)
            {
                Create(fn);
            }
            else if ((mode == HDF5FileMode.ReadOnly) || File.Exists(fn))
            {
                h5ID = H5F.open(fn, (uint) mode);
                ExpectValidFile(fn);
                _bhInfo = new H5F.info_t();
                H5F.get_info(h5ID, ref _bhInfo);
            }
            else
            {
                Create(fn);
            }
            name = "/";
        }

        public string Filename { get; set; }

        private void Create(string fn)
        {
            h5ID = H5F.create(fn, H5F.ACC_TRUNC);
            ExpectValidFile(fn);
        }

        private void ExpectValidFile(string fn)
        {
            if (h5ID <= 0)
                throw new IOException($"Could not open HDF5 File: {fn}, error code {h5ID}");
        }

        public void Close()
        {
            var result = H5F.close(h5ID);
            if (result < 0)
            {
                throw new IOException($"Error closing HDF5 File: {Filename}, error code {result}");
            }
            closed = true;
        }

        ~HDF5File()
        {
            if (!closed && (h5ID>0))
                H5F.close(h5ID);
        }
    }
}
