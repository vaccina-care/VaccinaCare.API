using VaccinaCare.Application.Interface.Common;
using Moq;
using VaccinaCare.Application.Service;
using VaccinaCare.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Application.Interface;
using VaccinaCare.Domain.DTOs.AuthDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using System.Linq.Expressions;
using VaccinaCare.Domain.DTOs.EmailDTOs;
using Microsoft.AspNetCore.Identity;


namespace VaccinaCare.UnitTest;

public class AuthServiceTest
{
    private readonly Mock<ILoggerService> _loggerMock;
    private readonly AuthService _authService;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IConfiguration> _mockConfiguration = new Mock<IConfiguration>();
    private readonly Mock<PasswordHasher> _passwordHasherMock;

    public AuthServiceTest()
    {
        _loggerMock = new Mock<ILoggerService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _emailServiceMock = new Mock<IEmailService>();
        _passwordHasherMock = new Mock<PasswordHasher>();

        _authService = new AuthService(_unitOfWorkMock.Object, _loggerMock.Object, _emailServiceMock.Object);
    }

    [Fact]
    // Test case 1: Create Account Successfully
    public async Task CreateAccount_ShouldReturnUser_WhenAllFieldsAreValid()
    {
        //Arrange
        var registerRequest = new RegisterRequestDTO
        {
            Email = "testuser@example.com",
            FullName = "John Doe",
            Password = "Password123",
            PhoneNumber = "123456789",
            Gender = true,
            DateOfBirth = new DateTime(1990, 1, 1),
            Address = "123 Main St",
            ImageUrl = "http://example.com/image.jpg"
        };

        var hashedPassword = "hashedPassword123";

        var expectedUser = new User
        {
            Email = registerRequest.Email.Trim(),
            FullName = registerRequest.FullName.Trim(),
            PasswordHash = hashedPassword,
            PhoneNumber = registerRequest.PhoneNumber.Trim(),
            Gender = registerRequest.Gender,
            DateOfBirth = registerRequest.DateOfBirth,
            Address = registerRequest.Address.Trim(),
            ImageUrl = registerRequest.ImageUrl,
            RoleName = RoleType.Customer
        };


        _unitOfWorkMock.Setup(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
        .ReturnsAsync((User)null);

        _unitOfWorkMock.Setup(u => u.UserRepository.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _emailServiceMock.Setup(e => e.SendWelcomeNewUserAsync(It.IsAny<EmailRequestDTO>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(registerRequest.Email, result.Email);
        Assert.Equal(registerRequest.FullName, result.FullName);
        Assert.Equal(RoleType.Customer, result.RoleName);

        _unitOfWorkMock.Verify(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.UserRepository.AddAsync(It.IsAny<User>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _emailServiceMock.Verify(e => e.SendWelcomeNewUserAsync(It.IsAny<EmailRequestDTO>()), Times.Once);
    }

    [Fact]
    // Test case 2: Create Account When Email Already Exists
    public async Task RegisterAsync_ShouldReturnNull_WhenEmailAlreadyExists()
    {
        // Arrange
        var registerRequest = new RegisterRequestDTO
        {
            Email = "existinguser@example.com",
            FullName = "Jane Doe",
            Password = "Password123",
            PhoneNumber = "987654321",
            Gender = true,
            DateOfBirth = new DateTime(1992, 5, 10),
            Address = "456 Main St",
            ImageUrl = "http://example.com/image.jpg"
        };

        var existingUser = new User { Email = registerRequest.Email };

        _unitOfWorkMock.Setup(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        Assert.Null(result);

        _unitOfWorkMock.Verify(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.UserRepository.AddAsync(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _emailServiceMock.Verify(e => e.SendWelcomeNewUserAsync(It.IsAny<EmailRequestDTO>()), Times.Never);
    }

    [Theory]
    [InlineData(null)] 
    [InlineData("")]
    [InlineData("   ")] 
    //Test case 3: Create Account When Missing Email
    public async Task RegisterAsync_ShouldReturnNull_WhenEmailIsMissing(string? email)
    {
        // Arrange
        var registerRequest = new RegisterRequestDTO
        {
            Email = email,
            FullName = "Alice Smith",
            Password = "Password123",
            PhoneNumber = "123456789",
            Gender = true,
            DateOfBirth = new DateTime(1990, 7, 20),
            Address = "789 Main St",
            ImageUrl = "http://example.com/image.jpg"
        };

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        Assert.Null(result);

        _unitOfWorkMock.Verify(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.UserRepository.AddAsync(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _emailServiceMock.Verify(e => e.SendWelcomeNewUserAsync(It.IsAny<EmailRequestDTO>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")] 
    [InlineData("   ")]
    //Test case 4: Create Account When Missing Password
    public async Task RegisterAsync_ShouldReturnNull_WhenPasswordIsMissing(string? password)
    {
        // Arrange
        var registerRequest = new RegisterRequestDTO
        {
            Email = "newuser@example.com",
            FullName = "Bob Brown",
            Password = password,
            PhoneNumber = "123456789",
            Gender = true,
            DateOfBirth = new DateTime(1991, 3, 15),
            Address = "321 Main St",
            ImageUrl = "http://example.com/image.jpg"
        };

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        Assert.Null(result);

        _unitOfWorkMock.Verify(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.UserRepository.AddAsync(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _emailServiceMock.Verify(e => e.SendWelcomeNewUserAsync(It.IsAny<EmailRequestDTO>()), Times.Never);
    }

    [Theory]
    [InlineData("invalidemail.com")]
    [InlineData("@example.com")]
    [InlineData("user@.com")]
    [InlineData("user@domain")]
    //Test case 5: Create Account When Email Is Invalid
    public async Task RegisterAsync_ShouldReturnNull_WhenEmailIsInvalid(string invalidEmail)
    {
        // Arrange
        var registerRequest = new RegisterRequestDTO
        {
            Email = invalidEmail,
            FullName = "Charlie White",
            Password = "Password123",
            PhoneNumber = "123456789",
            Gender = true,
            DateOfBirth = new DateTime(1989, 8, 12),
            Address = "456 Main St",
            ImageUrl = "http://example.com/image.jpg"
        };

        // Act
        var result = await _authService.RegisterAsync(registerRequest);

        // Assert
        Assert.Null(result);

        // Kiểm tra không truy vấn database
        _unitOfWorkMock.Verify(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.UserRepository.AddAsync(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _emailServiceMock.Verify(e => e.SendWelcomeNewUserAsync(It.IsAny<EmailRequestDTO>()), Times.Never);
    }

    [Fact]
    // Test case 6: Login When Email Is Missing
    public async Task LoginAsync_ShouldReturnNull_WhenEmailIsMissing()
    {
        // Arrange
        var loginRequest = new LoginRequestDto { Email = "", Password = "Password123" };

        _loggerMock.Setup(l => l.Warn(It.IsAny<string>()));

        // Act
        var result = await _authService.LoginAsync(loginRequest, _mockConfiguration.Object);

        // Assert
        Assert.Null(result);

        _loggerMock.Verify(l => l.Warn("Login attempt failed: Missing email or password. Both fields are required."), Times.Once);
    }

    [Fact]
    // Test case 7: Login When User Does Not Exist
    public async Task LoginAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var loginRequest = new LoginRequestDto { Email = "nonexistent@example.com", Password = "Password123" };

        _unitOfWorkMock.Setup(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);

        _loggerMock.Setup(l => l.Warn(It.IsAny<string>()));

        // Act
        var result = await _authService.LoginAsync(loginRequest, _mockConfiguration.Object);

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(l => l.Warn($"Login attempt failed: No active user found with email: {loginRequest.Email}."), Times.Once);
    }

    [Fact]
    //Test case 8 : Logout When User Exists
    public async Task LogoutAsync_ShouldReturnTrue_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            IsDeleted = false,
            RefreshToken = "old_refresh_token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddHours(1)
        };

        _unitOfWorkMock
            .Setup(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _authService.LogoutAsync(userId);

        // Assert
        Assert.True(result);
        Assert.Null(user.RefreshToken);
        Assert.Null(user.RefreshTokenExpiryTime);

        _unitOfWorkMock.Verify(u => u.UserRepository.Update(It.Is<User>(u => u.Id == userId)), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    // Test case 9: Logout When Have Exception
    public async Task LogoutAsync_ShouldThrowException_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedErrorMessage = "Database error";

        _unitOfWorkMock
            .Setup(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ThrowsAsync(new Exception(expectedErrorMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LogoutAsync(userId));

        Assert.Equal(expectedErrorMessage, exception.Message);

    }

    [Fact]
    //Test case 10: Logout When User Does Not Exist
    public async Task LogoutAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _unitOfWorkMock
            .Setup(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LogoutAsync(userId);

        // Assert
        Assert.False(result);

        _unitOfWorkMock.Verify(u => u.UserRepository.Update(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}

