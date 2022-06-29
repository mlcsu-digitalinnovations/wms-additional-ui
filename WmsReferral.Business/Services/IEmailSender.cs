using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WmsReferral.Business.Services
{
    public interface IEmailSender
    {
        
        public Task SendEmailAsync(string email, string subject, string htmlMessage, string message);
        public Task Execute(string apiKey, string subject, string htmlMessage, string message, string email, string fromEmail, string category);
    }
}
