using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class TakeWhileObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly Func<T, bool> predicate;
        readonly Func<T, int, bool> predicateWithIndex;

        public TakeWhileObservable(IObservable<T> source, Func<T, bool> predicate)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.predicate = predicate;
        }

        public TakeWhileObservable(IObservable<T> source, Func<T, int, bool> predicateWithIndex)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.predicateWithIndex = predicateWithIndex;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            if (this.predicate != null)
            {
                return new TakeWhile(this, observer, cancel).Run();
            }
            else
            {
                return new TakeWhile_(this, observer, cancel).Run();
            }
        }

        class TakeWhile : OperatorObserverBase<T, T>
        {
            readonly TakeWhileObservable<T> parent;

            public TakeWhile(TakeWhileObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
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
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); }
                    return;
                }

                if (isPassed)
                {
                    this.observer.OnNext(value);
                }
                else
                {
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                }
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

        class TakeWhile_ : OperatorObserverBase<T, T>
        {
            readonly TakeWhileObservable<T> parent;
            int index = 0;

            public TakeWhile_(TakeWhileObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(T value)
            {
                bool isPassed;
                try
                {
                    isPassed = this.parent.predicateWithIndex(value, this.index++);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); }
                    return;
                }

                if (isPassed)
                {
                    this.observer.OnNext(value);
                }
                else
                {
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                }
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