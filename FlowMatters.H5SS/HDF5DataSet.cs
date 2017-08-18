using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using HDF.PInvoke;

namespace FlowMatters.H5SS
{
    public enum HDF5DataType
    {
        Float,
        Double,
        Long,
        String
    }

    public class HDF5DataSet : HDF5Object
    {
        private ulong[] _shape;
        private ulong[] _maxDims;
        private int _stringLength;
        private HDF5DataType? _dataType;
        public HDF5DataType DataType
        {
            get
            {
                if (_dataType == null)
                {
                    With(id =>
                    {
                        var dtype = H5D.get_type(id); // Return?
                        if (H5T.equal(dtype,H5T.IEEE_F64LE)>0)
                        {
                            _dataType = HDF5DataType.Double;
                        }
                        else if (H5T.equal(dtype,H5T.IEEE_F32LE)>0)
                        {
                            _dataType = HDF5DataType.Float;
                        }
                        else if (H5T.equal(dtype, H5T.NATIVE_INT64) > 0)
                        {
                            _dataType = HDF5DataType.Long;
                        }
                        else if (H5T.get_class(dtype) == H5T.class_t.STRING)
                        {
                            _dataType = HDF5DataType.String;
                            _stringLength = H5T.get_size(dtype).ToInt32();
                        }
                        else
                        {
                            throw new Exception($"Unknown dtype {dtype}");
                        }
                    });
                }
                return _dataType.Value;
            }
        }

        //private long H5ID;
        public ulong[] Shape
        {
            get
            {
                if (_shape == null)
                {
                    WithDataSpace((datasetID,dataspaceID) =>
                    {
                        var rank = H5S.get_simple_extent_ndims(dataspaceID);
                        _shape = new ulong[rank];
                        _maxDims = new ulong[rank];
                        H5S.get_simple_extent_dims(dataspaceID, _shape, _maxDims);
                    });
                }
                return _shape;
            }
        }

        public override void With(Action<long> action)
        {
            var h5GRef = parent.Open();
            long h5Ref = H5D.open(h5GRef, Name);

            try
            {
                action(h5Ref);
            }
            finally
            {
                H5D.close(h5Ref);
                H5G.close(h5GRef);
            }
        }

        public override T With<T>(Func<long, T> action)
        {
            var h5GRef = parent.Open();
            long h5Ref = H5D.open(h5GRef, Name);
            try
            {
                return action(h5Ref);
            }
            finally
            {
                H5D.close(h5Ref);
                H5G.close(h5GRef);
            }
        }

        public void WithDataSpace(Action<long, long> action)
        {
            With(datasetID =>
            {
                long dataspaceID = H5D.get_space(datasetID);

                try
                {
                    action(datasetID, dataspaceID);
                }
                finally
                {
                    H5S.close(dataspaceID);
                }
            });
        }

        public Array Get()
        {
            Array result = null;
            WithDataSpace((h5Ref, dsRef) =>
            {
                var success = H5S.select_none(dsRef);
                Debug.Assert(success >= 0);
                success = H5S.select_all(dsRef);
                Debug.Assert(success >= 0);
                int selectElemNpoints = (int)H5S.get_select_npoints(dsRef);
                var effectiveSize = ElementSize * selectElemNpoints;
                if (DataType == HDF5DataType.String)
                {
                    effectiveSize *= _stringLength;
                }
                IntPtr iPtr = Marshal.AllocHGlobal(effectiveSize);

                var dtype = H5D.get_type(h5Ref); // Return?
                success = H5D.read(h5Ref, dtype, H5S.ALL, dsRef, H5P.DEFAULT, iPtr);
                Debug.Assert(success >= 0);
                H5T.close(dtype);

                var tmp = CreateClrArray(iPtr, selectElemNpoints);

                var shape = Shape.Select(ul => (long)ul).ToArray();
                if (ClrType == typeof(byte))
                    shape = shape.Concat(new[] { (long)_stringLength }).ToArray();

                result = Array.CreateInstance(ClrType, shape);
                Buffer.BlockCopy(tmp, 0, result, 0, effectiveSize);

                // Convert bytes to characters...
                if (DataType == HDF5DataType.String)
                {
                    byte[,] byteArray = (byte[,])result;
                    result = Enumerable.Range(0, byteArray.GetLength(0)).Select(i =>
                    {
                        var slice = Enumerable.Range(0, byteArray.GetLength(1)).Select(j => byteArray[i, j]).ToArray();
                        //return System.Text.Encoding.Default.GetString(slice);
                        return Encoding.ASCII.GetString(slice).TrimEnd((Char)0);
                    }).ToArray();
                }
                H5S.get_simple_extent_dims(dsRef, _shape, _maxDims); // WTF?
            });
            return result;


        }

        private Array CreateClrArray(IntPtr native,int selectElemNpoints)
        {
            if (ClrType == typeof(byte))
            {
                var size = selectElemNpoints* _stringLength;

                //var tmp = new byte[_stringLength];
                //Marshal.Copy(native, tmp, 0, _stringLength);

                var result = new byte[size];
                Marshal.Copy(native,result,0,size);
                return result;
            }

            if (ClrType == typeof(double))
            {
                var result = new double[selectElemNpoints];
                Marshal.Copy(native, result, 0, selectElemNpoints);
                return result;
            }

            if (ClrType == typeof(float))
            {
                var result = new float[selectElemNpoints];
                Marshal.Copy(native, result, 0, selectElemNpoints);
                return result;
            }

            if (ClrType == typeof(long))
            {
                var result = new long[selectElemNpoints];
                Marshal.Copy(native, result, 0, selectElemNpoints);
                return result;
            }
            throw new ArgumentException("Unknown ClrType");
        }

        private IntPtr CreateNativeArray(Array clrArray,long dtypeID)
        {
            var arraySize = clrArray.Length*ElementSize;

            Array oneD;
            if (clrArray.Rank > 1)
            {
                oneD = Array.CreateInstance(clrArray.ElementType(), clrArray.Length);
                Buffer.BlockCopy(clrArray, 0, oneD, 0, arraySize);
            }
            else
            {
                oneD = clrArray;
            }

            var nativeSize = arraySize;
            if (ClrType == typeof(byte))
                nativeSize *= H5T.get_size(dtypeID).ToInt32();
            var result = Marshal.AllocHGlobal(nativeSize);

            if (ClrType == typeof(byte))
            {
                var maxLength = H5T.get_size(dtypeID).ToInt32();
                string[] allStrings = (string[]) oneD;
                byte[] actualArray = new byte[maxLength*oneD.Length];
                for (var i = 0; i < allStrings.Length; i++)
                {
                    //return System.Text.Encoding.Default.GetString(slice);
                    byte[] bytes = Encoding.ASCII.GetBytes(allStrings[i] + '\0');

                    Buffer.BlockCopy(bytes,0,actualArray,i*maxLength,bytes.Length);
                }
                Marshal.Copy(actualArray, 0, result,actualArray.Length);
            } else if (ClrType == typeof(double))
            {
                Marshal.Copy((double[])oneD, 0, result, oneD.Length);
            }
            else if (ClrType == typeof(float))
            {
                Marshal.Copy((float[])oneD,0,result,oneD.Length);
            }
            else if (ClrType == typeof(long))
            {
                Marshal.Copy((long[]) oneD, 0, result, oneD.Length);
            }
            else
            {
                throw new ArgumentException("Unknown ClrType");
            }

            return result;
        }

        private int ElementSize
        {
            get
            {
                var t = ClrType;

                //if (t == typeof(byte))
                //{
                //    return _stringLength*sizeof(byte);
                //}
                return Marshal.SizeOf(t);
            }
        }

        private Type ClrType
        {
            get
            {
                switch (DataType)
                {
                    case HDF5DataType.Double:
                        return typeof(double);
                    case HDF5DataType.Float:
                        return typeof(float);
                    case HDF5DataType.String:
                        return typeof(byte);
                    case HDF5DataType.Long:
                        return typeof(long);
                }
                return null;
            }
        }

        private HDF5Container parent;

        internal HDF5DataSet(string name, HDF5Container parent)
        {
            this.name = name;
            this.parent = parent;
        }

        ~HDF5DataSet()
        {
            parent = null;
        }

        private string name;

        public override string Name
        {
            get { return name; }
        }

        public void Put(Array data)
        {
            WithDataSpace((h5Ref, dsRef) =>
            {
                IntPtr iPtr;
                var effectiveSize = data.Length*ElementSize;
                //if (DataType == HDF5DataType.String)
                //{
                //    // Convert to byte array...

                //}
                //else
                //{
                //}
                var dtype = H5D.get_type(h5Ref); // Return?

                iPtr = CreateNativeArray(data,dtype);
                // copy to unmanaged array?
                var success = H5D.write(h5Ref, dtype, H5S.ALL, dsRef, H5P.DEFAULT, iPtr);
                if (success < 0)
                {
                    throw new H5SSException("Couldn't write to dataset");
                }

                H5T.close(dtype);

            });
        }

        public static long GetDataType(Array data)
        {
            var clrType = data.ElementType();

            if (clrType == typeof(double))
                return H5T.copy(H5T.NATIVE_DOUBLE);

            if (clrType == typeof(float))
                return H5T.copy(H5T.NATIVE_FLOAT);

            if (clrType == typeof(int))
                return H5T.copy(H5T.NATIVE_INT32);

            if (clrType == typeof(long))
                return H5T.copy(H5T.NATIVE_INT64);

            if (clrType == typeof(string))
            {
                var maxLength = (long) data.OfType<string>().Select(s => s.Length).Max();
                var type = H5T.copy(H5T.C_S1);
                var ptr = new IntPtr(maxLength + 1);//Leak?
                var status = H5T.set_size(type, ptr);
                Debug.Assert(status>=0);
                return type;
            }
            throw new NotImplementedException();
        }
    }

    public static class ArrayExtensions
    {
        public static ulong[] Shape(this Array array)
        {
            return Enumerable.Range(0, array.Rank).Select(dim => (ulong) array.GetLength(dim)).ToArray();
        }

        public static Type ElementType(this Array array)
        {
            return array.GetType().GetElementType();
        }
    }
}