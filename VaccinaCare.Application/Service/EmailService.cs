using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.EmailDTOs;

namespace VaccinaCare.Application.Service
{
    public class EmailService : IEmailService
    {
        private readonly ILoggerService _logger;

        public EmailService(ILoggerService logger)
        {
            _logger = logger;
        }

        public async Task SendWelcomeNewUserAsync(string userEmail, string userName)
        {
            // Create a welcome email
            var welcomeEmail = new EmailDTO
            {
                To = userEmail,
                Subject = "Welcome to Our System!",
                Body = $@"
                <h1>Welcome, {userName}!</h1>
                <p>Thank you for signing up for our system. We're excited to have you on board.</p>
                <p>If you have any questions, feel free to reach out to our support team.</p>
                <p>Best regards,<br>The Team</p>
            "
            };

            // Send the email
            await SendEmailAsync(welcomeEmail);
        }

        private async Task SendEmailAsync(EmailDTO request)
        {
            var email = new MimeMessage();

            // Read environment variables
            var emailUserName = Environment.GetEnvironmentVariable("EMAIL_USERNAME");
            var emailPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
            var emailHost = Environment.GetEnvironmentVariable("EMAIL_HOST");

            if (string.IsNullOrEmpty(emailUserName) || string.IsNullOrEmpty(emailPassword) || string.IsNullOrEmpty(emailHost))
            {
                throw new InvalidOperationException("Email configuration is missing in environment variables.");
            }

            email.From.Add(MailboxAddress.Parse(emailUserName));
            email.To.Add(MailboxAddress.Parse(request.To));
            email.Subject = request.Subject;
            email.Body = new TextPart(TextFormat.Html)
            {
                Text = request.Body
            };

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(emailHost, 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(emailUserName, emailPassword);
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error sending email: {ex.Message}");
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }



}
