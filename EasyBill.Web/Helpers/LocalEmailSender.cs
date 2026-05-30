using Microsoft.AspNetCore.Identity.UI.Services;
using System.IO;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Hosting;

namespace EasyBill.UI.Helpers
{
    public class LocalEmailSender : IEmailSender
    {
        private readonly IWebHostEnvironment _env;

        public LocalEmailSender(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var logMessage = $"[{DateTime.Now}] TO: {email}\nSUBJECT: {subject}\nMESSAGE:\n{htmlMessage}\n-------------------------------------\n";
            
            // 1. Log to Console
            Console.WriteLine("=================== OUTGOING EMAIL ===================");
            Console.WriteLine(logMessage);
            Console.WriteLine("======================================================");

            // 2. Log to a HTML file in wwwroot so users can view it via browser
            try
            {
                var wwwroot = _env.WebRootPath;
                if (!string.IsNullOrEmpty(wwwroot))
                {
                    var filePath = Path.Combine(wwwroot, "sent_emails.html");
                    var htmlEntry = $@"
<div style='border: 1px solid #ccc; padding: 15px; margin: 15px 0; font-family: sans-serif; border-radius: 8px;'>
    <h3>Outgoing Email Log</h3>
    <p><strong>Timestamp:</strong> {DateTime.Now}</p>
    <p><strong>To:</strong> {email}</p>
    <p><strong>Subject:</strong> {subject}</p>
    <div style='border: 1px solid #eee; padding: 10px; background: #fafafa; border-radius: 4px;'>
        {htmlMessage}
    </div>
</div>";
                    await File.AppendAllTextAsync(filePath, htmlEntry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalEmailSender Error] Failed to write sent_emails.html: {ex.Message}");
            }
        }
    }
}
