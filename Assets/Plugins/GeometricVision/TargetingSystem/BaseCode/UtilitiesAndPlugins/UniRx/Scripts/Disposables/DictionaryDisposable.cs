using System;
using System.Collections.Generic;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables
{
    public sealed class DictionaryDisposable<TKey, TValue> : IDisposable, IDictionary<TKey, TValue>
        where TValue : IDisposable
    {
        bool isDisposed = false;
        readonly Dictionary<TKey, TValue> inner;

        public DictionaryDisposable()
        {
            this.inner = new Dictionary<TKey, TValue>();
        }

        public DictionaryDisposable(IEqualityComparer<TKey> comparer)
        {
            this.inner = new Dictionary<TKey, TValue>(comparer);
        }

        public TValue this[TKey key]
        {
            get
            {
                lock (this.inner)
                {
                    return this.inner[key];
                }
            }

            set
            {
                lock (this.inner)
                {
                    if (this.isDisposed) value.Dispose();

                    TValue oldValue;
                    if (this.TryGetValue(key, out oldValue))
                    {
                        oldValue.Dispose();
                        this.inner[key] = value;
                    }
                    else
                    {
                        this.inner[key] = value;
                    }
                }
            }
        }

        public int Count
        {
            get
            {
                lock (this.inner)
                {
                    return this.inner.Count;
                }
            }
        }

        public Dictionary<TKey, TValue>.KeyCollection Keys
        {
            get
            {
                throw new NotSupportedException("please use .Select(x => x.Key).ToArray()");
            }
        }

        public Dictionary<TKey, TValue>.ValueCollection Values
        {
            get
            {
                throw new NotSupportedException("please use .Select(x => x.Value).ToArray()");
            }
        }

        public void Add(TKey key, TValue value)
        {
            lock (this.inner)
            {
                if (this.isDisposed)
                {
                    value.Dispose();
                    return;
                }

                this.inner.Add(key, value);
            }
        }

        public void Clear()
        {
            lock (this.inner)
            {
                foreach (var item in this.inner)
                {
                    item.Value.Dispose();
                }

                this.inner.Clear();
            }
        }

        public bool Remove(TKey key)
        {
            lock (this.inner)
            {
                TValue oldValue;
                if (this.inner.TryGetValue(key, out oldValue))
                {
                    var isSuccessRemove = this.inner.Remove(key);
                    if (isSuccessRemove)
                    {
                        oldValue.Dispose();
                    }
                    return isSuccessRemove;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (this.inner)
            {
                return this.inner.ContainsKey(key);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (this.inner)
            {
                return this.inner.TryGetValue(key, out value);
            }
        }

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            lock (this.inner)
            {
                return new Dictionary<TKey, TValue>(this.inner).GetEnumerator();
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return ((ICollection<KeyValuePair<TKey, TValue>>) this.inner).IsReadOnly;
            }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                lock (this.inner)
                {
                    return new List<TKey>(this.inner.Keys);
                }
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                lock (this.inner)
                {
                    return new List<TValue>(this.inner.Values);
                }
            }
        }


#if !UNITY_METRO

        public void GetObjectData(global::System.Runtime.Serialization.SerializationInfo info, global::System.Runtime.Serialization.StreamingContext context)
        {
            lock (this.inner)
            {
                ((global::System.Runtime.Serialization.ISerializable) this.inner).GetObjectData(info, context);
            }
        }

        public void OnDeserialization(object sender)
        {
            lock (this.inner)
            {
                ((global::System.Runtime.Serialization.IDeserializationCallback) this.inner).OnDeserialization(sender);
            }
        }

#endif

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add((TKey)item.Key, (TValue)item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            lock (this.inner)
            {
                return ((ICollection<KeyValuePair<TKey, TValue>>) this.inner).Contains(item);
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            lock (this.inner)
            {
                ((ICollection<KeyValuePair<TKey, TValue>>) this.inner).CopyTo(array, arrayIndex);
            }
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            lock (this.inner)
            {
                return new List<KeyValuePair<TKey, TValue>>((ICollection<KeyValuePair<TKey, TValue>>) this.inner).GetEnumerator();
            }
        }

        global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            lock (this.inner)
            {
                if (this.isDisposed) return;
                this.isDisposed = true;

                foreach (var item in this.inner)
                {
                    item.Value.Dispose();
                }

                this.inner.Clear();
            }
        }
    }
}
