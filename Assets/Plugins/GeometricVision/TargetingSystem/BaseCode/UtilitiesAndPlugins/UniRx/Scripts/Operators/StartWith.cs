using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class StartWithObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly T value;
        readonly Func<T> valueFactory;

        public StartWithObservable(IObservable<T> source, T value)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.value = value;
        }

        public StartWithObservable(IObservable<T> source, Func<T> valueFactory)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.valueFactory = valueFactory;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new StartWith(this, observer, cancel).Run();
        }

        class StartWith : OperatorObserverBase<T, T>
        {
            readonly StartWithObservable<T> parent;

            public StartWith(StartWithObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                T t;
                if (this.parent.valueFactory == null)
                {
                    t = this.parent.value;
                }
                else
                {
                    try
                    {
                        t = this.parent.valueFactory();
                    }
                    catch (Exception ex)
                    {
                        try {
                            this.observer.OnError(ex); }
                        finally {
                            this.Dispose(); }
                        return Disposable.Empty;
                    }
                }

                this.OnNext(t);
                return this.parent.source.Subscribe(this.observer); // good bye StartWithObserver
            }

            public override void OnNext(T value)
            {
                this.observer.OnNext(value);
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
                try {
                    this.observer.OnCompleted(); }
                finally {
                    this.Dispose(); }
            }
        }
    }
}