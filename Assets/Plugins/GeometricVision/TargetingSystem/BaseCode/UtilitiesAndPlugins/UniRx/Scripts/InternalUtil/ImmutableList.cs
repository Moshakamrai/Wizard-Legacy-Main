using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil
{
    // ImmutableList is sometimes useful, use for public.
    public class ImmutableList<T>
    {
        public static readonly ImmutableList<T> Empty = new ImmutableList<T>();

        T[] data;

        public T[] Data
        {
            get { return this.data; }
        }

        ImmutableList()
        {
            this.data = new T[0];
        }

        public ImmutableList(T[] data)
        {
            this.data = data;
        }

        public ImmutableList<T> Add(T value)
        {
            var newData = new T[this.data.Length + 1];
            Array.Copy(this.data, newData, this.data.Length);
            newData[this.data.Length] = value;
            return new ImmutableList<T>(newData);
        }

        public ImmutableList<T> Remove(T value)
        {
            var i = this.IndexOf(value);
            if (i < 0) return this;

            var length = this.data.Length;
            if (length == 1) return Empty;

            var newData = new T[length - 1];

            Array.Copy(this.data, 0, newData, 0, i);
            Array.Copy(this.data, i + 1, newData, i, length - i - 1);

            return new ImmutableList<T>(newData);
        }

        public int IndexOf(T value)
        {
            for (var i = 0; i < this.data.Length; ++i)
            {
                // ImmutableList only use for IObserver(no worry for boxed)
                if (Equals(this.data[i], value)) return i;
            }
            return -1;
        }
    }
}