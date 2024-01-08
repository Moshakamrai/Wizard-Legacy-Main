using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil
{
    public class ListObserver<T> : IObserver<T>
    {
        private readonly ImmutableList<IObserver<T>> _observers;

        public ListObserver(ImmutableList<IObserver<T>> observers)
        {
            this._observers = observers;
        }

        public void OnCompleted()
        {
            var targetObservers = this._observers.Data;
            for (int i = 0; i < targetObservers.Length; i++)
            {
                targetObservers[i].OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            var targetObservers = this._observers.Data;
            for (int i = 0; i < targetObservers.Length; i++)
            {
                targetObservers[i].OnError(error);
            }
        }

        public void OnNext(T value)
        {
            var targetObservers = this._observers.Data;
            for (int i = 0; i < targetObservers.Length; i++)
            {
                targetObservers[i].OnNext(value);
            }
        }

        internal IObserver<T> Add(IObserver<T> observer)
        {
            return new ListObserver<T>(this._observers.Add(observer));
        }

        internal IObserver<T> Remove(IObserver<T> observer)
        {
            var i = Array.IndexOf(this._observers.Data, observer);
            if (i < 0)
                return this;

            if (this._observers.Data.Length == 2)
            {
                return this._observers.Data[1 - i];
            }
            else
            {
                return new ListObserver<T>(this._observers.Remove(observer));
            }
        }
    }

    public class EmptyObserver<T> : IObserver<T>
    {
        public static readonly EmptyObserver<T> Instance = new EmptyObserver<T>();

        EmptyObserver()
        {

        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(T value)
        {
        }
    }

    public class ThrowObserver<T> : IObserver<T>
    {
        public static readonly ThrowObserver<T> Instance = new ThrowObserver<T>();

        ThrowObserver()
        {

        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            error.Throw();
        }

        public void OnNext(T value)
        {
        }
    }

    public class DisposedObserver<T> : IObserver<T>
    {
        public static readonly DisposedObserver<T> Instance = new DisposedObserver<T>();

        DisposedObserver()
        {

        }

        public void OnCompleted()
        {
            throw new ObjectDisposedException("");
        }

        public void OnError(Exception error)
        {
            throw new ObjectDisposedException("");
        }

        public void OnNext(T value)
        {
            throw new ObjectDisposedException("");
        }
    }
}