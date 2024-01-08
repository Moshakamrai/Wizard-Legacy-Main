using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class LastObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly bool useDefault;
        readonly Func<T, bool> predicate;

        public LastObservable(IObservable<T> source, bool useDefault)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.useDefault = useDefault;
        }

        public LastObservable(IObservable<T> source, Func<T, bool> predicate, bool useDefault)
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
                return this.source.Subscribe(new Last(this, observer, cancel));
            }
            else
            {
                return this.source.Subscribe(new Last_(this, observer, cancel));
            }
        }

        class Last : OperatorObserverBase<T, T>
        {
            readonly LastObservable<T> parent;
            bool notPublished;
            T lastValue;

            public Last(LastObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.notPublished = true;
            }

            public override void OnNext(T value)
            {
                this.notPublished = false;
                this.lastValue = value;
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
                    else
                    {
                        this.observer.OnNext(this.lastValue);
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
                        this.observer.OnNext(this.lastValue);
                        try {
                            this.observer.OnCompleted(); }
                        finally {
                            this.Dispose(); }
                    }
                }
            }
        }

        class Last_ : OperatorObserverBase<T, T>
        {
            readonly LastObservable<T> parent;
            bool notPublished;
            T lastValue;

            public Last_(LastObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.notPublished = true;
            }

            public override void OnNext(T value)
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
                    this.lastValue = value;
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
                    else
                    {
                        this.observer.OnNext(this.lastValue);
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
                        this.observer.OnNext(this.lastValue);
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