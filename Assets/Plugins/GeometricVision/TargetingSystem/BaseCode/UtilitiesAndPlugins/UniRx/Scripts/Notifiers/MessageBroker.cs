using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Notifiers
{
    public interface IMessagePublisher
    {
        /// <summary>
        /// Send Message to all receiver.
        /// </summary>
        void Publish<T>(T message);
    }

    public interface IMessageReceiver
    {
        /// <summary>
        /// Subscribe typed message.
        /// </summary>
        IObservable<T> Receive<T>();
    }

    public interface IMessageBroker : IMessagePublisher, IMessageReceiver
    {
    }

    public interface IAsyncMessagePublisher
    {
        /// <summary>
        /// Send Message to all receiver and await complete.
        /// </summary>
        IObservable<Unit> PublishAsync<T>(T message);
    }

    public interface IAsyncMessageReceiver
    {
        /// <summary>
        /// Subscribe typed message.
        /// </summary>
        IDisposable Subscribe<T>(Func<T, IObservable<Unit>> asyncMessageReceiver);
    }

    public interface IAsyncMessageBroker : IAsyncMessagePublisher, IAsyncMessageReceiver
    {
    }

    /// <summary>
    /// In-Memory PubSub filtered by Type.
    /// </summary>
    public class MessageBroker : IMessageBroker, IDisposable
    {
        /// <summary>
        /// MessageBroker in Global scope.
        /// </summary>
        public static readonly IMessageBroker Default = new MessageBroker();

        bool isDisposed = false;
        readonly Dictionary<Type, object> notifiers = new Dictionary<Type, object>();

        public void Publish<T>(T message)
        {
            object notifier;
            lock (this.notifiers)
            {
                if (this.isDisposed) return;

                if (!this.notifiers.TryGetValue(typeof(T), out notifier))
                {
                    return;
                }
            }
            ((ISubject<T>)notifier).OnNext(message);
        }

        public IObservable<T> Receive<T>()
        {
            object notifier;
            lock (this.notifiers)
            {
                if (this.isDisposed) throw new ObjectDisposedException("MessageBroker");

                if (!this.notifiers.TryGetValue(typeof(T), out notifier))
                {
                    ISubject<T> n = SubjectExtensions.Synchronize(new Subject<T>());
                    notifier = n;
                    this.notifiers.Add(typeof(T), notifier);
                }
            }

            return ((IObservable<T>)notifier).AsObservable();
        }

        public void Dispose()
        {
            lock (this.notifiers)
            {
                if (!this.isDisposed)
                {
                    this.isDisposed = true;
                    this.notifiers.Clear();
                }
            }
        }
    }

    /// <summary>
    /// In-Memory PubSub filtered by Type.
    /// </summary>
    public class AsyncMessageBroker : IAsyncMessageBroker, IDisposable
    {
        /// <summary>
        /// AsyncMessageBroker in Global scope.
        /// </summary>
        public static readonly IAsyncMessageBroker Default = new AsyncMessageBroker();

        bool isDisposed = false;
        readonly Dictionary<Type, object> notifiers = new Dictionary<Type, object>();

        public IObservable<Unit> PublishAsync<T>(T message)
        {
            ImmutableList<Func<T, IObservable<Unit>>> notifier;
            lock (this.notifiers)
            {
                if (this.isDisposed) throw new ObjectDisposedException("AsyncMessageBroker");

                object _notifier;
                if (this.notifiers.TryGetValue(typeof(T), out _notifier))
                {
                    notifier = (ImmutableList<Func<T, IObservable<Unit>>>)_notifier;
                }
                else
                {
                    return Observable.ReturnUnit();
                }
            }

            var data = notifier.Data;
            var awaiter = new IObservable<Unit>[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                awaiter[i] = data[i].Invoke(message);
            }
            return Observable.WhenAll(awaiter);
        }

        public IDisposable Subscribe<T>(Func<T, IObservable<Unit>> asyncMessageReceiver)
        {
            lock (this.notifiers)
            {
                if (this.isDisposed) throw new ObjectDisposedException("AsyncMessageBroker");

                object _notifier;
                if (!this.notifiers.TryGetValue(typeof(T), out _notifier))
                {
                    var notifier = ImmutableList<Func<T, IObservable<Unit>>>.Empty;
                    notifier = notifier.Add(asyncMessageReceiver);
                    this.notifiers.Add(typeof(T), notifier);
                }
                else
                {
                    var notifier = (ImmutableList<Func<T, IObservable<Unit>>>)_notifier;
                    notifier = notifier.Add(asyncMessageReceiver);
                    this.notifiers[typeof(T)] = notifier;
                }
            }

            return new Subscription<T>(this, asyncMessageReceiver);
        }

        public void Dispose()
        {
            lock (this.notifiers)
            {
                if (!this.isDisposed)
                {
                    this.isDisposed = true;
                    this.notifiers.Clear();
                }
            }
        }

        class Subscription<T> : IDisposable
        {
            readonly AsyncMessageBroker parent;
            readonly Func<T, IObservable<Unit>> asyncMessageReceiver;

            public Subscription(AsyncMessageBroker parent, Func<T, IObservable<Unit>> asyncMessageReceiver)
            {
                this.parent = parent;
                this.asyncMessageReceiver = asyncMessageReceiver;
            }

            public void Dispose()
            {
                lock (this.parent.notifiers)
                {
                    object _notifier;
                    if (this.parent.notifiers.TryGetValue(typeof(T), out _notifier))
                    {
                        var notifier = (ImmutableList<Func<T, IObservable<Unit>>>)_notifier;
                        notifier = notifier.Remove(this.asyncMessageReceiver);

                        this.parent.notifiers[typeof(T)] = notifier;
                    }
                }
            }
        }
    }
}