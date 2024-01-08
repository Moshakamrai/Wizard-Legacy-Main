using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects
{
    public interface IConnectableObservable<T> : IObservable<T>
    {
        IDisposable Connect();
    }

    public static partial class Observable
    {
        internal class ConnectableObservable<T> : IConnectableObservable<T>
        {
            readonly IObservable<T> source;
            readonly ISubject<T> subject;
            readonly object gate = new object();
            Connection connection;

            public ConnectableObservable(IObservable<T> source, ISubject<T> subject)
            {
                this.source = source.AsObservable();
                this.subject = subject;
            }

            public IDisposable Connect()
            {
                lock (this.gate)
                {
                    // don't subscribe twice
                    if (this.connection == null)
                    {
                        var subscription = this.source.Subscribe(this.subject);
                        this.connection = new Connection(this, subscription);
                    }

                    return this.connection;
                }
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return this.subject.Subscribe(observer);
            }

            class Connection : IDisposable
            {
                readonly ConnectableObservable<T> parent;
                IDisposable subscription;

                public Connection(ConnectableObservable<T> parent, IDisposable subscription)
                {
                    this.parent = parent;
                    this.subscription = subscription;
                }

                public void Dispose()
                {
                    lock (this.parent.gate)
                    {
                        if (this.subscription != null)
                        {
                            this.subscription.Dispose();
                            this.subscription = null;
                            this.parent.connection = null;
                        }
                    }
                }
            }
        }
    }
}