using System;

namespace FlowMatters.H5SS
{
    public abstract class HDF5Object
    {
        public HDF5Object()
        {
            Attributes = new HDF5Attributes(this);
        }
        public abstract void With(Action<long> action);
        public abstract T With<T>(Func<long, T> action);

        public HDF5Attributes Attributes { get; private set; }

        public abstract string Name { get; }
    }
}
