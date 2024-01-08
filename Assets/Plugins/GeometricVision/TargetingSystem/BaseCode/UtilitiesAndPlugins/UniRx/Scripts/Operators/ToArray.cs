using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class ToArrayObservable<TSource> : OperatorObservableBase<TSource[]>
    {
        readonly IObservable<TSource> source;

        public ToArrayObservable(IObservable<TSource> source)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
        }

        protected override IDisposable SubscribeCore(IObserver<TSource[]> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new ToArray(observer, cancel));
        }

        class ToArray : OperatorObserverBase<TSource, TSource[]>
        {
            readonly List<TSource> list = new List<TSource>();

            public ToArray(IObserver<TSource[]> observer, IDisposable cancel)
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
                TSource[] result;
                try
                {
                    result = this.list.ToArray();
                }
                catch (Exception ex) 
                {
                    try {
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); }
                    return;
                }

                this.observer.OnNext(result);
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); };
            }
        }
    }
}