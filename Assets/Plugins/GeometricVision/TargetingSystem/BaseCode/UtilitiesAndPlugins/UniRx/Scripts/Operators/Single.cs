using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class SingleObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly bool useDefault;
        readonly Func<T, bool> predicate;

        public SingleObservable(IObservable<T> source, bool useDefault)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.useDefault = useDefault;
        }

        public SingleObservable(IObservable<T> source, Func<T, bool> predicate, bool useDefault)
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
                return this.source.Subscribe(new Single(this, observer, cancel));
            }
            else
            {
                return this.source.Subscribe(new Single_(this, observer, cancel));
            }
        }

        class Single : OperatorObserverBase<T, T>
        {
            readonly SingleObservable<T> parent;
            bool seenValue;
            T lastValue;

            public Single(SingleObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.seenValue = false;
            }

            public override void OnNext(T value)
            {
                if (this.seenValue)
                {
                    try {
                        this.observer.OnError(new InvalidOperationException("sequence is not single")); }
                    finally {
                        this.Dispose(); }
                }
                else
                {
                    this.seenValue = true;
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
                    if (!this.seenValue)
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
                    if (!this.seenValue)
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

        class Single_ : OperatorObserverBase<T, T>
        {
            readonly SingleObservable<T> parent;
            bool seenValue;
            T lastValue;

            public Single_(SingleObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.seenValue = false;
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
                    if (this.seenValue)
                    {
                        try {
                            this.observer.OnError(new InvalidOperationException("sequence is not single")); }
                        finally {
                            this.Dispose(); }
                        return;
                    }
                    else
                    {
                        this.seenValue = true;
                        this.lastValue = value;
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
                    if (!this.seenValue)
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
                    if (!this.seenValue)
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