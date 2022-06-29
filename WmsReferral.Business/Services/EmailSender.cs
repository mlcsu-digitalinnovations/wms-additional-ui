using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WmsReferral.Business.Services
{
    public class EmailSender : IEmailSender
    {
        public EmailSender(IOptions<AuthMessageSenderOptions> optionsAccessor)
        {
            Options = optionsAccessor.Value;
        }

        public AuthMessageSenderOptions Options { get; } //set only via Secret Manager

        public Task SendEmailAsync(string email, string subject, string htmlMessage, string message)
        {
            return Execute(Options.SendGridKey, subject, htmlMessage, message, email, Options.SendGridFromEmailAddress, Options.SendGridCategory);
        }

        public Task Execute(string apiKey, string subject, string htmlMessage, string message, string email, string fromEmail, string category)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(fromEmail, Options.SendGridUser),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = htmlMessage,
                Categories = new List<string>() { "WeightManagementSystem", category }
            };
            msg.AddTo(new EmailAddress(email));
           
            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);
            
            return client.SendEmailAsync(msg);
        }
    }
    public class AuthMessageSenderOptions
    {
        public string SendGridUser { get; set; }
        public string SendGridKey { get; set; }
        public string SendGridFromEmailAddress { get; set; }
        public string SendGridCategory { get; set; }
    }
}

