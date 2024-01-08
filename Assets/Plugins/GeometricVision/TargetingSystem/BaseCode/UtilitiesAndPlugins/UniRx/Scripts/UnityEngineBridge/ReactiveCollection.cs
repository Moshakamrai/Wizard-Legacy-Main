using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge
{
    public struct CollectionAddEvent<T> : IEquatable<CollectionAddEvent<T>>
    {
        public int Index { get; private set; }
        public T Value { get; private set; }

        public CollectionAddEvent(int index, T value)
            :this()
        {
            this.Index = index;
            this.Value = value;
        }

        public override string ToString()
        {
            return string.Format("Index:{0} Value:{1}", this.Index, this.Value);
        }

        public override int GetHashCode()
        {
            return this.Index.GetHashCode() ^ EqualityComparer<T>.Default.GetHashCode(this.Value) << 2;
        }

        public bool Equals(CollectionAddEvent<T> other)
        {
            return this.Index.Equals(other.Index) && EqualityComparer<T>.Default.Equals(this.Value, other.Value);
        }
    }

    public struct CollectionRemoveEvent<T> : IEquatable<CollectionRemoveEvent<T>>
    {
        public int Index { get; private set; }
        public T Value { get; private set; }

        public CollectionRemoveEvent(int index, T value)
            : this()
        {
            this.Index = index;
            this.Value = value;
        }

        public override string ToString()
        {
            return string.Format("Index:{0} Value:{1}", this.Index, this.Value);
        }

        public override int GetHashCode()
        {
            return this.Index.GetHashCode() ^ EqualityComparer<T>.Default.GetHashCode(this.Value) << 2;
        }

        public bool Equals(CollectionRemoveEvent<T> other)
        {
            return this.Index.Equals(other.Index) && EqualityComparer<T>.Default.Equals(this.Value, other.Value);
        }
    }

    public struct CollectionMoveEvent<T> : IEquatable<CollectionMoveEvent<T>>
    {
        public int OldIndex { get; private set; }
        public int NewIndex { get; private set; }
        public T Value { get; private set; }

        public CollectionMoveEvent(int oldIndex, int newIndex, T value)
            : this()
        {
            this.OldIndex = oldIndex;
            this.NewIndex = newIndex;
            this.Value = value;
        }

        public override string ToString()
        {
            return string.Format("OldIndex:{0} NewIndex:{1} Value:{2}", this.OldIndex, this.NewIndex, this.Value);
        }

        public override int GetHashCode()
        {
            return this.OldIndex.GetHashCode() ^ this.NewIndex.GetHashCode() << 2 ^ EqualityComparer<T>.Default.GetHashCode(this.Value) >> 2;
        }

        public bool Equals(CollectionMoveEvent<T> other)
        {
            return this.OldIndex.Equals(other.OldIndex) && this.NewIndex.Equals(other.NewIndex) && EqualityComparer<T>.Default.Equals(this.Value, other.Value);
        }
    }

    public struct CollectionReplaceEvent<T> : IEquatable<CollectionReplaceEvent<T>>
    {
        public int Index { get; private set; }
        public T OldValue { get; private set; }
        public T NewValue { get; private set; }

        public CollectionReplaceEvent(int index, T oldValue, T newValue)
            : this()
        {
            this.Index = index;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        public override string ToString()
        {
            return string.Format("Index:{0} OldValue:{1} NewValue:{2}", this.Index, this.OldValue, this.NewValue);
        }

        public override int GetHashCode()
        {
            return this.Index.GetHashCode() ^ EqualityComparer<T>.Default.GetHashCode(this.OldValue) << 2 ^ EqualityComparer<T>.Default.GetHashCode(this.NewValue) >> 2;
        }

        public bool Equals(CollectionReplaceEvent<T> other)
        {
            return this.Index.Equals(other.Index)
                   && EqualityComparer<T>.Default.Equals(this.OldValue, other.OldValue)
                   && EqualityComparer<T>.Default.Equals(this.NewValue, other.NewValue);
        }
    }

    // IReadOnlyList<out T> is from .NET 4.5
    public interface IReadOnlyReactiveCollection<T> : IEnumerable<T>
    {
        int Count { get; }
        T this[int index] { get; }
        IObservable<CollectionAddEvent<T>> ObserveAdd();
        IObservable<int> ObserveCountChanged(bool notifyCurrentCount = false);
        IObservable<CollectionMoveEvent<T>> ObserveMove();
        IObservable<CollectionRemoveEvent<T>> ObserveRemove();
        IObservable<CollectionReplaceEvent<T>> ObserveReplace();
        IObservable<Unit> ObserveReset();
    }

    public interface IReactiveCollection<T> : IList<T>, IReadOnlyReactiveCollection<T>
    {
        new int Count { get; }
        new T this[int index] { get; set; }
        void Move(int oldIndex, int newIndex);
    }

    [Serializable]
    public class ReactiveCollection<T> : Collection<T>, IReactiveCollection<T>, IDisposable
    {
        [NonSerialized]
        bool isDisposed = false;

        public ReactiveCollection()
        {

        }

        public ReactiveCollection(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");

            foreach (var item in collection)
            {
                this.Add(item);
            }
        }

        public ReactiveCollection(List<T> list)
            : base(list != null ? new List<T>(list) : null)
        {
        }

        protected override void ClearItems()
        {
            var beforeCount = this.Count;
            base.ClearItems();

            if (this.collectionReset != null) this.collectionReset.OnNext(Unit.Default);
            if (beforeCount > 0)
            {
                if (this.countChanged != null) this.countChanged.OnNext(this.Count);
            }
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);

            if (this.collectionAdd != null) this.collectionAdd.OnNext(new CollectionAddEvent<T>(index, item));
            if (this.countChanged != null) this.countChanged.OnNext(this.Count);
        }

        public void Move(int oldIndex, int newIndex)
        {
            this.MoveItem(oldIndex, newIndex);
        }

        protected virtual void MoveItem(int oldIndex, int newIndex)
        {
            T item = this[oldIndex];
            base.RemoveItem(oldIndex);
            base.InsertItem(newIndex, item);

            if (this.collectionMove != null) this.collectionMove.OnNext(new CollectionMoveEvent<T>(oldIndex, newIndex, item));
        }

        protected override void RemoveItem(int index)
        {
            T item = this[index];
            base.RemoveItem(index);

            if (this.collectionRemove != null) this.collectionRemove.OnNext(new CollectionRemoveEvent<T>(index, item));
            if (this.countChanged != null) this.countChanged.OnNext(this.Count);
        }

        protected override void SetItem(int index, T item)
        {
            T oldItem = this[index];
            base.SetItem(index, item);

            if (this.collectionReplace != null) this.collectionReplace.OnNext(new CollectionReplaceEvent<T>(index, oldItem, item));
        }


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
        Subject<CollectionAddEvent<T>> collectionAdd = null;
        public IObservable<CollectionAddEvent<T>> ObserveAdd()
        {
            if (this.isDisposed) return Scripts.Observable.Empty<CollectionAddEvent<T>>();
            return this.collectionAdd ?? (this.collectionAdd = new Subject<CollectionAddEvent<T>>());
        }

        [NonSerialized]
        Subject<CollectionMoveEvent<T>> collectionMove = null;
        public IObservable<CollectionMoveEvent<T>> ObserveMove()
        {
            if (this.isDisposed) return Scripts.Observable.Empty<CollectionMoveEvent<T>>();
            return this.collectionMove ?? (this.collectionMove = new Subject<CollectionMoveEvent<T>>());
        }

        [NonSerialized]
        Subject<CollectionRemoveEvent<T>> collectionRemove = null;
        public IObservable<CollectionRemoveEvent<T>> ObserveRemove()
        {
            if (this.isDisposed) return Scripts.Observable.Empty<CollectionRemoveEvent<T>>();
            return this.collectionRemove ?? (this.collectionRemove = new Subject<CollectionRemoveEvent<T>>());
        }

        [NonSerialized]
        Subject<CollectionReplaceEvent<T>> collectionReplace = null;
        public IObservable<CollectionReplaceEvent<T>> ObserveReplace()
        {
            if (this.isDisposed) return Scripts.Observable.Empty<CollectionReplaceEvent<T>>();
            return this.collectionReplace ?? (this.collectionReplace = new Subject<CollectionReplaceEvent<T>>());
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
                    this.DisposeSubject(ref this.collectionReset);
                    this.DisposeSubject(ref this.collectionAdd);
                    this.DisposeSubject(ref this.collectionMove);
                    this.DisposeSubject(ref this.collectionRemove);
                    this.DisposeSubject(ref this.collectionReplace);
                    this.DisposeSubject(ref this.countChanged);
                }

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }
        
        #endregion
    }

    public static partial class ReactiveCollectionExtensions
    {
        public static ReactiveCollection<T> ToReactiveCollection<T>(this IEnumerable<T> source)
        {
            return new ReactiveCollection<T>(source);
        }
    }
}
