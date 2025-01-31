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

        public async Task SendWelcomeNewUserAsync(EmailRequestDTO emailRequest)
        {
            // Create a welcome email
            var welcomeEmail = new EmailDTO
            {
                To = emailRequest.UserEmail,
                Subject = "Welcome to VaccinaCare!",
                Body = $@"
        <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <h1 style='color: #1e1b4b; text-align: center;'>Welcome, {emailRequest.UserName}!</h1>
            <p style='font-size: 16px;'>Thank you for signing up for VaccinaCare, your trusted partner in managing your child's vaccination schedule. We're excited to have you on board!</p>
            <p style='font-size: 16px;'>Here are some of the features you can enjoy:</p>
            <ul style='font-size: 16px; padding-left: 20px;'>
                <li>Track your child's vaccination history</li>
                <li>Receive timely reminders for upcoming vaccines</li>
                <li>Book and manage vaccination appointments with ease</li>
            </ul>
            <p style='font-size: 16px;'>If you have any questions, feel free to reach out to our support team at <a href='mailto:support@vaccinacare.com' style='color: #1e1b4b;'>support@vaccinacare.com</a>.</p>
            <p style='font-size: 16px;'>Best regards,<br>
            <span style='color: #1e1b4b; font-weight: bold;'>The VaccinaCare Team</span></p>
        </div>
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

            if (string.IsNullOrEmpty(emailUserName) || string.IsNullOrEmpty(emailPassword) ||
                string.IsNullOrEmpty(emailHost))
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