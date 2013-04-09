// Copyright 2012-2013 Chris Patterson
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
// the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for
// the specific language governing permissions and limitations under the License.
namespace Taskell
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;


    static class TaskUtil
    {
        static readonly Task _defaultCompleted = Completed(default(Unit));

        internal static Task Canceled()
        {
            return CancelCache<Unit>.CancelledTask;
        }

        internal static Task FastUnwrap(this Task<Task> task)
        {
            Task innerTask = task.Status == TaskStatus.RanToCompletion
                                 ? task.Result
                                 : null;
            return innerTask ?? task.Unwrap();
        }


        internal static Task Completed()
        {
            return _defaultCompleted;
        }

        internal static Task<T> Completed<T>(T result)
        {
            var source = new TaskCompletionSource<T>();
            source.SetResult(result);
            return source.Task;
        }

        internal static Task Faulted(Exception exception)
        {
            return Faulted<Unit>(exception);
        }

        internal static Task<T> Faulted<T>(Exception exception)
        {
            var source = new TaskCompletionSource<T>();
            source.SetException(exception);
            return source.Task;
        }

        internal static Task Faulted(IEnumerable<Exception> exceptions)
        {
            return Faulted<Unit>(exceptions);
        }

        internal static Task<T> Faulted<T>(IEnumerable<Exception> exceptions)
        {
            var source = new TaskCompletionSource<T>();
            source.SetException(exceptions);
            return source.Task;
        }

        internal static Task RunSynchronously(Action action,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                return Canceled();

            try
            {
                action();
                return Completed();
            }
            catch (Exception ex)
            {
                return Faulted(ex);
            }
        }

        internal static bool TrySetFromTask<T>(this TaskCompletionSource<T> source, Task task, T defaultValue = default(T))
        {
            if (task.Status == TaskStatus.Canceled)
                return source.TrySetCanceled();

            if (task.Status == TaskStatus.Faulted)
                return source.TrySetException(task.Exception.InnerExceptions);

            if (task.Status == TaskStatus.RanToCompletion)
            {
                var taskOfT = task as Task<T>;
                return source.TrySetResult(taskOfT != null
                                               ? taskOfT.Result
                                               : defaultValue);
            }

            return false;
        }

        internal static void MarkObserved(this Task task)
        {
            if (!task.IsCompleted)
                return;

            Exception unused = task.Exception;
        }

        /// <summary>
        /// Executes a task after the previous task is completed, taking the fast track if it is already completed
        /// otherwise deferring to async execution
        /// </summary>
        /// <param name="task"></param>
        /// <param name="continuationTask"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="runSynchronously"></param>
        /// <returns></returns>
        internal static Task Then(this Task task, Func<Task> continuationTask, CancellationToken cancellationToken,
            bool runSynchronously = true)
        {
            if (task.IsCompleted)
            {
                if (task.IsFaulted)
                    return Faulted(task.Exception.InnerExceptions);

                if (task.IsCanceled || cancellationToken.IsCancellationRequested)
                    return Canceled();

                if (task.Status == TaskStatus.RanToCompletion)
                {
                    try
                    {
                        return continuationTask();
                    }
                    catch (Exception ex)
                    {
                        return Faulted(ex);
                    }
                }
            }

            return ExecuteAsync(task, continuationTask, cancellationToken, runSynchronously);
        }

        static Task ExecuteAsync(Task task, Func<Task> continuationTask, CancellationToken cancellationToken,
            bool runSynchronously)
        {
            var source = new TaskCompletionSource<Task>();
            task.ContinueWith(innerTask =>
                {
                    if (innerTask.IsFaulted)
                        source.TrySetException(innerTask.Exception.InnerExceptions);
                    else if (innerTask.IsCanceled || cancellationToken.IsCancellationRequested)
                        source.TrySetCanceled();
                    else
                        source.TrySetResult(continuationTask());
                }, runSynchronously
                       ? TaskContinuationOptions.ExecuteSynchronously
                       : TaskContinuationOptions.None);

            return source.Task.FastUnwrap();
        }


        static class CancelCache<T>
        {
            public static readonly Task<T> CancelledTask = GetCancelledTask();

            static Task<T> GetCancelledTask()
            {
                var source = new TaskCompletionSource<T>();
                source.SetCanceled();
                return source.Task;
            }
        }


        struct Unit
        {
        }
    }
}