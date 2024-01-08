using System;
using System.Collections;
using System.Collections.Generic;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil
{
    /// <summary>
    /// Simple supports(only yield return null) lightweight, threadsafe coroutine dispatcher.
    /// </summary>
    public class MicroCoroutine
    {
        const int InitialSize = 16;

        readonly object runningAndQueueLock = new object();
        readonly object arrayLock = new object();
        readonly Action<Exception> unhandledExceptionCallback;

        int tail = 0;
        bool running = false;
        IEnumerator[] coroutines = new IEnumerator[InitialSize];
        Queue<IEnumerator> waitQueue = new Queue<IEnumerator>();

        public MicroCoroutine(Action<Exception> unhandledExceptionCallback)
        {
            this.unhandledExceptionCallback = unhandledExceptionCallback;
        }

        public void AddCoroutine(IEnumerator enumerator)
        {
            lock (this.runningAndQueueLock)
            {
                if (this.running)
                {
                    this.waitQueue.Enqueue(enumerator);
                    return;
                }
            }

            // worst case at multi threading, wait lock until finish Run() but it is super rarely.
            lock (this.arrayLock)
            {
                // Ensure Capacity
                if (this.coroutines.Length == this.tail)
                {
                    Array.Resize(ref this.coroutines, checked(this.tail * 2));
                }

                this.coroutines[this.tail++] = enumerator;
            }
        }

        public void Run()
        {
            lock (this.runningAndQueueLock)
            {
                this.running = true;
            }

            lock (this.arrayLock)
            {
                var j = this.tail - 1;

                // eliminate array-bound check for i
                for (int i = 0; i < this.coroutines.Length; i++)
                {
                    var coroutine = this.coroutines[i];
                    if (coroutine != null)
                    {
                        try
                        {
                            if (!coroutine.MoveNext())
                            {
                                this.coroutines[i] = null;
                            }
                            else
                            {
#if UNITY_EDITOR
                                // validation only on Editor.
                                if (coroutine.Current != null)
                                {
                                    UnityEngine.Debug.LogWarning("MicroCoroutine supports only yield return null. return value = " + coroutine.Current);
                                }
#endif

                                continue; // next i 
                            }
                        }
                        catch (Exception ex)
                        {
                            this.coroutines[i] = null;
                            try
                            {
                                this.unhandledExceptionCallback(ex);
                            }
                            catch { }
                        }
                    }

                    // find null, loop from tail
                    while (i < j)
                    {
                        var fromTail = this.coroutines[j];
                        if (fromTail != null)
                        {
                            try
                            {
                                if (!fromTail.MoveNext())
                                {
                                    this.coroutines[j] = null;
                                    j--;
                                    continue; // next j
                                }
                                else
                                {
#if UNITY_EDITOR
                                    // validation only on Editor.
                                    if (fromTail.Current != null)
                                    {
                                        UnityEngine.Debug.LogWarning("MicroCoroutine supports only yield return null. return value = " + coroutine.Current);
                                    }
#endif

                                    // swap
                                    this.coroutines[i] = fromTail;
                                    this.coroutines[j] = null;
                                    j--;
                                    goto NEXT_LOOP; // next i
                                }
                            }
                            catch (Exception ex)
                            {
                                this.coroutines[j] = null;
                                j--;
                                try
                                {
                                    this.unhandledExceptionCallback(ex);
                                }
                                catch { }
                                continue; // next j
                            }
                        }
                        else
                        {
                            j--;
                        }
                    }

                    this.tail = i; // loop end
                    break; // LOOP END

                    NEXT_LOOP:
                    continue;
                }


                lock (this.runningAndQueueLock)
                {
                    this.running = false;
                    while (this.waitQueue.Count != 0)
                    {
                        if (this.coroutines.Length == this.tail)
                        {
                            Array.Resize(ref this.coroutines, checked(this.tail * 2));
                        }

                        this.coroutines[this.tail++] = this.waitQueue.Dequeue();
                    }
                }
            }
        }
    }
}