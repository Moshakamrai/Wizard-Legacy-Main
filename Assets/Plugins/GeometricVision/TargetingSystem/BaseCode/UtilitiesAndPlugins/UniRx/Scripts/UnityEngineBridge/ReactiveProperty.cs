#if CSHARP_7_OR_LATER || (UNITY_2018_3_OR_NEWER && (NET_STANDARD_2_0 || NET_4_6))
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#endif

#if !UniRxLibrary
#endif
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;
using UnityEngine;
#if CSHARP_7_OR_LATER || (UNITY_2018_3_OR_NEWER && (NET_STANDARD_2_0 || NET_4_6))

#endif

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge
{
    public interface IReadOnlyReactiveProperty<T> : IObservable<T>
    {
        T Value { get; }
        bool HasValue { get; }
    }

    public interface IReactiveProperty<T> : IReadOnlyReactiveProperty<T>
    {
        new T Value { get; set; }
    }

    internal interface IObserverLinkedList<T>
    {
        void UnsubscribeNode(ObserverNode<T> node);
    }

    internal sealed class ObserverNode<T> : IObserver<T>, IDisposable
    {
        readonly IObserver<T> observer;
        IObserverLinkedList<T> list;

        public ObserverNode<T> Previous { get; internal set; }
        public ObserverNode<T> Next { get; internal set; }

        public ObserverNode(IObserverLinkedList<T> list, IObserver<T> observer)
        {
            this.list = list;
            this.observer = observer;
        }

        public void OnNext(T value)
        {
            this.observer.OnNext(value);
        }

        public void OnError(Exception error)
        {
            this.observer.OnError(error);
        }

        public void OnCompleted()
        {
            this.observer.OnCompleted();
        }

        public void Dispose()
        {
            var sourceList = Interlocked.Exchange(ref this.list, null);
            if (sourceList != null)
            {
                sourceList.UnsubscribeNode(this);
                sourceList = null;
            }
        }
    }

    /// <summary>
    /// Lightweight property broker.
    /// </summary>
    [Serializable]
    public class ReactiveProperty<T> : IReactiveProperty<T>, IDisposable, IOptimizedObservable<T>, IObserverLinkedList<T>
    {
#if !UniRxLibrary
        static readonly IEqualityComparer<T> defaultEqualityComparer = UnityEqualityComparer.GetDefault<T>();
#else
        static readonly IEqualityComparer<T> defaultEqualityComparer = EqualityComparer<T>.Default;
#endif

#if !UniRxLibrary
        [SerializeField]
#endif
        T value = default(T);

        [NonSerialized]
        ObserverNode<T> root;

        [NonSerialized]
        ObserverNode<T> last;

        [NonSerialized]
        bool isDisposed = false;

        protected virtual IEqualityComparer<T> EqualityComparer
        {
            get
            {
                return defaultEqualityComparer;
            }
        }

        public T Value
        {
            get
            {
                return this.value;
            }
            set
            {
                if (!this.EqualityComparer.Equals(this.value, value))
                {
                    this.SetValue(value);
                    if (this.isDisposed)
                        return;

                    this.RaiseOnNext(ref value);
                }
            }
        }

        // always true, allows empty constructor 'can' publish value on subscribe.
        // because sometimes value is deserialized from UnityEngine.
        public bool HasValue
        {
            get
            {
                return true;
            }
        }

        public ReactiveProperty()
            : this(default(T))
        {
        }

        public ReactiveProperty(T initialValue)
        {
            this.SetValue(initialValue);
        }

        void RaiseOnNext(ref T value)
        {
            var node = this.root;
            while (node != null)
            {
                node.OnNext(value);
                node = node.Next;
            }
        }

        protected virtual void SetValue(T value)
        {
            this.value = value;
        }

        public void SetValueAndForceNotify(T value)
        {
            this.SetValue(value);
            if (this.isDisposed)
                return;

            this.RaiseOnNext(ref value);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (this.isDisposed)
            {
                observer.OnCompleted();
                return Disposable.Empty;
            }

            // raise latest value on subscribe
            observer.OnNext(this.value);

            // subscribe node, node as subscription.
            var next = new ObserverNode<T>(this, observer);
            if (this.root == null)
            {
                this.root = this.last = next;
            }
            else
            {
                this.last.Next = next;
                next.Previous = this.last;
                this.last = next;
            }
            return next;
        }

        void IObserverLinkedList<T>.UnsubscribeNode(ObserverNode<T> node)
        {
            if (node == this.root)
            {
                this.root = node.Next;
            }
            if (node == this.last)
            {
                this.last = node.Previous;
            }

            if (node.Previous != null)
            {
                node.Previous.Next = node.Next;
            }
            if (node.Next != null)
            {
                node.Next.Previous = node.Previous;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed) return;

            var node = this.root;
            this.root = this.last = null;
            this.isDisposed = true;

            while (node != null)
            {
                node.OnCompleted();
                node = node.Next;
            }
        }

        public override string ToString()
        {
            return (this.value == null) ? "(null)" : this.value.ToString();
        }

        public bool IsRequiredSubscribeOnCurrentThread()
        {
            return false;
        }
    }

    /// <summary>
    /// Lightweight property broker.
    /// </summary>
    public class ReadOnlyReactiveProperty<T> : IReadOnlyReactiveProperty<T>, IDisposable, IOptimizedObservable<T>, IObserverLinkedList<T>, IObserver<T>
    {
#if !UniRxLibrary
        static readonly IEqualityComparer<T> defaultEqualityComparer = UnityEqualityComparer.GetDefault<T>();
#else
        static readonly IEqualityComparer<T> defaultEqualityComparer = EqualityComparer<T>.Default;
#endif

        readonly bool distinctUntilChanged = true;
        bool canPublishValueOnSubscribe = false;
        bool isDisposed = false;
        bool isSourceCompleted = false;

        T latestValue = default(T);
        Exception lastException = null;
        IDisposable sourceConnection = null;

        ObserverNode<T> root;
        ObserverNode<T> last;

        public T Value
        {
            get
            {
                return this.latestValue;
            }
        }

        public bool HasValue
        {
            get
            {
                return this.canPublishValueOnSubscribe;
            }
        }

        protected virtual IEqualityComparer<T> EqualityComparer
        {
            get
            {
                return defaultEqualityComparer;
            }
        }

        public ReadOnlyReactiveProperty(IObservable<T> source)
        {
            this.sourceConnection = source.Subscribe(this);
        }

        public ReadOnlyReactiveProperty(IObservable<T> source, bool distinctUntilChanged)
        {
            this.distinctUntilChanged = distinctUntilChanged;
            this.sourceConnection = source.Subscribe(this);
        }

        public ReadOnlyReactiveProperty(IObservable<T> source, T initialValue)
        {
            this.latestValue = initialValue;
            this.canPublishValueOnSubscribe = true;
            this.sourceConnection = source.Subscribe(this);
        }

        public ReadOnlyReactiveProperty(IObservable<T> source, T initialValue, bool distinctUntilChanged)
        {
            this.distinctUntilChanged = distinctUntilChanged;
            this.latestValue = initialValue;
            this.canPublishValueOnSubscribe = true;
            this.sourceConnection = source.Subscribe(this);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (this.lastException != null)
            {
                observer.OnError(this.lastException);
                return Disposable.Empty;
            }

            if (this.isSourceCompleted)
            {
                if (this.canPublishValueOnSubscribe)
                {
                    observer.OnNext(this.latestValue);
                    observer.OnCompleted();
                    return Disposable.Empty;
                }
                else
                {
                    observer.OnCompleted();
                    return Disposable.Empty;
                }
            }

            if (this.isDisposed)
            {
                observer.OnCompleted();
                return Disposable.Empty;
            }

            if (this.canPublishValueOnSubscribe)
            {
                observer.OnNext(this.latestValue);
            }

            // subscribe node, node as subscription.
            var next = new ObserverNode<T>(this, observer);
            if (this.root == null)
            {
                this.root = this.last = next;
            }
            else
            {
                this.last.Next = next;
                next.Previous = this.last;
                this.last = next;
            }

            return next;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed) return;
            this.sourceConnection.Dispose();

            var node = this.root;
            this.root = this.last = null;
            this.isDisposed = true;

            while (node != null)
            {
                node.OnCompleted();
                node = node.Next;
            }
        }

        void IObserverLinkedList<T>.UnsubscribeNode(ObserverNode<T> node)
        {
            if (node == this.root)
            {
                this.root = node.Next;
            }
            if (node == this.last)
            {
                this.last = node.Previous;
            }

            if (node.Previous != null)
            {
                node.Previous.Next = node.Next;
            }
            if (node.Next != null)
            {
                node.Next.Previous = node.Previous;
            }
        }

        void IObserver<T>.OnNext(T value)
        {
            if (this.isDisposed) return;

            if (this.canPublishValueOnSubscribe)
            {
                if (this.distinctUntilChanged && this.EqualityComparer.Equals(this.latestValue, value))
                {
                    return;
                }
            }

            this.canPublishValueOnSubscribe = true;

            // SetValue
            this.latestValue = value;

            // call source.OnNext
            var node = this.root;
            while (node != null)
            {
                node.OnNext(value);
                node = node.Next;
            }
        }

        void IObserver<T>.OnError(Exception error)
        {
            this.lastException = error;

            // call source.OnError
            var node = this.root;
            while (node != null)
            {
                node.OnError(error);
                node = node.Next;
            }

            this.root = this.last = null;
        }

        void IObserver<T>.OnCompleted()
        {
            this.isSourceCompleted = true;
            this.root = this.last = null;
        }

        public override string ToString()
        {
            return (this.latestValue == null) ? "(null)" : this.latestValue.ToString();
        }

        public bool IsRequiredSubscribeOnCurrentThread()
        {
            return false;
        }
    }

    /// <summary>
    /// Extension methods of ReactiveProperty&lt;T&gt;
    /// </summary>
    public static class ReactivePropertyExtensions
    {
        public static IReadOnlyReactiveProperty<T> ToReactiveProperty<T>(this IObservable<T> source)
        {
            return new ReadOnlyReactiveProperty<T>(source);
        }

        public static IReadOnlyReactiveProperty<T> ToReactiveProperty<T>(this IObservable<T> source, T initialValue)
        {
            return new ReadOnlyReactiveProperty<T>(source, initialValue);
        }

        public static ReadOnlyReactiveProperty<T> ToReadOnlyReactiveProperty<T>(this IObservable<T> source)
        {
            return new ReadOnlyReactiveProperty<T>(source);
        }

#if CSHARP_7_OR_LATER || (UNITY_2018_3_OR_NEWER && (NET_STANDARD_2_0 || NET_4_6))

        static readonly Action<object> Callback = CancelCallback;

        static void CancelCallback(object state)
        {
            var tuple = (Tuple<ICancellableTaskCompletionSource, IDisposable>)state;
            tuple.Item2.Dispose();
            tuple.Item1.TrySetCanceled();
        }

        public static Task<T> WaitUntilValueChangedAsync<T>(this IReadOnlyReactiveProperty<T> source, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new CancellableTaskCompletionSource<T>();

            var disposable = new SingleAssignmentDisposable();
            if (source.HasValue)
            {
                // Skip first value
                var isFirstValue = true;
                disposable.Disposable = source.Subscribe(x =>
                {
                    if (isFirstValue)
                    {
                        isFirstValue = false;
                        return;
                    }
                    else
                    {
                        disposable.Dispose(); // finish subscription.
                        tcs.TrySetResult(x);
                    }
                }, ex => tcs.TrySetException(ex), () => tcs.TrySetCanceled());
            }
            else
            {
                disposable.Disposable = source.Subscribe(x =>
                {
                    disposable.Dispose(); // finish subscription.
                    tcs.TrySetResult(x);
                }, ex => tcs.TrySetException(ex), () => tcs.TrySetCanceled());
            }

            cancellationToken.Register(Callback, Tuple.Create(tcs, disposable.Disposable), false);

            return tcs.Task;
        }

        public static global::System.Runtime.CompilerServices.TaskAwaiter<T> GetAwaiter<T>(this IReadOnlyReactiveProperty<T> source)
        {
            return source.WaitUntilValueChangedAsync(CancellationToken.None).GetAwaiter();
        }

#endif

        /// <summary>
        /// Create ReadOnlyReactiveProperty with distinctUntilChanged: false.
        /// </summary>
        public static ReadOnlyReactiveProperty<T> ToSequentialReadOnlyReactiveProperty<T>(this IObservable<T> source)
        {
            return new ReadOnlyReactiveProperty<T>(source, distinctUntilChanged: false);
        }

        public static ReadOnlyReactiveProperty<T> ToReadOnlyReactiveProperty<T>(this IObservable<T> source, T initialValue)
        {
            return new ReadOnlyReactiveProperty<T>(source, initialValue);
        }

        /// <summary>
        /// Create ReadOnlyReactiveProperty with distinctUntilChanged: false.
        /// </summary>
        public static ReadOnlyReactiveProperty<T> ToSequentialReadOnlyReactiveProperty<T>(this IObservable<T> source, T initialValue)
        {
            return new ReadOnlyReactiveProperty<T>(source, initialValue, distinctUntilChanged: false);
        }

        public static IObservable<T> SkipLatestValueOnSubscribe<T>(this IReadOnlyReactiveProperty<T> source)
        {
            return source.HasValue ? source.Skip(1) : source;
        }

        // for multiple toggle or etc..

        /// <summary>
        /// Lastest values of each sequence are all true.
        /// </summary>
        public static IObservable<bool> CombineLatestValuesAreAllTrue(this IEnumerable<IObservable<bool>> sources)
        {
            return sources.CombineLatest().Select(xs =>
            {
                foreach (var item in xs)
                {
                    if (item == false)
                        return false;
                }
                return true;
            });
        }


        /// <summary>
        /// Lastest values of each sequence are all false.
        /// </summary>
        public static IObservable<bool> CombineLatestValuesAreAllFalse(this IEnumerable<IObservable<bool>> sources)
        {
            return sources.CombineLatest().Select(xs =>
            {
                foreach (var item in xs)
                {
                    if (item == true)
                        return false;
                }
                return true;
            });
        }
    }
}