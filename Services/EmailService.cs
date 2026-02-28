using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using RefWeb.Models;

namespace RefWeb.Services
{
    public class EmailService : IEmailService, IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger,
            IHostEnvironment hostEnvironment)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        // ── IEmailSender (ASP.NET Identity) ──────────────────────────────
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // 1. Correo de Confirmación / Bienvenida
            if (subject.Contains("confirm", StringComparison.OrdinalIgnoreCase) ||
                subject.Contains("confirma", StringComparison.OrdinalIgnoreCase) ||
                subject.Contains("cuenta", StringComparison.OrdinalIgnoreCase) ||
                subject.Contains("bienvenido", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string tplPath = Path.Combine(_hostEnvironment.ContentRootPath, "Templates", "Bienvenida.html");
                    if (File.Exists(tplPath))
                    {
                        string body = (await File.ReadAllTextAsync(tplPath))
                            .Replace("{Nombre}", email.Split('@')[0])
                            .Replace("{UrlLogin}", htmlMessage)
                            .Replace("{Year}", DateTime.Now.Year.ToString());

                        await SendEmailAsync(email, "¡Bienvenido! Confirma tu cuenta", body, true);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar plantilla de bienvenida");
                }
            }

            // 2. Correo de Recuperación de Contraseña
            if (subject.Contains("reset", StringComparison.OrdinalIgnoreCase) ||
                subject.Contains("contraseña", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string tplPath = Path.Combine(_hostEnvironment.ContentRootPath, "Templates", "RecuperarPassword.html");
                    if (File.Exists(tplPath))
                    {
                        string body = (await File.ReadAllTextAsync(tplPath))
                            .Replace("{Nombre}", email.Split('@')[0])
                            .Replace("{UrlReset}", htmlMessage)
                            .Replace("{Year}", DateTime.Now.Year.ToString());

                        await SendEmailAsync(email, "Restablece tu contraseña", body, true);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar plantilla de recuperación");
                }
            }

            // Fallback — enviar el mensaje tal cual
            await SendEmailAsync(email, subject, htmlMessage, true);
        }

        // ── IEmailService (general) ───────────────────────────────────────
        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(_emailSettings.From));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                var builder = new BodyBuilder();
                if (isHtml)
                    builder.HtmlBody = body;
                else
                    builder.TextBody = body;

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();

                // Resend usa puerto 465 con SSL/TLS implícito
                int port = _emailSettings.Port > 0 ? _emailSettings.Port : 465;
                var secureSocketOptions = port == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await client.ConnectAsync(_emailSettings.Host, port, secureSocketOptions);
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email enviado exitosamente a {To} | Asunto: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email a {To} | Asunto: {Subject}", to, subject);
            }
        }
    }
}
