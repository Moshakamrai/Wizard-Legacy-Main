using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil
{
    public class ThreadSafeQueueWorker
    {
        const int MaxArrayLength = 0X7FEFFFFF;
        const int InitialSize = 16;

        object gate = new object();
        bool dequing = false;

        int actionListCount = 0;
        Action<object>[] actionList = new Action<object>[InitialSize];
        object[] actionStates = new object[InitialSize];

        int waitingListCount = 0;
        Action<object>[] waitingList = new Action<object>[InitialSize];
        object[] waitingStates = new object[InitialSize];

        public void Enqueue(Action<object> action, object state)
        {
            lock (this.gate)
            {
                if (this.dequing)
                {
                    // Ensure Capacity
                    if (this.waitingList.Length == this.waitingListCount)
                    {
                        var newLength = this.waitingListCount * 2;
                        if ((uint)newLength > MaxArrayLength) newLength = MaxArrayLength;

                        var newArray = new Action<object>[newLength];
                        var newArrayState = new object[newLength];
                        Array.Copy(this.waitingList, newArray, this.waitingListCount);
                        Array.Copy(this.waitingStates, newArrayState, this.waitingListCount);
                        this.waitingList = newArray;
                        this.waitingStates = newArrayState;
                    }

                    this.waitingList[this.waitingListCount] = action;
                    this.waitingStates[this.waitingListCount] = state;
                    this.waitingListCount++;
                }
                else
                {
                    // Ensure Capacity
                    if (this.actionList.Length == this.actionListCount)
                    {
                        var newLength = this.actionListCount * 2;
                        if ((uint)newLength > MaxArrayLength) newLength = MaxArrayLength;

                        var newArray = new Action<object>[newLength];
                        var newArrayState = new object[newLength];
                        Array.Copy(this.actionList, newArray, this.actionListCount);
                        Array.Copy(this.actionStates, newArrayState, this.actionListCount);
                        this.actionList = newArray;
                        this.actionStates = newArrayState;
                    }

                    this.actionList[this.actionListCount] = action;
                    this.actionStates[this.actionListCount] = state;
                    this.actionListCount++;
                }
            }
        }

        public void ExecuteAll(Action<Exception> unhandledExceptionCallback)
        {
            lock (this.gate)
            {
                if (this.actionListCount == 0) return;

                this.dequing = true;
            }

            for (int i = 0; i < this.actionListCount; i++)
            {
                var action = this.actionList[i];
                var state = this.actionStates[i];
                try
                {
                    action(state);
                }
                catch (Exception ex)
                {
                    unhandledExceptionCallback(ex);
                }
                finally
                {
                    // Clear
                    this.actionList[i] = null;
                    this.actionStates[i] = null;
                }
            }

            lock (this.gate)
            {
                this.dequing = false;

                var swapTempActionList = this.actionList;
                var swapTempActionStates = this.actionStates;

                this.actionListCount = this.waitingListCount;
                this.actionList = this.waitingList;
                this.actionStates = this.waitingStates;

                this.waitingListCount = 0;
                this.waitingList = swapTempActionList;
                this.waitingStates = swapTempActionStates;
            }
        }
    }
}