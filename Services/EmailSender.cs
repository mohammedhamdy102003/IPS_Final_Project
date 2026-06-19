using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
namespace IPS_PROJECT.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        // بنعمل حقن للـ Configuration عشان نقرأ البيانات من appsettings.json
        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // 1. قراءة الإعدادات
            string smtpHost = _configuration["EmailSettings:Host"];
            int smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
            string fromEmail = _configuration["EmailSettings:FromEmail"];
            string password = _configuration["EmailSettings:Password"];

            // 2. تجهيز عميل الـ SMTP
            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.Credentials = new NetworkCredential(fromEmail, password);
                client.EnableSsl = true; // ضروري لـ Gmail

                // 3. تجهيز الرسالة
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "IPS TEAM"), // الاسم اللي هيظهر للمستخدم
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true // عشان الرابط يظهر كـ Link مش نص
                };

                mailMessage.To.Add(email);

                // 4. الإرسال
                try
                {
                    await client.SendMailAsync(mailMessage);
                }
                catch (Exception ex)
                {
                    // لو حصل خطأ (مثل الباسورد غلط أو النت فاصل)
                    // ممكن نسجله في الـ Console مؤقتاً
                    Console.WriteLine($"Error sending email: {ex.Message}");
                    throw; // نعيد رمي الخطأ عشان السيستم يعرف إن الإرسال فشل
                }
            }
        }

    }
}
