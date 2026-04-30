using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
namespace IPS_PROJECT.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        
        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            
            string smtpHost = _configuration["EmailSettings:Host"];
            int smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
            string fromEmail = _configuration["EmailSettings:FromEmail"];
            string password = _configuration["EmailSettings:Password"];

          
            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.Credentials = new NetworkCredential(fromEmail, password);
                client.EnableSsl = true; 

                
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "IPS TEAM"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true 
                };

                mailMessage.To.Add(email);

              
                try
                {
                    await client.SendMailAsync(mailMessage);
                }
                catch (Exception ex)
                {
                   
                    Console.WriteLine($"Error sending email: {ex.Message}");
                    throw; 
                }
            }
        }

    }
}
