using System;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Pangolin
{
    public static class MailLayer
    {
        private static async Task<bool> SendMailAsync(string subject, string body, MailAddress from, MailAddress[] to, MailAddress[] cc, MailAddress[] bcc, Uri[] attachmentFiles, bool isBodyHtml = true, SmtpDeliveryMethod smtpDeliveryMethod = SmtpDeliveryMethod.PickupDirectoryFromIis, MailPriority mailPriority = MailPriority.Normal)
        {
            if (subject == null) { throw new ArgumentNullException(nameof(subject)); }
            if (body == null) { throw new ArgumentNullException(nameof(body)); }

            bool isSuccess = false;

            try
            {
                using (MailMessage mailMessage = new MailMessage()
                {
                    Subject = subject,
                    SubjectEncoding = ConfigurationLayer.DefaultEncoding,
                    From = from,
                    Body = body,
                    BodyEncoding = ConfigurationLayer.DefaultEncoding,
                    BodyTransferEncoding = TransferEncoding.QuotedPrintable,
                    IsBodyHtml = isBodyHtml,
                    DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure,
                    Priority = mailPriority,
                    Sender = new MailAddress(ConfigurationLayer.DeveloperMailFrom)
                })
                {
                    for (int i = default; i < to?.Length; i++)
                    {
                        mailMessage.To.Add(to[i]);
                    }

                    for (int i = default; i < cc?.Length; i++)
                    {
                        mailMessage.To.Add(cc[i]);
                    }

                    for (int i = default; i < bcc?.Length; i++)
                    {
                        mailMessage.To.Add(bcc[i]);
                    }

                    for (int i = default; i < attachmentFiles?.Length; i++)
                    {
                        mailMessage.Attachments.Add(new Attachment(attachmentFiles[i].AbsolutePath));
                    }

                    SmtpClient smtpClient = null;
                    switch (smtpDeliveryMethod)
                    {
                        case SmtpDeliveryMethod.PickupDirectoryFromIis:
                            using (smtpClient = new SmtpClient())
                            {
                                smtpClient.DeliveryMethod = smtpDeliveryMethod;
                                await smtpClient.SendMailAsync(mailMessage);
                                isSuccess = true;
                            }
                            break;
                        case SmtpDeliveryMethod.Network:
                            using (smtpClient = new SmtpClient())
                            {
                                smtpClient.DeliveryMethod = smtpDeliveryMethod;
                                await smtpClient.SendMailAsync(mailMessage);
                                isSuccess = true;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (ArgumentNullException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (ArgumentException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (FormatException exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }
            catch (Exception exc) { await ExceptionLayer.CoreHandleAsync(exc); throw; }

            return isSuccess;
        }

        private static async Task<bool> SendMailAsync(string subject, string body, string from, string[] to, string[] cc, string[] bcc, string[] attachmentFiles, bool isBodyHtml = true, SmtpDeliveryMethod smtpDeliveryMethod = SmtpDeliveryMethod.PickupDirectoryFromIis, MailPriority mailPriority = MailPriority.Normal)
        {
            if (subject == null) { throw new ArgumentNullException(nameof(subject)); }
            if (body == null) { throw new ArgumentNullException(nameof(body)); }
            if (from == null) { throw new ArgumentNullException(nameof(from)); }
            if (to == null) { throw new ArgumentNullException(nameof(to)); }
            else if (to.Length < 1) { throw new ArgumentException($"{nameof(to)} contains no email addresses.", nameof(to)); }

            MailAddress fromAddress = new MailAddress(from);

            MailAddress[] toAddresses = new MailAddress[to.Length];
            for (int i = default; i < to.Length; i++)
            {
                toAddresses[i] = new MailAddress(to[i]);
            }

            MailAddress[] ccAddresses = null;
            if (cc?.Length > 0)
            {
                ccAddresses = new MailAddress[cc.Length];

                for (int i = default; i < cc.Length; i++)
                {
                    ccAddresses[i] = new MailAddress(cc[i]);
                }
            }

            MailAddress[] bccAddresses = null;
            if (bcc?.Length > 0)
            {
                bccAddresses = new MailAddress[bcc.Length];

                for (int i = default; i < bcc.Length; i++)
                {
                    bccAddresses[i] = new MailAddress(bcc[i]);
                }
            }

            Uri[] uris = null;
            if (attachmentFiles?.Length > 0)
            {
                uris = new Uri[attachmentFiles.Length];
                for (int i = default; i < attachmentFiles.Length; i++)
                {
                    if (Uri.TryCreate(attachmentFiles[i], UriKind.Absolute, out Uri uri) && uri.IsFile)
                    {
                        uris[i] = uri;
                    }
                }
            }

            return await SendMailAsync(subject, body, fromAddress, toAddresses, ccAddresses, bccAddresses, uris, isBodyHtml, smtpDeliveryMethod, mailPriority);
        }

        public static async Task<bool> SendCoreMailAsync(string subject, string body, string[] attachmentFiles = null)
        {
            if (string.IsNullOrWhiteSpace(ConfigurationLayer.DeveloperMailFrom) || ConfigurationLayer.DeveloperMailTo?.Length > 0)
            {
                throw new InvalidOperationException("Developer Mail Configuration settings missing");
            }
            
            return await SendMailAsync(subject, body, ConfigurationLayer.DeveloperMailFrom, ConfigurationLayer.DeveloperMailTo, ConfigurationLayer.DeveloperMailCC, ConfigurationLayer.DeveloperMailBCC, attachmentFiles, true, SmtpDeliveryMethod.Network, MailPriority.High);
        }

        public static async Task<bool> SendMailIisAsync(string subject, string body, string from, string[] to, string[] cc = null, string[] bcc = null, string[] attachmentFiles = null, bool isBodyHtml = true)
        {
            return await SendMailAsync(subject, body, from, to, cc, bcc, attachmentFiles, isBodyHtml, SmtpDeliveryMethod.PickupDirectoryFromIis); ;
        }

        public static async Task<bool> SendMailSmtpAsync(string subject, string body, string from, string[] to, string[] cc = null, string[] bcc = null, string[] attachmentFiles = null, bool isBodyHtml = true)
        {
            return await SendMailAsync(subject, body, from, to, cc, bcc, attachmentFiles, isBodyHtml, SmtpDeliveryMethod.Network); ;
        }
    }
}
