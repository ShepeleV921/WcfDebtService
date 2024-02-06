using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Tools.Rosreestr
{
    public abstract class AsyncTaskItemBase : IDisposable
    {
        protected readonly Dispatcher _dispatcher;


        protected AsyncTaskItemBase()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public abstract void Execute();

        public abstract void Dispose();
    }

    public class AsyncTaskItem : AsyncTaskItemBase
    {
        public Task Task { get; private set; }

        public Action<AsyncWorkerResult<object>> CompleteAction { get; set; }


        public AsyncTaskItem(Task task)
        {
            Task = task;
        }

        public override void Execute()
        {
            AsyncWorkerResult<object> res = new AsyncWorkerResult<object>();

            Task.RunSynchronously();

            if (Task.Exception != null)
            {
                res.Error = Task.Exception;
            }

            if (CompleteAction != null)
            {
                _dispatcher.Invoke(CompleteAction, res);
            }
        }

        public override void Dispose()
        {
            Task?.Dispose();

            Task = null;
            CompleteAction = null;
        }
    }

    public class AsyncTaskItem<T> : AsyncTaskItemBase
    {
        public Task<T> Task { get; private set; }

        public Action<AsyncWorkerResult<T>> CompleteAction { get; set; }


        public AsyncTaskItem(Task<T> task)
        {
            Task = task;
        }

        public override void Execute()
        {
            AsyncWorkerResult<T> res = new AsyncWorkerResult<T>();
            try
            {
                Task.RunSynchronously();
                res.Result = Task.Result;
            }
            catch (Exception exc)
            {
                res.Error = exc;
            }

            if (CompleteAction != null)
            {
                _dispatcher.Invoke(CompleteAction, res);
            }
        }

        public override void Dispose()
        {
            Task?.Dispose();

            Task = null;
            CompleteAction = null;
        }
    }
}
