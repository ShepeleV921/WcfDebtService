﻿using System;

namespace Tools.Rosreestr
{
    public class RosreestrEventArgs : EventArgs
    {
        public string Message { get; }

        public Exception Exception { get; }


        public RosreestrEventArgs(Exception exception, string message)
        {
            Message = message;
            Exception = exception;
        }
    }
}
