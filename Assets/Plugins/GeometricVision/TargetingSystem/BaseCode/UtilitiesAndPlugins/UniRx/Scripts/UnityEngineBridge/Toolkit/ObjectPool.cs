#if UNITY_5_3_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Toolkit
{
    /// <summary>
    /// Bass class of ObjectPool.
    /// </summary>
    public abstract class ObjectPool<T> : IDisposable
        where T : UnityEngine.Component
    {
        bool isDisposed = false;
        Queue<T> q;

        /// <summary>
        /// Limit of instace count.
        /// </summary>
        protected int MaxPoolCount
        {
            get
            {
                return int.MaxValue;
            }
        }

        /// <summary>
        /// Create instance when needed.
        /// </summary>
        protected abstract T CreateInstance();

        /// <summary>
        /// Called before return to pool, useful for set active object(it is default behavior).
        /// </summary>
        protected virtual void OnBeforeRent(T instance)
        {
            instance.gameObject.SetActive(true);
        }

        /// <summary>
        /// Called before return to pool, useful for set inactive object(it is default behavior).
        /// </summary>
        protected virtual void OnBeforeReturn(T instance)
        {
            instance.gameObject.SetActive(false);
        }

        /// <summary>
        /// Called when clear or disposed, useful for destroy instance or other finalize method.
        /// </summary>
        protected virtual void OnClear(T instance)
        {
            if (instance == null) return;

            var go = instance.gameObject;
            if (go == null) return;
            UnityEngine.Object.Destroy(go);
        }

        /// <summary>
        /// Current pooled object count.
        /// </summary>
        public int Count
        {
            get
            {
                if (this.q == null) return 0;
                return this.q.Count;
            }
        }

        /// <summary>
        /// Get instance from pool.
        /// </summary>
        public T Rent()
        {
            if (this.isDisposed) throw new ObjectDisposedException("ObjectPool was already disposed.");
            if (this.q == null) this.q = new Queue<T>();

            var instance = (this.q.Count > 0)
                ? this.q.Dequeue()
                : this.CreateInstance();

            this.OnBeforeRent(instance);
            return instance;
        }

        /// <summary>
        /// Return instance to pool.
        /// </summary>
        public void Return(T instance)
        {
            if (this.isDisposed) throw new ObjectDisposedException("ObjectPool was already disposed.");
            if (instance == null) throw new ArgumentNullException("instance");

            if (this.q == null) this.q = new Queue<T>();

            if ((this.q.Count + 1) == this.MaxPoolCount)
            {
                throw new InvalidOperationException("Reached Max PoolSize");
            }

            this.OnBeforeReturn(instance);
            this.q.Enqueue(instance);
        }

        /// <summary>
        /// Clear pool.
        /// </summary>
        public void Clear(bool callOnBeforeRent = false)
        {
            if (this.q == null) return;
            while (this.q.Count != 0)
            {
                var instance = this.q.Dequeue();
                if (callOnBeforeRent)
                {
                    this.OnBeforeRent(instance);
                }

                this.OnClear(instance);
            }
        }

        /// <summary>
        /// Trim pool instances. 
        /// </summary>
        /// <param name="instanceCountRatio">0.0f = clear all ~ 1.0f = live all.</param>
        /// <param name="minSize">Min pool count.</param>
        /// <param name="callOnBeforeRent">If true, call OnBeforeRent before OnClear.</param>
        public void Shrink(float instanceCountRatio, int minSize, bool callOnBeforeRent = false)
        {
            if (this.q == null) return;

            if (instanceCountRatio <= 0) instanceCountRatio = 0;
            if (instanceCountRatio >= 1.0f) instanceCountRatio = 1.0f;

            var size = (int)(this.q.Count * instanceCountRatio);
            size = Math.Max(minSize, size);

            while (this.q.Count > size)
            {
                var instance = this.q.Dequeue();
                if (callOnBeforeRent)
                {
                    this.OnBeforeRent(instance);
                }

                this.OnClear(instance);
            }
        }

        /// <summary>
        /// If needs shrink pool frequently, start check timer.
        /// </summary>
        /// <param name="checkInterval">Interval of call Shrink.</param>
        /// <param name="instanceCountRatio">0.0f = clearAll ~ 1.0f = live all.</param>
        /// <param name="minSize">Min pool count.</param>
        /// <param name="callOnBeforeRent">If true, call OnBeforeRent before OnClear.</param>
        public IDisposable StartShrinkTimer(TimeSpan checkInterval, float instanceCountRatio, int minSize, bool callOnBeforeRent = false)
        {
            return Scripts.Observable.Interval(checkInterval)
                .TakeWhile(_ => !this.isDisposed)
                .Subscribe(_ =>
                {
                    this.Shrink(instanceCountRatio, minSize, callOnBeforeRent);
                });
        }

        /// <summary>
        /// Fill pool before rent operation.
        /// </summary>
        /// <param name="preloadCount">Pool instance count.</param>
        /// <param name="threshold">Create count per frame.</param>
        public IObservable<Unit> PreloadAsync(int preloadCount, int threshold)
        {
            if (this.q == null) this.q = new Queue<T>(preloadCount);

            return Observable.FromMicroCoroutine<Unit>((observer, cancel) => this.PreloadCore(preloadCount, threshold, observer, cancel));
        }

        IEnumerator PreloadCore(int preloadCount, int threshold, IObserver<Unit> observer, CancellationToken cancellationToken)
        {
            while (this.Count < preloadCount && !cancellationToken.IsCancellationRequested)
            {
                var requireCount = preloadCount - this.Count;
                if (requireCount <= 0) break;

                var createCount = Math.Min(requireCount, threshold);

                for (int i = 0; i < createCount; i++)
                {
                    try
                    {
                        var instance = this.CreateInstance();
                        this.Return(instance);
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                        yield break;
                    }
                }
                yield return null; // next frame.
            }

            observer.OnNext(Unit.Default);
            observer.OnCompleted();
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.Clear(false);
                }

                this.isDisposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        #endregion
    }

    /// <summary>
    /// Bass class of ObjectPool. If needs asynchronous initialization, use this instead of standard ObjectPool.
    /// </summary>
    public abstract class AsyncObjectPool<T> : IDisposable
        where T : UnityEngine.Component
    {
        bool isDisposed = false;
        Queue<T> q;

        /// <summary>
        /// Limit of instace count.
        /// </summary>
        protected int MaxPoolCount
        {
            get
            {
                return int.MaxValue;
            }
        }

        /// <summary>
        /// Create instance when needed.
        /// </summary>
        protected abstract IObservable<T> CreateInstanceAsync();

        /// <summary>
        /// Called before return to pool, useful for set active object(it is default behavior).
        /// </summary>
        protected virtual void OnBeforeRent(T instance)
        {
            instance.gameObject.SetActive(true);
        }

        /// <summary>
        /// Called before return to pool, useful for set inactive object(it is default behavior).
        /// </summary>
        protected virtual void OnBeforeReturn(T instance)
        {
            instance.gameObject.SetActive(false);
        }

        /// <summary>
        /// Called when clear or disposed, useful for destroy instance or other finalize method.
        /// </summary>
        protected virtual void OnClear(T instance)
        {
            if (instance == null) return;

            var go = instance.gameObject;
            if (go == null) return;
            UnityEngine.Object.Destroy(go);
        }

        /// <summary>
        /// Current pooled object count.
        /// </summary>
        public int Count
        {
            get
            {
                if (this.q == null) return 0;
                return this.q.Count;
            }
        }

        /// <summary>
        /// Get instance from pool.
        /// </summary>
        public IObservable<T> RentAsync()
        {
            if (this.isDisposed) throw new ObjectDisposedException("ObjectPool was already disposed.");
            if (this.q == null) this.q = new Queue<T>();

            if (this.q.Count > 0)
            {
                var instance = this.q.Dequeue();
                this.OnBeforeRent(instance);
                return Scripts.Observable.Return(instance);
            }
            else
            {
                var instance = this.CreateInstanceAsync();
                return instance.Do(x => this.OnBeforeRent(x));
            }
        }

        /// <summary>
        /// Return instance to pool.
        /// </summary>
        public void Return(T instance)
        {
            if (this.isDisposed) throw new ObjectDisposedException("ObjectPool was already disposed.");
            if (instance == null) throw new ArgumentNullException("instance");

            if (this.q == null) this.q = new Queue<T>();

            if ((this.q.Count + 1) == this.MaxPoolCount)
            {
                throw new InvalidOperationException("Reached Max PoolSize");
            }

            this.OnBeforeReturn(instance);
            this.q.Enqueue(instance);
        }

        /// <summary>
        /// Trim pool instances. 
        /// </summary>
        /// <param name="instanceCountRatio">0.0f = clear all ~ 1.0f = live all.</param>
        /// <param name="minSize">Min pool count.</param>
        /// <param name="callOnBeforeRent">If true, call OnBeforeRent before OnClear.</param>
        public void Shrink(float instanceCountRatio, int minSize, bool callOnBeforeRent = false)
        {
            if (this.q == null) return;

            if (instanceCountRatio <= 0) instanceCountRatio = 0;
            if (instanceCountRatio >= 1.0f) instanceCountRatio = 1.0f;

            var size = (int)(this.q.Count * instanceCountRatio);
            size = Math.Max(minSize, size);

            while (this.q.Count > size)
            {
                var instance = this.q.Dequeue();
                if (callOnBeforeRent)
                {
                    this.OnBeforeRent(instance);
                }

                this.OnClear(instance);
            }
        }

        /// <summary>
        /// If needs shrink pool frequently, start check timer.
        /// </summary>
        /// <param name="checkInterval">Interval of call Shrink.</param>
        /// <param name="instanceCountRatio">0.0f = clearAll ~ 1.0f = live all.</param>
        /// <param name="minSize">Min pool count.</param>
        /// <param name="callOnBeforeRent">If true, call OnBeforeRent before OnClear.</param>
        public IDisposable StartShrinkTimer(TimeSpan checkInterval, float instanceCountRatio, int minSize, bool callOnBeforeRent = false)
        {
            return Scripts.Observable.Interval(checkInterval)
                .TakeWhile(_ => !this.isDisposed)
                .Subscribe(_ =>
                {
                    this.Shrink(instanceCountRatio, minSize, callOnBeforeRent);
                });
        }

        /// <summary>
        /// Clear pool.
        /// </summary>
        public void Clear(bool callOnBeforeRent = false)
        {
            if (this.q == null) return;
            while (this.q.Count != 0)
            {
                var instance = this.q.Dequeue();
                if (callOnBeforeRent)
                {
                    this.OnBeforeRent(instance);
                }

                this.OnClear(instance);
            }
        }

        /// <summary>
        /// Fill pool before rent operation.
        /// </summary>
        /// <param name="preloadCount">Pool instance count.</param>
        /// <param name="threshold">Create count per frame.</param>
        public IObservable<Unit> PreloadAsync(int preloadCount, int threshold)
        {
            if (this.q == null) this.q = new Queue<T>(preloadCount);

            return Observable.FromMicroCoroutine<Unit>((observer, cancel) => this.PreloadCore(preloadCount, threshold, observer, cancel));
        }

        IEnumerator PreloadCore(int preloadCount, int threshold, IObserver<Unit> observer, CancellationToken cancellationToken)
        {
            while (this.Count < preloadCount && !cancellationToken.IsCancellationRequested)
            {
                var requireCount = preloadCount - this.Count;
                if (requireCount <= 0) break;

                var createCount = Math.Min(requireCount, threshold);

                var loaders = new IObservable<Unit>[createCount];
                for (int i = 0; i < createCount; i++)
                {
                    var instanceFuture = this.CreateInstanceAsync();
                    loaders[i] = instanceFuture.ForEachAsync(x => this.Return(x));
                }

                var awaiter = Scripts.Observable.WhenAll(loaders).ToYieldInstruction(false, cancellationToken);
                while (!(awaiter.HasResult || awaiter.IsCanceled || awaiter.HasError))
                {
                    yield return null;
                }

                if (awaiter.HasError)
                {
                    observer.OnError(awaiter.Error);
                    yield break;
                }
                else if (awaiter.IsCanceled)
                {
                    yield break; // end.
                }
            }

            observer.OnNext(Unit.Default);
            observer.OnCompleted();
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.Clear(false);
                }

                this.isDisposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        #endregion
    }
}

#endif