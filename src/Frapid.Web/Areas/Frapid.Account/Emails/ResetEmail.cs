using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Frapid.Account.DTO;
using Frapid.ApplicationState.Cache;
using Frapid.Messaging;
using Frapid.Messaging.DTO;

namespace Frapid.Account.Emails
{
    public class ResetEmail
    {
        private readonly Reset _resetDetails;

        public ResetEmail(Reset reset)
        {
            this._resetDetails = reset;
        }

        private string GetTemplate()
        {
            string path = "~/Catalogs/{catalog}/Areas/Frapid.Account/EmailTemplates/password-reset.html";
            path = path.Replace("{catalog}", AppUsers.GetCatalog());
            path = HostingEnvironment.MapPath(path);

            if (!File.Exists(path))
            {
                return string.Empty;
            }

            return path != null ? File.ReadAllText(path, Encoding.UTF8) : string.Empty;
        }

        private string ParseTemplate(string template)
        {
            string siteUrl = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
            string link = siteUrl + "/account/reset/confirm?token=" + this._resetDetails.RequestId;

            string parsed = template.Replace("{{Name}}", this._resetDetails.Name);
            parsed = parsed.Replace("{{EmailAddress}}", this._resetDetails.Email);
            parsed = parsed.Replace("{{ResetLink}}", link);
            parsed = parsed.Replace("{{SiteUrl}}", siteUrl);

            return parsed;
        }

        private EmailQueue GetEmail(Reset model, string subject, string message)
        {
            return new EmailQueue
            {
                AddedOn = DateTime.Now,
                FromName = model.Name,
                Subject = subject,
                Message = message,
                SendTo = model.Email
            };
        }

        public async Task SendAsync()
        {
            string template = this.GetTemplate();
            string parsed = this.ParseTemplate(template);
            string subject = "Your Password Reset Link for " + HttpContext.Current.Request.Url.Authority;

            string catalog = AppUsers.GetCatalog();
            var email = this.GetEmail(this._resetDetails, subject, parsed);

            var processor = EmailProcessor.GetDefault(catalog);
            if (string.IsNullOrWhiteSpace(email.ReplyTo))
            {
                email.ReplyTo = processor.Config.FromEmail;
            }

            var queue = new MailQueueManager(catalog, email);
            queue.Add();
            await queue.ProcessMailQueueAsync(processor);
        }
    }
}