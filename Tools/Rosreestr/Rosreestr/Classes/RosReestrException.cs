using System;
using System.Runtime.Serialization;

namespace Tools.Rosreestr
{
    public class RosReestrException : Exception
    {
        public bool RealEstateFormPIDs { get; set; }


        public RosReestrException()
            : base()
        {

        }

        public RosReestrException(string message)
            : base(message)
        {
        }

        protected RosReestrException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public RosReestrException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
