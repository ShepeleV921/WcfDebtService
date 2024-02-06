using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace Tools.Rosreestr
{
    /// <summary>
    /// Представляет очередь операций, выполняемых в отдельном потоке
    /// </summary>
    public class BackgroundQueue : IDisposable, INotifyPropertyChanged
    {
        private static readonly BackgroundQueue _backQueue = new BackgroundQueue();
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(true);
        private readonly List<QueueTask> _operationQueue = new List<QueueTask>();
        private readonly BackgroundWorker _backWorker = new BackgroundWorker();
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private bool _pause;

        public event PropertyChangedEventHandler PropertyChanged;

        private int _progressValue;
        public int ProgressValue
        {
            get { return _progressValue; }
            set
            {
                _progressValue = value;
                OnPropertyChanged("ProgressValue");
            }
        }

        private int _progressMax;
        public int ProgressMax
        {
            get { return _progressMax; }
            set
            {
                _progressMax = value;
                OnPropertyChanged("ProgressMax");
            }
        }

        private bool _indeterminate;
        public bool IsIndeterminate
        {
            get { return _indeterminate; }
            set
            {
                _indeterminate = value;
                OnPropertyChanged("IsIndeterminate");
            }
        }

        private object _progressUserState;
        public object ProgressUserState
        {
            get { return _progressUserState; }
            set
            {
                _progressUserState = value;
                _dispatcher.Invoke(new Action<string>(OnPropertyChanged), new object[] { "ProgressUserState" });
            }
        }

        public bool CurrentOperationCancellationPending { get; private set; }

        public bool AllOperationsCancellationPending { get; private set; }

        public bool IsWorking { get { return _backWorker.IsBusy; } }

        public bool IsPaused
        {
            get { return _pause; }
            set
            {
                _pause = value;
                if (_pause)
                {
                    _resetEvent.Reset();
                }
                else
                {
                    _resetEvent.Set();
                }

                OnPropertyChanged("IsPaused");
            }
        }

        public QueueTask CurrentTask { get; private set; }

        public static Dispatcher Dispatcher { get { return Instance._dispatcher; } }

        public static BackgroundQueue Instance { get { return _backQueue; } }


        public BackgroundQueue()
        {
            _backWorker.WorkerReportsProgress = true;
            _backWorker.WorkerSupportsCancellation = true;

            _backWorker.DoWork += BackWorker_DoWork;
            _backWorker.ProgressChanged += BackWorker_ProgressChanged;
            _backWorker.RunWorkerCompleted += BackWorker_RunWorkerCompleted;
        }


        public void Start()
        {
            IsPaused = false;

            if (!_backWorker.IsBusy)
                _backWorker.RunWorkerAsync();

            OnPropertyChanged("IsWorking");
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public QueueTask CreateTask()
        {
            return new QueueTask(_dispatcher);
        }

        public QueueTask CreateTask(string description)
        {
            return new QueueTask(_dispatcher) { Description = description };
        }

        public void AddTask(QueueTask task)
        {
            _operationQueue.Add(task);
        }

        private void ResetProgressValues(int maxValue)
        {
            ProgressValue = 0;
            IsIndeterminate = maxValue == 1;
            ProgressMax = maxValue;
        }

        private void BackWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ProgressValue = 0;
            ProgressUserState = null;

            CurrentOperationCancellationPending = false;
            AllOperationsCancellationPending = false;

            OnPropertyChanged("IsWorking");
        }

        private void BackWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressValue = e.ProgressPercentage;
            ProgressUserState = e.UserState;
        }

        private void BackWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Пока очередь задач не пуста
            while (_operationQueue.Count > 0)
            {
                QueueTask task = CurrentTask = _operationQueue[0];
                _operationQueue.RemoveAt(0);

                if (AllOperationsCancellationPending)
                {
                    _operationQueue.Clear();
                    break;
                }

                try
                {
                    try
                    {
                        if (task.BeginAction != null)
                        {
                            _dispatcher.Invoke(new Action<int>(ResetProgressValues), new object[] { 1 });
                            _backWorker.ReportProgress(0, task.Description + task.BeginAction.ElementDescription);
                            task.BeginAction.InvokeAction();
                        }
                    }
                    catch (Exception exc)
                    {
                        _dispatcher.Invoke(new Action<Exception>(TaskError), exc);
                        task.Clear();
                        break;
                    }

                    // Обновление данных прогресс бара
                    _dispatcher.Invoke(new Action<int>(ResetProgressValues), new object[] { task.ElementCount });

                    ExecuteTaskQueue(task);
                }
                finally
                {
                    if (task.Finalizer != null)
                    {
                        _dispatcher.Invoke(new Action<int>(ResetProgressValues), new object[] { 1 });
                        _backWorker.ReportProgress(task.ElementCount, task.Description + task.Finalizer.ElementDescription);
                        task.Finalizer.InvokeAction();
                    }
                }

                if (task.DependentTask != null)
                {
                    _operationQueue.Insert(0, task.DependentTask);
                    task.DependentTask = null;
                }
            }
        }

        private void ExecuteTaskQueue(QueueTask task)
        {
            int itemNumber = 0;
            while (task.ElementCount > 0)
            {
                AsyncQueueElementBase element = null;
                object result = null;

                try
                {
                    try
                    {
                        if (IsPaused)
                        {
                            _resetEvent.WaitOne(Timeout.Infinite);
                        }

                        if (CurrentOperationCancellationPending)
                        {
                            task.Clear();
                            break;
                        }

                        element = task.GetElement();
                        _backWorker.ReportProgress(++itemNumber, task.Description + element.ElementDescription);

                        result = element.InvokeAction();
                    }
                    catch (Exception exc)
                    {
                        if (element != null)
                            element.Exception = exc;

                        bool res = (bool)_dispatcher.Invoke(new Func<Exception, bool>(ShowError), exc);
                        if (!res)
                        {
                            task.Clear();
                            break;
                        }
                    }
                }
                finally
                {
                    if (element != null && element.CompleteAction != null)
                    {
                        _dispatcher.Invoke(element.CompleteAction,
                            new AsyncQueueResult
                            {
                                Exception = element.Exception,
                                ActionResult = result,
                                Args = element.GetArgs()
                            });
                    }
                }
            }
        }

        private bool ShowError(Exception exc)
        {
            //IMessageDialog dialog = IoC.Resolve<IMessageDialog>();

            //if (dialog.Show(exc.Message, "Ошибка!", DialogTypes.Error,
            //    DialogAnswers.CustomCustom, "  Продолжить  ", "  Прервать  ") == DialogResult.Custom2)
            //    return false;

            return true;
        }

        private void TaskError(Exception exc)
        {
            throw new NotImplementedException();
        }

        public void CancelCurrentOperation()
        {
            CurrentOperationCancellationPending = true;
        }

        public void CancelAllOperations()
        {
            AllOperationsCancellationPending = true;
            CancelCurrentOperation();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var tmp = PropertyChanged;
            if (tmp != null)
                tmp(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (_resetEvent != null)
            {
                _resetEvent.Dispose();
            }

            if (_backWorker != null)
            {
                _backWorker.Dispose();
            }
        }
    }
}
