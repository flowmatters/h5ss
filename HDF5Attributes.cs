using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HDF.PInvoke;

namespace FlowMatters.Source.HDF5IO.h5ss
{
    public class HDF5Attributes
    {
        public HDF5Attributes(HDF5Object obj)
        {
            Host = obj;
        }

        public HDF5Object Host { get; set; }

        public List<string> Keys
        {
            get
            {
                var keys = new List<string>();
                Host.With((id) =>
                {
                    H5O.info_t attrInfo = new H5O.info_t();
                    var result = H5O.get_info(id, ref attrInfo);
                    if (result < 0)
                    {
                        throw new Exception("Unable to find attribute keys");
                    }

                    var n = (int)attrInfo.num_attrs;

                    for (var i = 0; i < n; i++)
                    {
                        const int MAX_LENGTH = 256;
                        IntPtr name = Marshal.AllocHGlobal(MAX_LENGTH);
                        IntPtr size = new IntPtr(MAX_LENGTH);
//                        var hostName = Host.Name;
                        var actualSize = H5A.get_name_by_idx(id, ".", H5.index_t.CRT_ORDER, H5.iter_order_t.NATIVE, (ulong)i,
                            name, size,0);
                        var actualSizeI = actualSize.ToInt32();
                        if (actualSizeI <= 0)
                        {
                            continue;
                        }
                        var dest = new byte[actualSizeI];

                        Marshal.Copy(name, dest, 0, actualSizeI);
                        keys.Add(Encoding.ASCII.GetString(dest).TrimEnd((Char)0));
                    }
                });
                return keys;
            }
        }

        public void Create(string key, object value)
        {
            Host.With((id) =>
            {
                long dataspace = 0;
                long typeId = 0;
                long attributeId = 0;
                try
                {
                    if (value is Array)
                    {
                        var array = (Array) value;
                        dataspace = H5S.create_simple(array.Rank, array.Shape(), null);
                        throw new NotImplementedException("Scalar attributes only");
                    }
                    else
                    {
                        dataspace = H5S.create(H5S.class_t.SCALAR);
                    }

                    IntPtr nativeMemory;

                    if (value is String)
                    {
                        var str = (string) value;
                        typeId = H5T.copy(H5T.C_S1);
                        H5T.set_size(typeId, new IntPtr(str.Length));
                        nativeMemory = Marshal.StringToHGlobalAnsi(str);
                    } else if (value is long)
                    {
                        var l = new long[] {(long)value};
                        typeId = H5T.copy(H5T.NATIVE_INT64);
                        nativeMemory = Marshal.AllocHGlobal(sizeof(long));
                        Marshal.Copy(l,0,nativeMemory,1); 
                    }
                    else
                    {
                        throw new NotImplementedException("Unsupported attribute type: " + value.GetType().Name);
                    }

                    attributeId = H5A.create(id, key, typeId, dataspace);
                    H5A.write(attributeId, typeId, nativeMemory);
                }
                finally
                {
                    if (attributeId > 0) H5A.close(attributeId);
                    if (typeId > 0) H5T.close(typeId);
                    if (dataspace > 0) H5S.close(dataspace);
                }

            });
        }

        public bool Contains(string key)
        {
            return Host.With((objId) =>
            {
                long attrId = 0;
                try
                {
                    attrId = H5A.open(objId, key);
                    if (attrId < 0)
                    {
                        return false;
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    if (attrId > 0) H5A.close(attrId);
                }
            });
        }

        public object this[string key]
        {
            get
            {
                return Host.With<object>((objId) =>
                {
                    long attrId = 0;
                    long dtypeId = 0;
                    try
                    {
                        attrId = H5A.open(objId, key);
                        if (attrId < 0)
                        {
                            throw new ArgumentException($"Unknown Attribute: {key}", nameof(key));
                        }
                        dtypeId = H5A.get_type(attrId);
                        int size = H5T.get_size(dtypeId).ToInt32();

                        IntPtr iPtr = Marshal.AllocHGlobal(size);
                        int result = H5A.read(attrId, dtypeId, iPtr);
                        if (result < 0)
                        {
                            throw new IOException("Failed to read attribute");
                        }

                        if (H5T.equal(dtypeId, H5T.NATIVE_INT64) > 0)
                        {
                            var dest = new long[1];
                            Marshal.Copy(iPtr,dest,0,1);
                            return dest[0];
                        }
                        else // Must be a string...
                        {
                            var dest = new byte[size];
                            Marshal.Copy(iPtr, dest, 0, size);
                            return Encoding.ASCII.GetString(dest).TrimEnd((Char)0);
                        }

//                        return null;
                    }
                    finally
                    {
                        if (attrId > 0) H5A.close(attrId);
                        if (dtypeId > 0) H5T.close(dtypeId);
                    }
                });
            }
        }
    }
}