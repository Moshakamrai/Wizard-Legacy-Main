﻿using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects
{
    public static class SubjectExtensions
    {
        public static ISubject<T> Synchronize<T>(this ISubject<T> subject)
        {
            return new AnonymousSubject<T>((subject as IObserver<T>).Synchronize(), subject);
        }

        public static ISubject<T> Synchronize<T>(this ISubject<T> subject, object gate)
        {
            return new AnonymousSubject<T>((subject as IObserver<T>).Synchronize(gate), subject);
        }

        class AnonymousSubject<T, U> : ISubject<T, U>
        {
            readonly IObserver<T> observer;
            readonly IObservable<U> observable;

            public AnonymousSubject(IObserver<T> observer, IObservable<U> observable)
            {
                this.observer = observer;
                this.observable = observable;
            }

            public void OnCompleted()
            {
                this.observer.OnCompleted();
            }

            public void OnError(Exception error)
            {
                if (error == null) throw new ArgumentNullException("error");

                this.observer.OnError(error);
            }

            public void OnNext(T value)
            {
                this.observer.OnNext(value);
            }

            public IDisposable Subscribe(IObserver<U> observer)
            {
                if (observer == null) throw new ArgumentNullException("observer");

                return this.observable.Subscribe(observer);
            }
        }

        class AnonymousSubject<T> : AnonymousSubject<T, T>, ISubject<T>
        {
            public AnonymousSubject(IObserver<T> observer, IObservable<T> observable)
                : base(observer, observable)
            {
            }
        }
    }
}