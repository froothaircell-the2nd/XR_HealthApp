using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using Cysharp.Threading.Tasks;
using CoreResources.Singleton;
using Unity.VisualScripting;

namespace CoreResources.Managers
{
    /// <summary>
    /// Utilities to run tasks on separate threads 
    /// and move them back to the main thread when 
    /// required
    /// </summary>
    public class TaskUtilitiesManager : NonMonoSingleton<TaskUtilitiesManager>
    {
        private static ObjectIDGenerator IDGenerator;

        // We'll make use of object IDs to distinguish between functions
        private static Dictionary<long, CancellationTokenSource> _genericTaskCancellationHandles;

        private static string _newIdGenerated = "ID generated for the first time. Will not be present in the dictionary";

        #region Overrides
        public override void InitSingleton()
        {
            base.InitSingleton();

            IDGenerator = new ObjectIDGenerator();

            _genericTaskCancellationHandles = new Dictionary<long, CancellationTokenSource>();
        }

        public override void CleanSingleton()
        {
            _genericTaskCancellationHandles.Clear();
            _genericTaskCancellationHandles = null;
            IDGenerator = null;

            base.CleanSingleton();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Wrapper function to run an arbitrary task on the thread pool
        /// </summary>
        /// <param name="task">Task to run</param>
        /// <param name="OnSuccess">Callback to run on success</param>
        /// <param name="OnFail">Callback to run on fail</param>
        /// <param name="setTimeout">Flag for setting a timeout of the task</param>
        /// <param name="timeout">Timeout value of the task</param>
        public static void RunTask(Func<CancellationToken, UniTask> task, Action OnSuccess = null, Action OnFail = null, bool setTimeout = false, int timeout = 5000)
        {
            var cts = new CancellationTokenSource();
            if (!TrySetCancellationToken(task, cts))
                return;

            if (setTimeout)
                cts.CancelAfterSlim(timeout);

            UniTask.RunOnThreadPool(() => TaskWrapper(task, cts.Token, OnSuccess, OnFail));
        }

        /// <summary>
        /// Wrapper function to run an arbitrary task with a return value on the thread pool
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="task">Task to run</param>
        /// <param name="OnSuccess">Callback to run on success</param>
        /// <param name="OnFail">Callback to run on fail</param>
        /// <param name="setTimeout">Flag for setting a timeout of the task</param>
        /// <param name="timeout">Timeout value of the task</param>
        public static void RunTask<T>(Func<CancellationToken, UniTask<T>> task, Action<T> OnSuccess = null, Action OnFail = null, bool setTimeout = false, int timeout = 5000)
        {
            var cts = new CancellationTokenSource();
            if (!TrySetCancellationToken(task, cts))
                return;

            if (setTimeout)
                cts.CancelAfterSlim(timeout);

            UniTask.RunOnThreadPool(() => TaskWrapper<T>(task, cts.Token, OnSuccess, OnFail));
        }

        /// <summary>
        /// Wrapper function to run a function after a certain predicate is fulfilled. 
        /// It has a timeout set by default to ensure it doesn't take up a thread forever.
        /// </summary>
        /// <param name="predicate">Predicate to fulfill</param>
        /// <param name="OnWaitComplete">Callback for when the wait time is completed</param>
        /// <param name="timeout">Timeout value</param>
        /// <param name="OnSuccess">Callback to run on success</param>
        /// <param name="OnFail">Callback to run on fail</param>
        public static void RunAfter(Func<bool> predicate, Action OnWaitComplete, int timeout = 10000, Action OnSuccess = null, Action OnFail = null)
        {
            RunTask(async (token) =>
            {
                await UniTask.WaitUntil(predicate, PlayerLoopTiming.Update, token);

                await UniTask.SwitchToMainThread();
                OnWaitComplete.Invoke();
            }, OnSuccess, OnFail, true, timeout);
        }

        /// <summary>
        /// Wrapper function to run a function after a certain time delay is fulfilled.
        /// </summary>
        /// <param name="milliSecondDelay">Delay in milliseconds</param>
        /// <param name="OnWaitComplete">Callback for when the wait time is completed</param>
        /// <param name="OnSuccess">Callback to run on success</param>
        /// <param name="OnFail">Callback to run on fail</param>
        public static void RunAfter(int milliSecondDelay, Action OnWaitComplete, Action OnSuccess = null, Action OnFail = null)
        {
            RunTask(async (token) =>
            {
                await UniTask.Delay(milliSecondDelay, false, PlayerLoopTiming.Update, token);
                OnWaitComplete.Invoke();
            }, OnSuccess, OnFail);
        }

        /// <summary>
        /// Cancel a task by the function reference. Lambda function will not work
        /// </summary>
        /// <param name="task">task to cancel</param>
        public static void CancelTask(Func<CancellationToken, UniTask> task)
        {
            if (!TryGetCancellationToken(task, out var cts))
                return;

            cts.Cancel();
        }

        /// <summary>
        /// Cancel a task with a return type by the function reference. Lambda function will not work
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="task">task to cancel</param>
        public static void CancelTask<T>(Func<CancellationToken, UniTask<T>> task)
        {
            if (!TryGetCancellationToken(task, out var cts))
                return;

            cts.Cancel();
        }

        /// <summary>
        /// Cancels all tasks stored in the dictionary. Use sparingly
        /// </summary>
        public static void CancelAllTasks()
        {
            foreach (var action in _genericTaskCancellationHandles)
            {
                action.Value.Cancel();
            }

            _genericTaskCancellationHandles.Clear();
        }
        #endregion

        #region Cancellation Token Management
        private static bool TrySetCancellationToken(Func<CancellationToken, UniTask> task, CancellationTokenSource tokenSource)
        {
            var ID = IDGenerator.GetId(task.Method, out var first);
            if (!_genericTaskCancellationHandles.TryAdd(ID, tokenSource))
            {
                Debug.LogError("Task could not be added to the dictionary");
                return false;
            }

            return true;
        }

        private static bool TrySetCancellationToken<T>(Func<CancellationToken, UniTask<T>> task, CancellationTokenSource tokenSource)
        {
            var ID = IDGenerator.GetId(task.Method, out var first);
            if (!_genericTaskCancellationHandles.TryAdd(ID, tokenSource))
            {
                Debug.LogError("Task could not be added to the dictionary");
                return false;
            }
            return true;
        }

        private static bool TryGetCancellationToken(Func<CancellationToken, UniTask> task, out CancellationTokenSource cts)
        {
            var ID = IDGenerator.GetId(task.Method, out var first);
            if (first)
            {
                Debug.LogWarning(_newIdGenerated);
            }

            if (!_genericTaskCancellationHandles.TryGetValue(ID, out var tokenSource))
            {
                Debug.LogWarning("Task does not have a corresponding Cancellation token");
                cts = null;
                return false;
            }

            cts = tokenSource;
            return true;
        }

        private static bool TryGetCancellationToken<T>(Func<CancellationToken, UniTask<T>> task, out CancellationTokenSource cts)
        {
            var ID = IDGenerator.GetId(task.Method, out var first);
            if (first)
            {
                Debug.LogWarning(_newIdGenerated);
            }

            if (!_genericTaskCancellationHandles.TryGetValue(ID, out var tokenSource))
            {
                Debug.LogWarning("Task does not have a corresponding Cancellation token");
                cts = null;
                return false;
            }

            cts = tokenSource;
            return true;
        }

        private static bool TryRemoveCancellationToken(Func<CancellationToken, UniTask> task)
        {
            var ID = IDGenerator.GetId(task.Method, out var first);
            if (first)
            {
                Debug.LogWarning(_newIdGenerated);
            }

            if (!_genericTaskCancellationHandles.Remove(ID))
            {
                Debug.LogWarning("Cancellation Token no longer present in dictionary");
                return false;
            }

            return true;
        }

        private static bool TryRemoveCancellationToken<T>(Func<CancellationToken, UniTask<T>> task)
        {
            var ID = IDGenerator.GetId(task.Method, out var first);
            if (first)
            {
                Debug.LogWarning(_newIdGenerated);
            }

            if (!_genericTaskCancellationHandles.Remove(ID))
            {
                Debug.LogWarning("Cancellation Token no longer present in dictionary");
                return false;
            }

            return true;
        }
        #endregion

        #region Utils
        private static async UniTask TaskWrapper(Func<CancellationToken, UniTask> task, CancellationToken token, Action OnSuccess = null, Action OnFail = null)
        {
            try
            {
                await task.Invoke(token);
                TryRemoveCancellationToken(task);
                if (OnSuccess != null)
                {
                    await UniTask.SwitchToMainThread();
                    OnSuccess.Invoke();
                }
            }
            catch (OperationCanceledException ex)
            {
                Debug.Log($"Task cancelled: {ex}");
                TryRemoveCancellationToken(task);
                if (OnFail != null)
                {
                    await UniTask.SwitchToMainThread();
                    OnFail.Invoke();
                }
            }
        }

        private static async UniTask<T> TaskWrapper<T>(Func<CancellationToken, UniTask<T>> task, CancellationToken token, Action<T> OnSuccess = null, Action OnFail = null)
        {
            try
            {
                var res = await task.Invoke(token);
                TryRemoveCancellationToken(task);
                if (OnSuccess != null)
                {
                    await UniTask.SwitchToMainThread();
                    OnSuccess.Invoke(res);
                }
                return res;
            }
            catch (OperationCanceledException ex)
            {
                Debug.LogError($"Task cancelled: {ex}");
                TryRemoveCancellationToken(task);
                if (OnFail != null)
                {
                    await UniTask.SwitchToMainThread();
                    OnFail.Invoke();
                }
                return default;
            }
        }
        #endregion
    }
}