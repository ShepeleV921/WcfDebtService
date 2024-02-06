using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Tools.Rosreestr
{
    public class AsyncTaskWorker : IDisposable
    {
        private readonly BlockingCollection<AsyncTaskItemBase> _consumerProducerCollection;
        private readonly List<Thread> _threads = new List<Thread>();

        private volatile int _count;
        public int Count { get { return Thread.VolatileRead(ref _count); } }

        public bool CancellationPending { get; set; }


        public AsyncTaskWorker(int maxCapacity)
        {
            if (maxCapacity < 2)
                throw new ArgumentOutOfRangeException("maxCapacity");

            _consumerProducerCollection = new BlockingCollection<AsyncTaskItemBase>(maxCapacity);

            int processorCount = Environment.ProcessorCount;
            if (processorCount >= 4)
                processorCount -= 2;

            for (int i = 0; i < processorCount; ++i)
                _threads.Add(new Thread(ListenQueue) { Name = "AsyncTaskThread_" + (i + 1), IsBackground = true });

            for (int i = 0; i < processorCount; ++i)
                _threads[i].Start();
        }

        public AsyncTaskWorker(int maxCapacity, int threads)
        {
            if (maxCapacity < 1)
                throw new ArgumentOutOfRangeException("maxCapacity");

            _consumerProducerCollection = new BlockingCollection<AsyncTaskItemBase>(maxCapacity);

            int processorCount = threads > 0 ? threads : 1;

            for (int i = 0; i < processorCount; ++i)
                _threads.Add(new Thread(ListenQueue) { Name = "AsyncTaskThread_" + (i + 1), IsBackground = true });

            for (int i = 0; i < processorCount; ++i)
                _threads[i].Start();
        }

        private void ListenQueue()
        {
            foreach (AsyncTaskItemBase item in _consumerProducerCollection.GetConsumingEnumerable())
            {
                if (CancellationPending)
                {
                    Interlocked.Decrement(ref _count);
                    break;
                }

                try
                {
                    using (item)
                    {
                        item.Execute();
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref _count);
                }
            }
        }

        public void AddTask(AsyncTaskItemBase task)
        {
            if (CancellationPending)
                return;

            Interlocked.Increment(ref _count);

            try
            {
                _consumerProducerCollection.Add(task);
            }
            catch
            {
                Interlocked.Decrement(ref _count);
                throw;
            }
        }

        public void Complete(int millisecondsTimeout = 10000)
        {
            if (_consumerProducerCollection?.IsAddingCompleted == false)
                _consumerProducerCollection.CompleteAdding();

            for (int i = 0; i < _threads.Count; i++)
                _threads[i].Join(millisecondsTimeout);
        }

        public void Dispose()
        {
            if (_consumerProducerCollection?.IsCompleted == false)
                Complete();

            for (int i = 0; i < _threads.Count; i++)
            {
                try
                {
                    if (_threads[i].IsAlive)
                        _threads[i].Interrupt();
                }
                catch
                {
                    try
                    {
                        _threads[i].Abort();
                    }
                    catch
                    {
                        ;
                    }
                }
            }

            _threads.Clear();

            _consumerProducerCollection?.Dispose();
        }
    }
}
