using System.Collections.Generic;
using FlowMatters.Source.HDF5IO.h5ss.Tests;
using HDF.PInvoke;

namespace FlowMatters.Source.HDF5IO.h5ss
{
    public class HDF5Group : HDF5Container
    {
        //public string Name {
        //    get { return name; }
        //    //set
        //    //{
        //    //    name = value;
        //    //    // Update HDF5
        //    //}
        //}

        public HDF5Group(string name,HDF5Container parent)
        {
            this.name = name;
            this.parent = parent;
            h5ID = H5G.open(parent.h5ID,name);
            H5G.close(h5ID);
        }
    }
}