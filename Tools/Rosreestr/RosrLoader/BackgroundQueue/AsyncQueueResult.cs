using System;

namespace Tools.Rosreestr
{
    public class AsyncQueueResult
    {
        public Exception Exception { get; internal set; }

        public object[] Args { get; internal set; }

        public object ActionResult { get; internal set; }
    }
}
