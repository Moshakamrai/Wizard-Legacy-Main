using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class SkipWhileObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly Func<T, bool> predicate;
        readonly Func<T, int, bool> predicateWithIndex;

        public SkipWhileObservable(IObservable<T> source, Func<T, bool> predicate)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.predicate = predicate;
        }

        public SkipWhileObservable(IObservable<T> source, Func<T, int, bool> predicateWithIndex)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.predicateWithIndex = predicateWithIndex;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            if (this.predicate != null)
            {
                return new SkipWhile(this, observer, cancel).Run();
            }
            else
            {
                return new SkipWhile_(this, observer, cancel).Run();
            }
        }

        class SkipWhile : OperatorObserverBase<T, T>
        {
            readonly SkipWhileObservable<T> parent;
            bool endSkip = false;

            public SkipWhile(SkipWhileObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(T value)
            {
                if (!this.endSkip)
                {
                    try
                    {
                        this.endSkip = !this.parent.predicate(value);
                    }
                    catch (Exception ex)
                    {
                        try {
                            this.observer.OnError(ex); } finally {
                            this.Dispose(); }
                        return;
                    }

                    if (!this.endSkip) return;
                }

                this.observer.OnNext(value);
            }

            public override void OnError(Exception error)
            {
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }
        }

        class SkipWhile_ : OperatorObserverBase<T, T>
        {
            readonly SkipWhileObservable<T> parent;
            bool endSkip = false;
            int index = 0;

            public SkipWhile_(SkipWhileObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(T value)
            {
                if (!this.endSkip)
                {
                    try
                    {
                        this.endSkip = !this.parent.predicateWithIndex(value, this.index++);
                    }
                    catch (Exception ex)
                    {
                        try {
                            this.observer.OnError(ex); } finally {
                            this.Dispose(); }
                        return;
                    }

                    if (!this.endSkip) return;
                }

                this.observer.OnNext(value);
            }

            public override void OnError(Exception error)
            {
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }
        }
    }
}