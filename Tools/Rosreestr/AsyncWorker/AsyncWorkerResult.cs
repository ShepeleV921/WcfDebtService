using System;

namespace Tools.Rosreestr
{
    public class AsyncWorkerResult<T>
    {
        public T Result { get; set; }

        public Exception Error { get; set; }
    }
}
