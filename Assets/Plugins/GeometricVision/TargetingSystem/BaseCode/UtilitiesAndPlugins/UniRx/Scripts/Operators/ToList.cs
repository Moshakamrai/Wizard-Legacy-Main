using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class ToListObservable<TSource> : OperatorObservableBase<IList<TSource>>
    {
        readonly IObservable<TSource> source;

        public ToListObservable(IObservable<TSource> source)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
        }

        protected override IDisposable SubscribeCore(IObserver<IList<TSource>> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new ToList(observer, cancel));
        }

        class ToList : OperatorObserverBase<TSource, IList<TSource>>
        {
            readonly List<TSource> list = new List<TSource>();

            public ToList(IObserver<IList<TSource>> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public override void OnNext(TSource value)
            {
                try
                {
                    this.list.Add(value); // sometimes cause error on multithread
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); }
                    return;
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
                this.observer.OnNext(this.list);
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); };
            }
        }
    }
}