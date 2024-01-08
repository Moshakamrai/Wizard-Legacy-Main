using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge
{
    public struct DictionaryAddEvent<TKey, TValue> : IEquatable<DictionaryAddEvent<TKey, TValue>>
    {
        public TKey Key { get; private set; }
        public TValue Value { get; private set; }

        public DictionaryAddEvent(TKey key, TValue value)
            : this()
        {
            this.Key = key;
            this.Value = value;
        }

        public override string ToString()
        {
            return string.Format("Key:{0} Value:{1}", this.Key, this.Value);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<TKey>.Default.GetHashCode(this.Key) ^ EqualityComparer<TValue>.Default.GetHashCode(this.Value) << 2;
        }

        public bool Equals(DictionaryAddEvent<TKey, TValue> other)
        {
            return EqualityComparer<TKey>.Default.Equals(this.Key, other.Key) && EqualityComparer<TValue>.Default.Equals(this.Value, other.Value);
        }
    }

    public struct DictionaryRemoveEvent<TKey, TValue> : IEquatable<DictionaryRemoveEvent<TKey, TValue>>
    {
        public TKey Key { get; private set; }
        public TValue Value { get; private set; }

        public DictionaryRemoveEvent(TKey key, TValue value)
            : this()
        {
            this.Key = key;
            this.Value = value;
        }

        public override string ToString()
        {
            return string.Format("Key:{0} Value:{1}", this.Key, this.Value);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<TKey>.Default.GetHashCode(this.Key) ^ EqualityComparer<TValue>.Default.GetHashCode(this.Value) << 2;
        }

        public bool Equals(DictionaryRemoveEvent<TKey, TValue> other)
        {
            return EqualityComparer<TKey>.Default.Equals(this.Key, other.Key) && EqualityComparer<TValue>.Default.Equals(this.Value, other.Value);
        }
    }

    public struct DictionaryReplaceEvent<TKey, TValue> : IEquatable<DictionaryReplaceEvent<TKey, TValue>>
    {
        public TKey Key { get; private set; }
        public TValue OldValue { get; private set; }
        public TValue NewValue { get; private set; }

        public DictionaryReplaceEvent(TKey key, TValue oldValue, TValue newValue)
            : this()
        {
            this.Key = key;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        public override string ToString()
        {
            return string.Format("Key:{0} OldValue:{1} NewValue:{2}", this.Key, this.OldValue, this.NewValue);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<TKey>.Default.GetHashCode(this.Key) ^ EqualityComparer<TValue>.Default.GetHashCode(this.OldValue) << 2 ^ EqualityComparer<TValue>.Default.GetHashCode(this.NewValue) >> 2;
        }

        public bool Equals(DictionaryReplaceEvent<TKey, TValue> other)
        {
            return EqualityComparer<TKey>.Default.Equals(this.Key, other.Key) && EqualityComparer<TValue>.Default.Equals(this.OldValue, other.OldValue) && EqualityComparer<TValue>.Default.Equals(this.NewValue, other.NewValue);
        }
    }

    // IReadOnlyDictionary is from .NET 4.5
    public interface IReadOnlyReactiveDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        int Count { get; }
        TValue this[TKey index] { get; }
        bool ContainsKey(TKey key);
        bool TryGetValue(TKey key, out TValue value);

        IObservable<DictionaryAddEvent<TKey, TValue>> ObserveAdd();
        IObservable<int> ObserveCountChanged(bool notifyCurrentCount = false);
        IObservable<DictionaryRemoveEvent<TKey, TValue>> ObserveRemove();
        IObservable<DictionaryReplaceEvent<TKey, TValue>> ObserveReplace();
        IObservable<Unit> ObserveReset();
    }

    public interface IReactiveDictionary<TKey, TValue> : IReadOnlyReactiveDictionary<TKey, TValue>, IDictionary<TKey, TValue>
    {
    }

    [Serializable]
    public class ReactiveDictionary<TKey, TValue> : IReactiveDictionary<TKey, TValue>, IDictionary<TKey, TValue>, IEnumerable, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IDictionary, IDisposable
#if !UNITY_METRO
        , ISerializable, IDeserializationCallback
#endif
    {
        [NonSerialized]
        bool isDisposed = false;

#if !UniRxLibrary
        [UnityEngine.SerializeField]
#endif
        readonly Dictionary<TKey, TValue> inner;

        public ReactiveDictionary()
        {
            this.inner = new Dictionary<TKey, TValue>();
        }

        public ReactiveDictionary(IEqualityComparer<TKey> comparer)
        {
            this.inner = new Dictionary<TKey, TValue>(comparer);
        }

        public ReactiveDictionary(Dictionary<TKey, TValue> innerDictionary)
        {
            this.inner = innerDictionary;
        }

        public TValue this[TKey key]
        {
            get
            {
                return this.inner[key];
            }

            set
            {
                TValue oldValue;
                if (this.TryGetValue(key, out oldValue))
                {
                    this.inner[key] = value;
                    if (this.dictionaryReplace != null) this.dictionaryReplace.OnNext(new DictionaryReplaceEvent<TKey, TValue>(key, oldValue, value));
                }
                else
                {
                    this.inner[key] = value;
                    if (this.dictionaryAdd != null) this.dictionaryAdd.OnNext(new DictionaryAddEvent<TKey, TValue>(key, value));
                    if (this.countChanged != null) this.countChanged.OnNext(this.Count);
                }
            }
        }

        public int Count
        {
            get
            {
                return this.inner.Count;
            }
        }

        public Dictionary<TKey, TValue>.KeyCollection Keys
        {
            get
            {
                return this.inner.Keys;
            }
        }

        public Dictionary<TKey, TValue>.ValueCollection Values
        {
            get
            {
                return this.inner.Values;
            }
        }

        public void Add(TKey key, TValue value)
        {
            this.inner.Add(key, value);

            if (this.dictionaryAdd != null) this.dictionaryAdd.OnNext(new DictionaryAddEvent<TKey, TValue>(key, value));
            if (this.countChanged != null) this.countChanged.OnNext(this.Count);
        }

        public void Clear()
        {
            var beforeCount = this.Count;
            this.inner.Clear();

            if (this.collectionReset != null) this.collectionReset.OnNext(Unit.Default);
            if (beforeCount > 0)
            {
                if (this.countChanged != null) this.countChanged.OnNext(this.Count);
            }
        }

        public bool Remove(TKey key)
        {
            TValue oldValue;
            if (this.inner.TryGetValue(key, out oldValue))
            {
                var isSuccessRemove = this.inner.Remove(key);
                if (isSuccessRemove)
                {
                    if (this.dictionaryRemove != null) this.dictionaryRemove.OnNext(new DictionaryRemoveEvent<TKey, TValue>(key, oldValue));
                    if (this.countChanged != null) this.countChanged.OnNext(this.Count);
                }
                return isSuccessRemove;
            }
            else
            {
                return false;
            }
        }

        public bool ContainsKey(TKey key)
        {
            return this.inner.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return this.inner.TryGetValue(key, out value);
        }

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            return this.inner.GetEnumerator();
        }

        void DisposeSubject<TSubject>(ref Subject<TSubject> subject)
        {
            if (subject != null)
            {
                try
                {
                    subject.OnCompleted();
                }
                finally
                {
                    subject.Dispose();
                    subject = null;
                }
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.DisposeSubject(ref this.countChanged);
                    this.DisposeSubject(ref this.collectionReset);
                    this.DisposeSubject(ref this.dictionaryAdd);
                    this.DisposeSubject(ref this.dictionaryRemove);
                    this.DisposeSubject(ref this.dictionaryReplace);
                }

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        #endregion


        #region Observe

        [NonSerialized]
        Subject<int> countChanged = null;
        public IObservable<int> ObserveCountChanged(bool notifyCurrentCount = false)
        {
            if (this.isDisposed) return Scripts.Observable.Empty<int>();

            var subject = this.countChanged ?? (this.countChanged = new Subject<int>());
            if (notifyCurrentCount)
            {
                return subject.StartWith(() => this.Count);
            }
            else
            {
                return subject;
            }
        }

        [NonSerialized]
        Subject<Unit> collectionReset = null;
        public IObservable<Unit> ObserveReset()
        {
            if (this.isDisposed) return Scripts.Observable.Empty<Unit>();
            return this.collectionReset ?? (this.collectionReset = new Subject<Unit>());
        }

        [NonSerialized]
        Subject<DictionaryAddEvent<TKey, TValue>> dictionaryAdd = null;
        public IObservable<DictionaryAddEvent<TKey, TValue>> ObserveAdd()
        {
            if (this.isDisposed) return Scripts.Observable.Empty<DictionaryAddEvent<TKey, TValue>>();
            return this.dictionaryAdd ?? (this.dictionaryAdd = new Subject<DictionaryAddEvent<TKey, TValue>>());
        }

        [NonSerialized]
        Subject<DictionaryRemoveEvent<TKey, TValue>> dictionaryRemove = null;
        public IObservable<DictionaryRemoveEvent<TKey, TValue>> ObserveRemove()
        {
            if (this.isDisposed) return Scripts.Observable.Empty<DictionaryRemoveEvent<TKey, TValue>>();
            return this.dictionaryRemove ?? (this.dictionaryRemove = new Subject<DictionaryRemoveEvent<TKey, TValue>>());
        }

        [NonSerialized]
        Subject<DictionaryReplaceEvent<TKey, TValue>> dictionaryReplace = null;
        public IObservable<DictionaryReplaceEvent<TKey, TValue>> ObserveReplace()
        {
            if (this.isDisposed) return Scripts.Observable.Empty<DictionaryReplaceEvent<TKey, TValue>>();
            return this.dictionaryReplace ?? (this.dictionaryReplace = new Subject<DictionaryReplaceEvent<TKey, TValue>>());
        }

        #endregion

        #region implement explicit

        object IDictionary.this[object key]
        {
            get
            {
                return this[(TKey)key];
            }

            set
            {
                this[(TKey)key] = (TValue)value;
            }
        }


        bool IDictionary.IsFixedSize
        {
            get
            {
                return ((IDictionary) this.inner).IsFixedSize;
            }
        }

        bool IDictionary.IsReadOnly
        {
            get
            {
                return ((IDictionary) this.inner).IsReadOnly;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return ((IDictionary) this.inner).IsSynchronized;
            }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                return ((IDictionary) this.inner).Keys;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return ((IDictionary) this.inner).SyncRoot;
            }
        }

        ICollection IDictionary.Values
        {
            get
            {
                return ((IDictionary) this.inner).Values;
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
                return this.inner.Keys;
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                return this.inner.Values;
            }
        }

        void IDictionary.Add(object key, object value)
        {
            this.Add((TKey)key, (TValue)value);
        }

        bool IDictionary.Contains(object key)
        {
            return ((IDictionary) this.inner).Contains(key);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((IDictionary) this.inner).CopyTo(array, index);
        }

#if !UNITY_METRO

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ((ISerializable) this.inner).GetObjectData(info, context);
        }

        public void OnDeserialization(object sender)
        {
            ((IDeserializationCallback) this.inner).OnDeserialization(sender);
        }

#endif

        void IDictionary.Remove(object key)
        {
            this.Remove((TKey)key);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add((TKey)item.Key, (TValue)item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>) this.inner).Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>) this.inner).CopyTo(array, arrayIndex);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>) this.inner).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.inner.GetEnumerator();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            TValue v;
            if (this.TryGetValue(item.Key, out v))
            {
                if (EqualityComparer<TValue>.Default.Equals(v, item.Value))
                {
                    this.Remove(item.Key);
                    return true;
                }
            }

            return false;
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary) this.inner).GetEnumerator();
        }

        #endregion
    }

    public static partial class ReactiveDictionaryExtensions
    {
        public static ReactiveDictionary<TKey, TValue> ToReactiveDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            return new ReactiveDictionary<TKey, TValue>(dictionary);
        }
    }
}
