namespace VaccinaCare.Domain.DTOs.EmailDTOs;

public class EmailRequestDTO
{
    /// <summary>
    /// The email address of the recipient.
    /// </summary>
    public string UserEmail { get; set; }

    /// <summary>
    /// The name of the recipient.
    /// </summary>
    public string UserName { get; set; }

    public EmailRequestDTO(string userEmail, string userName)
    {
        UserEmail = userEmail;
        UserName = userName;
    }
}