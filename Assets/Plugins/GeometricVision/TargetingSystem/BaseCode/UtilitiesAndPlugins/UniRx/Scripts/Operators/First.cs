using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class FirstObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly bool useDefault;
        readonly Func<T, bool> predicate;

        public FirstObservable(IObservable<T> source, bool useDefault)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.useDefault = useDefault;
        }

        public FirstObservable(IObservable<T> source, Func<T, bool> predicate, bool useDefault)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.predicate = predicate;
            this.useDefault = useDefault;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            if (this.predicate == null)
            {
                return this.source.Subscribe(new First(this, observer, cancel));
            }
            else
            {
                return this.source.Subscribe(new First_(this, observer, cancel));
            }
        }

        class First : OperatorObserverBase<T, T>
        {
            readonly FirstObservable<T> parent;
            bool notPublished;

            public First(FirstObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.notPublished = true;
            }

            public override void OnNext(T value)
            {
                if (this.notPublished)
                {
                    this.notPublished = false;
                    this.observer.OnNext(value);
                    try {
                        this.observer.OnCompleted(); }
                    finally {
                        this.Dispose(); }
                    return;
                }
            }

            public override void OnError(Exception error)
            {
                try {
                    this.observer.OnError(error); }
                finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {
                if (this.parent.useDefault)
                {
                    if (this.notPublished)
                    {
                        this.observer.OnNext(default(T));
                    }
                    try {
                        this.observer.OnCompleted(); }
                    finally {
                        this.Dispose(); }
                }
                else
                {
                    if (this.notPublished)
                    {
                        try {
                            this.observer.OnError(new InvalidOperationException("sequence is empty")); }
                        finally {
                            this.Dispose(); }
                    }
                    else
                    {
                        try {
                            this.observer.OnCompleted(); }
                        finally {
                            this.Dispose(); }
                    }
                }
            }
        }

        // with predicate
        class First_ : OperatorObserverBase<T, T>
        {
            readonly FirstObservable<T> parent;
            bool notPublished;

            public First_(FirstObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.notPublished = true;
            }

            public override void OnNext(T value)
            {
                if (this.notPublished)
                {
                    bool isPassed;
                    try
                    {
                        isPassed = this.parent.predicate(value);
                    }
                    catch (Exception ex)
                    {
                        try {
                            this.observer.OnError(ex); }
                        finally {
                            this.Dispose(); }
                        return;
                    }

                    if (isPassed)
                    {
                        this.notPublished = false;
                        this.observer.OnNext(value);
                        try {
                            this.observer.OnCompleted(); }
                        finally {
                            this.Dispose(); }
                    }
                }
            }

            public override void OnError(Exception error)
            {
                try {
                    this.observer.OnError(error); }
                finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {
                if (this.parent.useDefault)
                {
                    if (this.notPublished)
                    {
                        this.observer.OnNext(default(T));
                    }
                    try {
                        this.observer.OnCompleted(); }
                    finally {
                        this.Dispose(); }
                }
                else
                {
                    if (this.notPublished)
                    {
                        try {
                            this.observer.OnError(new InvalidOperationException("sequence is empty")); }
                        finally {
                            this.Dispose(); }
                    }
                    else
                    {
                        try {
                            this.observer.OnCompleted(); }
                        finally {
                            this.Dispose(); }
                    }
                }
            }
        }
    }
}