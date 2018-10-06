using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace DevBots.Shared
{
    public static class ExtensionMethods
    {
        public static string ToHash(this string input)
        {
            var crypt = new SHA256Managed();
            var hash = string.Empty;
            var crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(input));
            return crypto.Aggregate(hash, (current, theByte) => current + theByte.ToString("x2"));
        }

        public static void SendEmail(string email, string username)
        {
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential("noreply.dev.bots@gmail.com", "devBotsPasss");
            client.EnableSsl = true;

            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("noreply.dev.bots@gmail.com", "DevBots noreply");
            mailMessage.To.Add(email);
            mailMessage.Body =
                $"<h3>Confirm email</h3>Hi {username},<br />this email was registered on DevBots.pl, an online game about programming and robot fights! <br /><br /> Please confirm that this is your email! <br /> <a href=\"test.pl\">Click here to confirm and activate your account</a>. <br /><br /> If you don't tried to register on this website, then just ignore this email.<br />Thank you,<br /> DevBots automatic response";
            mailMessage.Subject = "Account activation - DevBots";
            mailMessage.IsBodyHtml = true;
            client.Send(mailMessage);

            //var smtpClient = new SmtpClient("smtp.gmail.com", 465)
            //{
            //    Credentials = new System.Net.NetworkCredential("noreply.dev.bots@gmail.com", "devBotsPasss"),
            //    UseDefaultCredentials = true,
            //    DeliveryMethod = SmtpDeliveryMethod.Network,
            //    EnableSsl = true
            //};

            //var mail = new MailMessage
            //{
            //    From = new MailAddress("noreply.dev.bots@gmail.com", "DevBots noreply"),
            //    Subject = "Account activation - DevBots",
            //    IsBodyHtml = true,
            //    Body =
            //        $"<h3>Confirm email</h3><br />Hi {username},<be />this email was registered on DevBots.pl, an online game about programming and robot fights! <br /><br /> Please confirm that this is your email! <br /> <a href=\"\">Click here to confirm and activate your account.</a> <br /><br /> If you don't tried to register on this website, then just ignore this email.<br />Thank you, DevBots automatic response"
            //};
            //mail.To.Add(new MailAddress(email));

            //smtpClient.SendMailAsync(mail);
        }
    }
}
