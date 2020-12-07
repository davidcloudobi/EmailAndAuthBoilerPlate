using System;
using System.Collections.Generic;
using System.Text;
using Domain.Helper;
using Domain.Interface;
using Microsoft.Extensions.Options;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using MailKit.Net.Smtp;

namespace Domain.Service
{
    public class EmailService : IEmailService
    {
        private readonly AppSettings _appSettings;

        public EmailService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public void Send(string to, string subject, string html, string @from = null)
        {
            // create message
            var email = new MimeMessage();
            //   email.From.Add(MailboxAddress.Parse(from ?? _appSettings.EmailFrom));
            email.From.Add(new MailboxAddress(from ?? _appSettings.EmailFrom, _appSettings.SmtpUser));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = html };

            // send email
            using var smtp = new SmtpClient();
            smtp.Connect(_appSettings.SmtpHost, _appSettings.SmtpPort, true);
            smtp.AuthenticationMechanisms.Remove("XOAUTH2");
            //smtp.Connect(_appSettings.SmtpHost, _appSettings.SmtpPort, false);
            //  smtp.AuthenticationMechanisms.Remove("XOAUTH2");
            smtp.Authenticate(_appSettings.SmtpUser, _appSettings.SmtpPass);
            smtp.Send(email);
            smtp.Disconnect(true);
        }
    }
}
