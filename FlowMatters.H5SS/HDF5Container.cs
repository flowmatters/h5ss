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
        ~HDF5Container()
        {
            parent = null;
        }

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

        public HDF5DataSet CreateDataset(string name, Array data,
            bool[] unlimited=null,ulong[] chunkShape=null,bool compress=false)
        {
            HDF5DataSet result = null;
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

                long creationPropertyList = 0L;
                if (compress)
                {
                    if (chunkShape == null)
                        chunkShape = shape;

                    creationPropertyList =H5P.create(H5P.DATASET_CREATE);
                    H5P.set_layout(creationPropertyList, H5D.layout_t.CHUNKED);
                    H5P.set_deflate(creationPropertyList, 9);
                    H5P.set_chunk(creationPropertyList, shape.Length, chunkShape);
                }

                var newID = H5D.create(id, name, dataTypeID, dataspaceID,0L,creationPropertyList,0L);
                if (newID <= 0)
                {
                    throw new H5SSException("Couldn't create DataSet");
                }

                if (creationPropertyList>0)
                    H5P.close(creationPropertyList);
                H5T.close(dataTypeID);
                H5S.close(dataspaceID);

                // write!
                H5D.close(newID);
                result = new HDF5DataSet(name,this);

            });
            result.Put(data);
            return result;
        }
    }
}