#if !UNITY_METRO

using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers
{
    public static partial class Scheduler
    {
        public static readonly IScheduler ThreadPool = new ThreadPoolScheduler();

        class ThreadPoolScheduler : IScheduler, ISchedulerPeriodic
        {
            public ThreadPoolScheduler()
            {
            }

            public DateTimeOffset Now
            {
                get { return Scheduler.Now; }
            }

            public IDisposable Schedule(Action action)
            {
                var d = new BooleanDisposable();

                global::System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    if (!d.IsDisposed)
                    {
                        action();
                    }
                });

                return d;
            }

            public IDisposable Schedule(DateTimeOffset dueTime, Action action)
            {
                return this.Schedule(dueTime - this.Now, action);
            }

            public IDisposable Schedule(TimeSpan dueTime, Action action)
            {
                return new Timer(dueTime, action);
            }

            public IDisposable SchedulePeriodic(TimeSpan period, Action action)
            {
                return new PeriodicTimer(period, action);
            }

            public void ScheduleQueueing<T>(ICancelable cancel, T state, Action<T> action)
            {
                global::System.Threading.ThreadPool.QueueUserWorkItem(callBackState =>
                {
                    if (!cancel.IsDisposed)
                    {
                        action((T)callBackState);
                    }
                }, state);
            }

            // timer was borrwed from Rx Official

            sealed class Timer : IDisposable
            {
                static readonly HashSet<global::System.Threading.Timer> s_timers = new HashSet<global::System.Threading.Timer>();

                private readonly SingleAssignmentDisposable _disposable;

                private Action _action;
                private global::System.Threading.Timer _timer;

                private bool _hasAdded;
                private bool _hasRemoved;

                public Timer(TimeSpan dueTime, Action action)
                {
                    this._disposable = new SingleAssignmentDisposable();
                    this._disposable.Disposable = Disposable.Create(this.Unroot);

                    this._action = action;
                    this._timer = new global::System.Threading.Timer(this.Tick, null, dueTime, TimeSpan.FromMilliseconds(global::System.Threading.Timeout.Infinite));

                    lock (s_timers)
                    {
                        if (!this._hasRemoved)
                        {
                            s_timers.Add(this._timer);

                            this._hasAdded = true;
                        }
                    }
                }

                private void Tick(object state)
                {
                    try
                    {
                        if (!this._disposable.IsDisposed)
                        {
                            this._action();
                        }
                    }
                    finally
                    {
                        this.Unroot();
                    }
                }

                private void Unroot()
                {
                    this._action = Stubs.Nop;

                    var timer = default(global::System.Threading.Timer);

                    lock (s_timers)
                    {
                        if (!this._hasRemoved)
                        {
                            timer = this._timer;
                            this._timer = null;

                            if (this._hasAdded && timer != null)
                                s_timers.Remove(timer);

                            this._hasRemoved = true;
                        }
                    }

                    if (timer != null)
                        timer.Dispose();
                }

                public void Dispose()
                {
                    this._disposable.Dispose();
                }
            }

            sealed class PeriodicTimer : IDisposable
            {
                static readonly HashSet<global::System.Threading.Timer> s_timers = new HashSet<global::System.Threading.Timer>();

                private Action _action;
                private global::System.Threading.Timer _timer;
                private readonly AsyncLock _gate;

                public PeriodicTimer(TimeSpan period, Action action)
                {
                    this._action = action;
                    this._timer = new global::System.Threading.Timer(this.Tick, null, period, period);
                    this._gate = new AsyncLock();

                    lock (s_timers)
                    {
                        s_timers.Add(this._timer);
                    }
                }

                private void Tick(object state)
                {
                    this._gate.Wait(() =>
                    {
                        this._action();
                    });
                }

                public void Dispose()
                {
                    var timer = default(global::System.Threading.Timer);

                    lock (s_timers)
                    {
                        timer = this._timer;
                        this._timer = null;

                        if (timer != null)
                            s_timers.Remove(timer);
                    }

                    if (timer != null)
                    {
                        timer.Dispose();
                        this._action = Stubs.Nop;
                    }
                }
            }
        }
    }
}

#endif