using System.Globalization;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.EmailDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class EmailService : IEmailService
{
    private readonly ILoggerService _logger;
    private readonly IUnitOfWork _unitOfWork;

    public EmailService(ILoggerService logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
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
            throw new InvalidOperationException("Email configuration is missing in environment variables.");

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

    public async Task SendWelcomeNewUserAsync(EmailRequestDTO emailRequest)
    {
        // Create a welcome email
        var welcomeEmail = new EmailDTO
        {
            To = emailRequest.UserEmail,
            Subject = "Welcome to VaccinaCare!",
            Body = $@"
        <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <h1 style='color: #1e1b4b; text-align: center;'>Welcome, {emailRequest.UserEmail}!</h1>
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

    public async Task SendDeactivationNotificationAsync(EmailRequestDTO emailRequest)
    {
        // Create a deactivation notification email
        var deactivationEmail = new EmailDTO
        {
            To = emailRequest.UserEmail,
            Subject = "Your VaccinaCare Account Has Been Deactivated",
            Body = $@"
    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>

        <h1 style='color: #1e1b4b; text-align: center;'>Dear {emailRequest.UserEmail},</h1>

        <p style='font-size: 16px;'>We regret to inform you that your VaccinaCare account has been deactivated. This action was taken due to various reasons, which may include inactivity or a request for deactivation.</p>
        
        <p style='font-size: 16px;'>Here are a few things you should know:</p>
        <ul style='font-size: 16px; padding-left: 20px;'>
            <li>Your vaccination appointment history will still be available for viewing.</li>
            <li>You will no longer receive reminders for future vaccinations.</li>
            <li>If you believe this was a mistake or wish to discuss your account, please contact our support team.</li>
        </ul>

        <p style='font-size: 16px;'>For any inquiries or assistance, please reach out to us at <a href='mailto:support@vaccinacare.com' style='color: #1e1b4b;'>support@vaccinacare.com</a>.</p>

        <p style='font-size: 16px;'>Thank you for using VaccinaCare, and we hope to assist you in the future if needed.</p>

        <p style='font-size: 16px;'>Best regards,<br>
        <span style='color: #1e1b4b; font-weight: bold;'>The VaccinaCare Team</span></p>

    </div>
    "
        };
        // Send the email
        await SendEmailAsync(deactivationEmail);
    }

    public async Task SendSingleAppointmentConfirmationAsync(EmailRequestDTO emailRequest, Appointment appointment)
    {
        var email = new EmailDTO
        {
            To = emailRequest.UserEmail,
            Subject = "Appointment Confirmation - VaccinaCare",
            Body = $@"
        <div style='font-family: Arial, sans-serif; line-height: 1.8; color: #333;'>
            <h1 style='color: #1e1b4b; text-align: center; font-size: 24px;'>Appointment Confirmation</h1>
            <p style='font-size: 18px; margin-top: 20px;'>Dear <strong>{emailRequest.UserEmail}</strong>,</p>
            <p style='font-size: 16px; line-height: 1.6;'>We are pleased to inform you that your appointment for the vaccination of <strong>{appointment.AppointmentsVaccines.First().Vaccine?.VaccineName ?? "Unknown Vaccine"}</strong> has been successfully scheduled. Please find the details of the appointment below:</p>
            <div style='margin-top: 20px; background-color: #f8f8f8; padding: 20px; border-radius: 8px;'>
                <ul style='font-size: 16px; line-height: 1.6; padding-left: 20px;'>
                    <li><strong style='color: #1e1b4b;'>Child:</strong> {appointment.Child.FullName}</li>
                    <li><strong style='color: #1e1b4b;'>Appointment Date:</strong> {appointment.AppointmentDate:yyyy-MM-dd HH:mm}</li>
                    <li><strong style='color: #1e1b4b;'>Status:</strong> {appointment.Status}</li>
                    <li><strong style='color: #1e1b4b;'>Dose:</strong> {appointment.AppointmentsVaccines.First().DoseNumber}/{appointment.AppointmentsVaccines.First().Vaccine?.RequiredDoses}</li>
                    <li><strong style='color: #1e1b4b;'>Total Price:</strong> {appointment.AppointmentsVaccines.First().TotalPrice:C0} VND</li>
                </ul>
            </div>
            <p style='font-size: 16px; margin-top: 20px;'>If you have any questions or need further assistance, please feel free to contact our support team at <a href='mailto:support@vaccinacare.com' style='color: #1e1b4b;'>support@vaccinacare.com</a>.</p>
            <p style='font-size: 16px;'>We look forward to serving you.</p>
            <p style='font-size: 16px; margin-top: 30px;'>Best regards,<br>
            <span style='color: #1e1b4b; font-weight: bold;'>The VaccinaCare Team</span></p>
        </div>
        "
        };

        await SendEmailAsync(email);
    }

    public async Task SendPackageAppointmentConfirmationAsync(EmailRequestDTO emailRequest,
        List<Appointment> appointments, Guid packageId)
    {
        // Lấy thông tin gói vaccine từ PackageId
        var vaccinePackage = await _unitOfWork.VaccinePackageRepository.GetByIdAsync(packageId);
        if (vaccinePackage == null)
        {
            throw new ArgumentException($"Gói vaccine với ID {packageId} không tồn tại.");
        }

        var totalPrice = vaccinePackage.Price ?? 0; // Nếu giá gói vaccine không có, mặc định là 0

        // Định dạng giá tiền đúng với đơn vị VND
        var totalPriceFormatted = totalPrice.ToString("C0", new CultureInfo("vi-VN"));

        var emailBody = $@"
    <div style='font-family: Arial, sans-serif; line-height: 1.8; color: #333;'>
        <h1 style='color: #1e1b4b; text-align: center; font-size: 24px;'>Vaccine Package Appointment Confirmation</h1>
        <p style='font-size: 18px; margin-top: 20px;'>Dear <strong>{emailRequest.UserEmail}</strong>,</p>
        <p style='font-size: 16px; line-height: 1.6;'>We are pleased to inform you that your vaccination package appointments have been successfully scheduled. Below is the full list of your upcoming vaccination appointments:</p>
        <table style='width: 100%; border-collapse: collapse; margin-top: 20px; background-color: #f8f8f8;'>
            <thead>
                <tr style='background-color: #1e1b4b; color: white;'>
                    <th style='padding: 10px; border: 1px solid #ddd;'>Vaccine Name</th>
                    <th style='padding: 10px; border: 1px solid #ddd;'>Dose</th>
                    <th style='padding: 10px; border: 1px solid #ddd;'>Appointment Date</th>
                    <th style='padding: 10px; border: 1px solid #ddd;'>Status</th>
                    <th style='padding: 10px; border: 1px solid #ddd;'>Price</th>
                </tr>
            </thead>
            <tbody>";

        foreach (var appointment in appointments)
        {
            var vaccine = appointment.AppointmentsVaccines.First().Vaccine;
            var doseNumber = appointment.AppointmentsVaccines.First().DoseNumber;
            var totalPriceForAppointment = appointment.AppointmentsVaccines.First().TotalPrice;

            // Định dạng giá tiền cho mỗi lịch hẹn
            var priceFormatted = totalPriceForAppointment?.ToString("C0", new CultureInfo("vi-VN")) ?? "0 VND";

            emailBody += $@"
            <tr>
                <td style='padding: 10px; border: 1px solid #ddd; text-align: center;'>{vaccine?.VaccineName ?? "Unknown"}</td>
                <td style='padding: 10px; border: 1px solid #ddd; text-align: center;'>{doseNumber}/{vaccine?.RequiredDoses}</td>
                <td style='padding: 10px; border: 1px solid #ddd; text-align: center;'>{appointment.AppointmentDate:yyyy-MM-dd HH:mm}</td>
                <td style='padding: 10px; border: 1px solid #ddd; text-align: center;'>{appointment.Status}</td>
                <td style='padding: 10px; border: 1px solid #ddd; text-align: center;'>{priceFormatted}</td>
            </tr>";
        }

        emailBody += $@"
            </tbody>
        </table>
        <p style='font-size: 16px; margin-top: 20px;'>The total price for this package is: <strong>{totalPriceFormatted}</strong></p>
        <p style='font-size: 16px; margin-top: 20px;'>If you have any questions or need further assistance, please feel free to contact our support team at <a href='mailto:support@vaccinacare.com' style='color: #1e1b4b;'>support@vaccinacare.com</a>.</p>
        <p style='font-size: 16px;'>We look forward to serving you.</p>
        <p style='font-size: 16px; margin-top: 30px;'>Best regards,<br>
        <span style='color: #1e1b4b; font-weight: bold;'>The VaccinaCare Team</span></p>
    </div>";

        var email = new EmailDTO
        {
            To = emailRequest.UserEmail,
            Subject = "Vaccine Package Appointment Confirmation - VaccinaCare",
            Body = emailBody
        };

        await SendEmailAsync(email);
    }
}