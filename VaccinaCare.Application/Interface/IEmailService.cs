namespace VaccinaCare.Application.Interface
{
    public interface IEmailService
    {
        Task SendWelcomeNewUserAsync(string userEmail, string userName);
    }
}
