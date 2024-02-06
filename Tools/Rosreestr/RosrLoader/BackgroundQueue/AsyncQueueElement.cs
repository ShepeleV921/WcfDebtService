using System;
using System.Windows.Threading;

namespace Tools.Rosreestr
{
    public abstract class AsyncQueueElementBase
    {
        public Exception Exception { get; set; }

        public string ElementDescription { get; set; }

        public Action<AsyncQueueResult> CompleteAction { get; set; }

        public Dispatcher Dispatcher { get; set; }


        public abstract object InvokeAction();

        public abstract object[] GetArgs();
    }

    internal class AsyncQueueElement : AsyncQueueElementBase
    {
        public Action Action { get; set; }

        public override object InvokeAction()
        {
            if (Dispatcher != null)
            {
                Dispatcher.Invoke(Action, null);
            }
            else
            {
                Action();
            }

            return null;
        }

        public override object[] GetArgs()
        {
            return new object[0];
        }
    }

    internal class AsyncQueueElement<T1> : AsyncQueueElementBase
    {
        public Action<T1> Action { get; set; }

        public T1 Arg1 { get; set; }


        public override object InvokeAction()
        {
            if (Dispatcher != null)
            {
                Dispatcher.Invoke(Action, Arg1);
            }
            else
            {
                Action(Arg1);
            }

            return null;
        }

        public override object[] GetArgs()
        {
            return new object[] { Arg1 };
        }
    }

    internal class AsyncQueueElement<T1, T2> : AsyncQueueElementBase
    {
        public Action<T1, T2> Action { get; set; }

        public T1 Arg1 { get; set; }

        public T2 Arg2 { get; set; }


        public override object InvokeAction()
        {
            if (Dispatcher != null)
            {
                Dispatcher.Invoke(Action, Arg1, Arg2);
            }
            else
            {
                Action(Arg1, Arg2);
            }

            return null;
        }

        public override object[] GetArgs()
        {
            return new object[] { Arg1, Arg2 };
        }
    }

    internal class AsyncQueueElement<T1, T2, T3> : AsyncQueueElementBase
    {
        public Action<T1, T2, T3> Action { get; set; }

        public T1 Arg1 { get; set; }

        public T2 Arg2 { get; set; }

        public T3 Arg3 { get; set; }


        public override object InvokeAction()
        {
            if (Dispatcher != null)
            {
                Dispatcher.Invoke(Action, Arg1, Arg2, Arg3);
            }
            else
            {
                Action(Arg1, Arg2, Arg3);
            }

            return null;
        }

        public override object[] GetArgs()
        {
            return new object[] { Arg1, Arg2, Arg3 };
        }
    }

    internal class AsyncQueueElement<T1, T2, T3, T4> : AsyncQueueElementBase
    {
        public Action<T1, T2, T3, T4> Action { get; set; }

        public T1 Arg1 { get; set; }

        public T2 Arg2 { get; set; }

        public T3 Arg3 { get; set; }

        public T4 Arg4 { get; set; }


        public override object InvokeAction()
        {
            if (Dispatcher != null)
            {
                Dispatcher.Invoke(Action, Arg1, Arg2, Arg3, Arg4);
            }
            else
            {
                Action(Arg1, Arg2, Arg3, Arg4);
            }

            return null;
        }

        public override object[] GetArgs()
        {
            return new object[] { Arg1, Arg2, Arg3, Arg4 };
        }
    }

    internal class AsyncQueueElement<T1, T2, T3, T4, T5> : AsyncQueueElementBase
    {
        public Action<T1, T2, T3, T4, T5> Action { get; set; }

        public T1 Arg1 { get; set; }

        public T2 Arg2 { get; set; }

        public T3 Arg3 { get; set; }

        public T4 Arg4 { get; set; }

        public T5 Arg5 { get; set; }


        public override object InvokeAction()
        {
            if (Dispatcher != null)
            {
                Dispatcher.Invoke(Action, Arg1, Arg2, Arg3, Arg4, Arg5);
            }
            else
            {
                Action(Arg1, Arg2, Arg3, Arg4, Arg5);
            }

            return null;
        }

        public override object[] GetArgs()
        {
            return new object[] { Arg1, Arg2, Arg3, Arg4, Arg5 };
        }
    }


    internal class AsyncQueueFunc<TRes> : AsyncQueueElementBase
    {
        public Func<TRes> Func { get; set; }

        public override object InvokeAction()
        {
            if (Dispatcher != null)
            {
                return Dispatcher.Invoke(Func, null);
            }
            else
            {
                return Func();
            }
        }

        public override object[] GetArgs()
        {
            return new object[0];
        }
    }

    internal class AsyncQueueFunc<T1, TRes> : AsyncQueueElementBase
    {
        public Func<T1, TRes> Func { get; set; }

        public T1 Arg1 { get; set; }


        public override object InvokeAction()
        {
            if (Dispatcher != null)
            {
                return Dispatcher.Invoke(Func, Arg1);
            }
            else
            {
                return Func(Arg1);
            }
        }

        public override object[] GetArgs()
        {
            return new object[] { Arg1 };
        }
    }

    internal class AsyncQueueFunc<T1, T2, TRes> : AsyncQueueElementBase
    {
        public Func<T1, T2, TRes> Func { get; set; }

        public T1 Arg1 { get; set; }

        public T2 Arg2 { get; set; }


        public override object InvokeAction()
        {
            if (Dispatcher != null)
            {
                return Dispatcher.Invoke(Func, Arg1, Arg2);
            }
            else
            {
                return Func(Arg1, Arg2);
            }
        }

        public override object[] GetArgs()
        {
            return new object[] { Arg1, Arg2 };
        }
    }

    internal class AsyncQueueFunc<T1, T2, T3, TRes> : AsyncQueueElementBase
    {
        public Func<T1, T2, T3, TRes> Func { get; set; }

        public T1 Arg1 { get; set; }

        public T2 Arg2 { get; set; }

        public T3 Arg3 { get; set; }


        public override object InvokeAction()
        {
            if (Dispatcher != null)
            {
                return Dispatcher.Invoke(Func, Arg1, Arg2, Arg3);
            }
            else
            {
                return Func(Arg1, Arg2, Arg3);
            }
        }

        public override object[] GetArgs()
        {
            return new object[] { Arg1, Arg2, Arg3 };
        }
    }

    internal class AsyncQueueFunc<T1, T2, T3, T4, TRes> : AsyncQueueElementBase
    {
        public Func<T1, T2, T3, T4, TRes> Func { get; set; }

        public T1 Arg1 { get; set; }

        public T2 Arg2 { get; set; }

        public T3 Arg3 { get; set; }

        public T4 Arg4 { get; set; }


        public override object InvokeAction()
        {
            if (Dispatcher != null)
            {
                return Dispatcher.Invoke(Func, Arg1, Arg2, Arg3, Arg4);
            }
            else
            {
                return Func(Arg1, Arg2, Arg3, Arg4);
            }
        }

        public override object[] GetArgs()
        {
            return new object[] { Arg1, Arg2, Arg3, Arg4 };
        }
    }
}
