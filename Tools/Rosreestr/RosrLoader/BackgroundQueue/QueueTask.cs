using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace Tools.Rosreestr
{
    /// <summary>
    /// Содержит очередь операций, которые выполняются асинхронной очередью
    /// в рамках одной задачи
    /// </summary>
    public class QueueTask
    {
        private readonly Queue<AsyncQueueElementBase> _operationQueue = new Queue<AsyncQueueElementBase>();
        private readonly Dispatcher _dispatcher;

        public AsyncQueueElementBase Finalizer { get; private set; }

        public AsyncQueueElementBase BeginAction { get; private set; }


        public string Name { get; set; }

        public string Description { get; set; }

        public int ElementCount { get { return _operationQueue.Count; } }

        /// <summary>
        /// Зависимая задача, которая выполнится в случае успешного завершения основной задачи
        /// </summary>
        public QueueTask DependentTask { get; set; }


        internal QueueTask(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        internal AsyncQueueElementBase GetElement()
        {
            return _operationQueue.Dequeue();
        }

        internal void Clear()
        {
            _operationQueue.Clear();
        }

        public AsyncQueueElementBase AddAsyncOperation(Action action, Action<AsyncQueueResult> completeAction)
        {
            AsyncQueueElement tmp = new AsyncQueueElement { Action = action, CompleteAction = completeAction };
            _operationQueue.Enqueue(tmp);

            return tmp;
        }

        public AsyncQueueElementBase AddAsyncOperation<T>(Action<T> action, Action<AsyncQueueResult> completeAction, T arg)
        {
            AsyncQueueElement<T> tmp = new AsyncQueueElement<T> { Action = action, CompleteAction = completeAction, Arg1 = arg };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }

        public AsyncQueueElementBase AddAsyncOperation<T1, T2>(Action<T1, T2> action, Action<AsyncQueueResult> completeAction, T1 arg1, T2 arg2)
        {
            AsyncQueueElement<T1, T2> tmp = new AsyncQueueElement<T1, T2>
            {
                Action = action,
                CompleteAction = completeAction,
                Arg1 = arg1,
                Arg2 = arg2
            };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }

        public AsyncQueueElementBase AddAsyncOperation<T1, T2, T3>(
            Action<T1, T2, T3> action, Action<AsyncQueueResult> completeAction, T1 arg1, T2 arg2, T3 arg3)
        {
            AsyncQueueElement<T1, T2, T3> tmp = new AsyncQueueElement<T1, T2, T3>
            {
                Action = action,
                CompleteAction = completeAction,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3
            };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }

        public AsyncQueueElementBase AddAsyncOperation<T1, T2, T3, T4>(
            Action<T1, T2, T3, T4> action, Action<AsyncQueueResult> completeAction, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            AsyncQueueElement<T1, T2, T3, T4> tmp = new AsyncQueueElement<T1, T2, T3, T4>
            {
                Action = action,
                CompleteAction = completeAction,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3,
                Arg4 = arg4
            };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }

        public AsyncQueueElementBase AddAsyncOperation<T1, T2, T3, T4, T5>(
            Action<T1, T2, T3, T4, T5> action, Action<AsyncQueueResult> completeAction, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            AsyncQueueElement<T1, T2, T3, T4, T5> tmp = new AsyncQueueElement<T1, T2, T3, T4, T5>
            {
                Action = action,
                CompleteAction = completeAction,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3,
                Arg4 = arg4,
                Arg5 = arg5
            };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }


        public AsyncQueueElementBase AddSyncOperation(Action action, Action<AsyncQueueResult> completeAction)
        {
            AsyncQueueElement tmp = new AsyncQueueElement
            {
                Dispatcher = _dispatcher,
                Action = action,
                CompleteAction = completeAction
            };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }

        public AsyncQueueElementBase AddSyncOperation<T>(Action<T> action, Action<AsyncQueueResult> completeAction, T arg)
        {
            AsyncQueueElement<T> tmp = new AsyncQueueElement<T>
            {
                Dispatcher = _dispatcher,
                Action = action,
                CompleteAction = completeAction,
                Arg1 = arg
            };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }

        public AsyncQueueElementBase AddSyncOperation<T1, T2>(Action<T1, T2> action, Action<AsyncQueueResult> completeAction, T1 arg1, T2 arg2)
        {
            AsyncQueueElement<T1, T2> tmp = new AsyncQueueElement<T1, T2>
            {
                Dispatcher = _dispatcher,
                Action = action,
                CompleteAction = completeAction,
                Arg1 = arg1,
                Arg2 = arg2
            };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }

        public AsyncQueueElementBase AddSyncOperation<T1, T2, T3>(
            Action<T1, T2, T3> action, Action<AsyncQueueResult> completeAction, T1 arg1, T2 arg2, T3 arg3)
        {
            AsyncQueueElement<T1, T2, T3> tmp = new AsyncQueueElement<T1, T2, T3>
            {
                Dispatcher = _dispatcher,
                Action = action,
                CompleteAction = completeAction,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3
            };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }

        public AsyncQueueElementBase AddSyncOperation<T1, T2, T3, T4>(
            Action<T1, T2, T3, T4> action, Action<AsyncQueueResult> completeAction, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            AsyncQueueElement<T1, T2, T3, T4> tmp = new AsyncQueueElement<T1, T2, T3, T4>
            {
                Dispatcher = _dispatcher,
                Action = action,
                CompleteAction = completeAction,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3,
                Arg4 = arg4
            };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }


        public AsyncQueueElementBase AddAsyncFunction<TRes>(Func<TRes> func, Action<AsyncQueueResult> completeAction)
        {
            AsyncQueueFunc<TRes> tmp = new AsyncQueueFunc<TRes> { Func = func, CompleteAction = completeAction };
            _operationQueue.Enqueue(tmp);

            return tmp;
        }

        public AsyncQueueElementBase AddAsyncFunction<T, TRes>(Func<T, TRes> func, Action<AsyncQueueResult> completeAction, T arg)
        {
            AsyncQueueFunc<T, TRes> tmp = new AsyncQueueFunc<T, TRes> { Func = func, CompleteAction = completeAction, Arg1 = arg };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }

        public AsyncQueueElementBase AddAsyncFunction<T1, T2, TRes>(Func<T1, T2, TRes> func, Action<AsyncQueueResult> completeAction, T1 arg1, T2 arg2)
        {
            AsyncQueueFunc<T1, T2, TRes> tmp = new AsyncQueueFunc<T1, T2, TRes>
            {
                Func = func,
                CompleteAction = completeAction,
                Arg1 = arg1,
                Arg2 = arg2
            };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }

        public AsyncQueueElementBase AddAsyncFunction<T1, T2, T3, TRes>(
            Func<T1, T2, T3, TRes> func, Action<AsyncQueueResult> completeAction, T1 arg1, T2 arg2, T3 arg3)
        {
            AsyncQueueFunc<T1, T2, T3, TRes> tmp = new AsyncQueueFunc<T1, T2, T3, TRes>
            {
                Func = func,
                CompleteAction = completeAction,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3
            };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }

        public AsyncQueueElementBase AddAsyncFunction<T1, T2, T3, T4, TRes>(
            Func<T1, T2, T3, T4, TRes> func, Action<AsyncQueueResult> completeAction, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            AsyncQueueFunc<T1, T2, T3, T4, TRes> tmp = new AsyncQueueFunc<T1, T2, T3, T4, TRes>
            {
                Func = func,
                CompleteAction = completeAction,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3,
                Arg4 = arg4
            };

            _operationQueue.Enqueue(tmp);

            return tmp;
        }




        private void CheckFinalizer()
        {
            if (Finalizer != null)
                throw new InvalidOperationException("Метод Finalizer для задачи уже задан");
        }

        public void AddAsyncFinalizer(Action action)
        {
            CheckFinalizer();
            Finalizer = new AsyncQueueElement { Action = action };
        }

        public void AddAsyncFinalizer<T>(Action<T> action, T arg)
        {
            CheckFinalizer();
            Finalizer = new AsyncQueueElement<T> { Action = action, Arg1 = arg };
        }

        public void AddAsyncFinalizer<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            CheckFinalizer();
            Finalizer = new AsyncQueueElement<T1, T2>
            {
                Action = action,
                Arg1 = arg1,
                Arg2 = arg2
            };
        }

        public void AddAsyncFinalizer<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            CheckFinalizer();
            Finalizer = new AsyncQueueElement<T1, T2, T3>
            {
                Action = action,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3
            };
        }

        public void AddAsyncFinalizer<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            CheckFinalizer();
            Finalizer = new AsyncQueueElement<T1, T2, T3, T4>
            {
                Action = action,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3,
                Arg4 = arg4
            };
        }

        public void AddSyncFinalizer(Action action)
        {
            CheckFinalizer();
            Finalizer = new AsyncQueueElement
            {
                Dispatcher = _dispatcher,
                Action = action
            };
        }

        public void AddSyncFinalizer<T>(Action<T> action, T arg)
        {
            CheckFinalizer();
            Finalizer = new AsyncQueueElement<T>
            {
                Dispatcher = _dispatcher,
                Action = action,
                Arg1 = arg
            };
        }

        public void AddSyncFinalizer<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            CheckFinalizer();
            Finalizer = new AsyncQueueElement<T1, T2>
            {
                Dispatcher = _dispatcher,
                Action = action,
                Arg1 = arg1,
                Arg2 = arg2
            };
        }

        public void AddSyncFinalizer<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            CheckFinalizer();
            Finalizer = new AsyncQueueElement<T1, T2, T3>
            {
                Dispatcher = _dispatcher,
                Action = action,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3
            };
        }

        public void AddSyncFinalizer<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            CheckFinalizer();
            Finalizer = new AsyncQueueElement<T1, T2, T3, T4>
            {
                Dispatcher = _dispatcher,
                Action = action,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3,
                Arg4 = arg4
            };
        }


        private void CheckBeginAction()
        {
            if (BeginAction != null)
                throw new InvalidOperationException("Метод BeginAction для задачи уже задан");
        }

        public void AddAsyncBeginAction(Action action)
        {
            CheckBeginAction();
            BeginAction = new AsyncQueueElement { Action = action };
        }

        public void AddAsyncBeginAction<T>(Action<T> action, T arg)
        {
            CheckBeginAction();
            BeginAction = new AsyncQueueElement<T> { Action = action, Arg1 = arg };
        }

        public void AddAsyncBeginAction<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            CheckBeginAction();
            BeginAction = new AsyncQueueElement<T1, T2>
            {
                Action = action,
                Arg1 = arg1,
                Arg2 = arg2
            };
        }

        public void AddAsyncBeginAction<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            CheckBeginAction();
            BeginAction = new AsyncQueueElement<T1, T2, T3>
            {
                Action = action,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3
            };
        }

        public void AddAsyncBeginAction<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            CheckBeginAction();
            BeginAction = new AsyncQueueElement<T1, T2, T3, T4>
            {
                Action = action,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3,
                Arg4 = arg4
            };
        }

        public void AddSyncBeginAction(Action action)
        {
            CheckBeginAction();
            BeginAction = new AsyncQueueElement
            {
                Dispatcher = _dispatcher,
                Action = action
            };
        }

        public void AddSyncBeginAction<T>(Action<T> action, T arg)
        {
            CheckBeginAction();
            BeginAction = new AsyncQueueElement<T>
            {
                Dispatcher = _dispatcher,
                Action = action,
                Arg1 = arg
            };
        }

        public void AddSyncBeginAction<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            CheckBeginAction();
            BeginAction = new AsyncQueueElement<T1, T2>
            {
                Dispatcher = _dispatcher,
                Action = action,
                Arg1 = arg1,
                Arg2 = arg2
            };
        }

        public void AddSyncBeginAction<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            CheckBeginAction();
            BeginAction = new AsyncQueueElement<T1, T2, T3>
            {
                Dispatcher = _dispatcher,
                Action = action,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3
            };
        }

        public void AddSyncBeginAction<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            CheckBeginAction();
            BeginAction = new AsyncQueueElement<T1, T2, T3, T4>
            {
                Dispatcher = _dispatcher,
                Action = action,
                Arg1 = arg1,
                Arg2 = arg2,
                Arg3 = arg3,
                Arg4 = arg4
            };
        }
    }
}
