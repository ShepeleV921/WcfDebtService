using System;
using System.Net.Mail;

namespace LoadPipeline.MailNotify
{
    public class MailInfo
    {
        public string Recipient { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }

        public MailInfo(string resipient, string subj, string msg)
        {
            Recipient = resipient;
            Subject = subj;
            Message = msg;
        }
    }
}
