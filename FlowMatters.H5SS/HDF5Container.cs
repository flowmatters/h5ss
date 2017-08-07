using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using HDF.PInvoke;

namespace FlowMatters.H5SS
{
    public class HDF5Container : HDF5Object
    {
        internal Int64 h5ID;
        protected string name;

        public HDF5Container parent { get; protected set; }

        public IDictionary<string,HDF5Group> Groups
        {
            get
            {
                var nameIDs = FindChildren(false);
                var topLevel = nameIDs.Keys.Where(n => !n.Contains("/"));

                Dictionary<string, HDF5Group> result = new Dictionary<string, HDF5Group>();
                foreach( var key in topLevel)
                {
                    result[key] = new HDF5Group(key, this);
                    //var nested = result.Where(kvp => kvp.Key.StartsWith(key + '/'));
                }
                return result;
            }
        }

        public IDictionary<string, HDF5DataSet> DataSets
        {
            get
            {
                var nameIDs = FindChildren(true);
                var topLevel = nameIDs.Keys.Where(n => !n.Contains("/"));

                Dictionary<string, HDF5DataSet> result = new Dictionary<string, HDF5DataSet>();
                foreach (var key in topLevel)
                {
                    result[key] = new HDF5DataSet(key, this);
                    //var nested = result.Where(kvp => kvp.Key.StartsWith(key + '/'));
                }
                return result;

            }
        }

        protected Dictionary<string,long> FindChildren(bool dataSets)
        {
            Dictionary<string,long> datasetNames = new Dictionary<string, long>();
            Dictionary<string,long> groupNames = new Dictionary<string, long>();
            var rootID = Open();

            ulong dummy = 0;
            H5L.iterate(rootID, H5.index_t.NAME, H5.iter_order_t.INC, ref dummy, new H5L.iterate_t(
                delegate (long objectId, IntPtr namePtr, ref H5L.info_t info, IntPtr op_data)
                {
                    string objectName = Marshal.PtrToStringAnsi(namePtr);
                    H5O.info_t gInfo = new H5O.info_t();
                    H5O.get_info_by_name(objectId, objectName, ref gInfo);
                  
                    if (gInfo.type == H5O.type_t.DATASET)
                    {
                        datasetNames[objectName]=objectId;
                    }
                    else if (gInfo.type == H5O.type_t.GROUP)
                    {
                        groupNames[objectName] = objectId;
                    }
                    return 0;
                }), new IntPtr());

            H5G.close(rootID);

            // Print out the information that we found
            foreach (var line in datasetNames)
            {
                Debug.WriteLine(line);
            }

            if (dataSets)
                return datasetNames;
            return groupNames;
        }

        internal long Open()
        {
            if (parent == null)
            {
                return H5G.open(h5ID, name);
            }
            else
            {
                return H5G.open(parent.h5ID, name);
            }
        }

        public override T With<T>(Func<long, T> action)
        {
            var id = Open();
            try
            {
                return action(id);
            }
            finally
            {
                H5G.close(id);
            }
        }

        public override string Name { get { return name; } }

        public override void With(Action<long> action)
        {
            var id = Open();
            try
            {
                action(id);
            }
            finally
            {
                H5G.close(id);
            }
        }

        public void CreateGroup(string name)
        {
            With((id) =>
            {
                var newID = H5G.create(id, name);
                H5G.close(newID);
            });
        }

        public void CreateDataset(string name, Array data,bool[] unlimited=null)
        {
            With((id) =>
            {
                var nDims = data.Rank;
                if (unlimited == null)
                    unlimited = Enumerable.Range(0, nDims).Select(d => false).ToArray();

                ulong[] shape = data.Shape();
                ulong[] maxShape =
                    Enumerable.Range(0, nDims).Select(d => unlimited[d] ? H5S.UNLIMITED : shape[d]).ToArray();
                var dataspaceID = H5S.create_simple(nDims, shape, maxShape);
                var dataTypeID = HDF5DataSet.GetDataType(data);

                var newID = H5D.create(id, name, dataTypeID, dataspaceID);

                H5T.close(dataTypeID);
                H5S.close(dataspaceID);
                Debug.Assert(newID > 0);

                // write!
                H5D.close(newID);
            });
            DataSets[name].Put(data);
        }
    }
}