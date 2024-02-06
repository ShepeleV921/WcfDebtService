using System;
using System.Net;
using System.Net.Mail;

namespace LoadPipeline.MailNotify
{
    public static class Notifier
    {
        private static readonly MailAddress _addrFrom = new MailAddress("uslugi@rvdk.ru", "Ростовводоканал");

        public static bool Notify(MailInfo minfo, out string error)
        {
            MailAddress addrTo = new MailAddress(minfo.Recipient);

            using (MailMessage mail = new MailMessage(_addrFrom, addrTo))
            {
                mail.Body = minfo.Message;
                mail.IsBodyHtml = false;
                mail.Subject = minfo.Subject;

                using (SmtpClient client = new SmtpClient())
                {
                    client.Host = "172.201.111.222";
                    client.Port = 25;
                    client.EnableSsl = false;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential("uslugi@rvdk.ru", "dFFFSxd44fd!fd");
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    try
                    {
                        client.Send(mail);
                        error = "None";
                        return true;
                    }
                    catch (Exception ex)
                    {
                        error = ex.Message;
                        return false;
                    }
                }
            }
        }



    }
}
