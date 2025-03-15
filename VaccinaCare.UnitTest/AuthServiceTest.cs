//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using VaccinaCare.Application.Interface.Common;
//using Moq;
//using VaccinaCare.Application.Service;
//using VaccinaCare.Repository.Interfaces;
//using Microsoft.AspNetCore.Identity;
//using VaccinaCare.Domain.DTOs.AuthDTOs;
//using VaccinaCare.Domain.Entities;
//using VaccinaCare.Domain.Enums;
//using System.Linq.Expressions;
//using Microsoft.Extensions.Configuration;
//using VaccinaCare.Application.Ultils;


//namespace VaccinaCare.UnitTest;

//public class AuthServiceTest
//{
//    private readonly Mock<ILoggerService> _loggerMock;
//    private readonly AuthService _authService;
//    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
//    private readonly Mock<IConfiguration> _mockConfiguration;
//    private readonly Mock<PasswordHasher> _passwordHasherMock;

//    public AuthServiceTest(Mock<ILoggerService> loggerMock, AuthService authService, Mock<IUnitOfWork> unitOfWorkMock,
//        Mock<IConfiguration> mockConfiguration, Mock<PasswordHasher> passwordHasherMock)
//    {
//        _loggerMock = loggerMock;
//        _authService = authService;
//        _unitOfWorkMock = unitOfWorkMock;
//        _mockConfiguration = mockConfiguration;
//        _passwordHasherMock = passwordHasherMock;
//    }

//    [Fact]
//    // Test case 1: Create Account Successfully
//    public async Task CreateAccount_ShouldReturnUser_WhenAllFieldsAreValid()
//    {
//        //Arrange
//        var registerRequest = new RegisterRequestDTO
//        {
//            Email = "testuser@example.com",
//            FullName = "John Doe",
//            Password = "Password123",
//            PhoneNumber = "123456789",
//            Gender = true,
//            DateOfBirth = new DateTime(1990, 1, 1),
//            Address = "123 Main St",
//            ImageUrl = "http://example.com/image.jpg"
//        };

//        var newUser = new User
//        {
//            Email = registerRequest.Email.Trim(),
//            FullName = registerRequest.FullName.Trim(),
//            PasswordHash = "hashedPassword123",
//            PhoneNumber = registerRequest.PhoneNumber.Trim(),
//            Gender = registerRequest.Gender,
//            DateOfBirth = registerRequest.DateOfBirth,
//            Address = registerRequest.Address.Trim(),
//            ImageUrl = registerRequest.ImageUrl,
//            RoleName = RoleType.Customer
//        };


//        _unitOfWorkMock.Setup(u => u.UserRepository.AddAsync(It.IsAny<User>()))
//            .ReturnsAsync((User u) => u);
//        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

//        // Act
//        var result = await _authService.RegisterAsync(registerRequest);

//        // Assert
//        Assert.NotNull(result);
//        Assert.Equal(registerRequest.Email, result.Email);
//        Assert.Equal(registerRequest.FullName, result.FullName);
//        Assert.Equal(RoleType.Customer, result.RoleName);
//    }

//    [Fact]
//    //Test case 2 : Create Account When Email Already Exists
//    public async Task CreateAccount_ShouldReturnNull_WhenEmailAlreadyExists()
//    {
//        // Arrange
//        var registerRequest = new RegisterRequestDTO
//        {
//            Email = "existinguser@example.com",
//            FullName = "Jane Doe",
//            Password = "Password123",
//            PhoneNumber = "987654321",
//            Gender = true,
//            DateOfBirth = new DateTime(1992, 5, 10),
//            Address = "456 Main St",
//            ImageUrl = "http://example.com/image.jpg"
//        };

//        _unitOfWorkMock.Setup(u =>
//                u.UserRepository.FirstOrDefaultAsync(
//                    It.Is<Expression<Func<User, bool>>>(expr => expr.Compile().Invoke(It.IsAny<User>()))))
//            .ReturnsAsync(new User { Email = registerRequest.Email });

//        // Act
//        var result = await _authService.RegisterAsync(registerRequest);

//        // Assert
//        Assert.Null(result);
//    }

//    [Fact]
//    //Test case 3: Create Account When Missing Email
//    public async Task CreateAccount_ShouldReturnNull_WhenEmailIsMissing()
//    {
//        // Arrange
//        var registerRequest = new RegisterRequestDTO
//        {
//            Email = "", // Email null 
//            FullName = "Alice Smith",
//            Password = "Password123",
//            PhoneNumber = "123456789",
//            Gender = true,
//            DateOfBirth = new DateTime(1990, 7, 20),
//            Address = "789 Main St",
//            ImageUrl = "http://example.com/image.jpg"
//        };

//        // Act
//        var result = await _authService.RegisterAsync(registerRequest);

//        // Assert
//        Assert.Null(result);
//    }

//    [Fact]
//    //Test case 4 : Create Account When Missing Password
//    public async Task CreateAccount_ShouldReturnNull_WhenPasswordIsMissing()
//    {
//        // Arrange
//        var registerRequest = new RegisterRequestDTO
//        {
//            Email = "newuser@example.com",
//            FullName = "Bob Brown",
//            Password = "", // Password null
//            PhoneNumber = "123456789",
//            Gender = true,
//            DateOfBirth = new DateTime(1991, 3, 15),
//            Address = "321 Main St",
//            ImageUrl = "http://example.com/image.jpg"
//        };

//        // Act
//        var result = await _authService.RegisterAsync(registerRequest);

//        // Assert
//        Assert.Null(result);
//    }

//    [Fact]
//    //Test case 5 : Create Account When Email Is Invalid
//    public async Task CreateAccount_ShouldReturnNull_WhenEmailIsInvalid()
//    {
//        // Arrange
//        var registerRequest = new RegisterRequestDTO
//        {
//            Email = "invalidemail.com", // Email is invalid
//            FullName = "Charlie White",
//            Password = "Password123",
//            PhoneNumber = "123456789",
//            Gender = true,
//            DateOfBirth = new DateTime(1989, 8, 12),
//            Address = "456 Main St",
//            ImageUrl = "http://example.com/image.jpg"
//        };

//        // Act
//        var result = await _authService.RegisterAsync(registerRequest);

//        // Assert
//        Assert.Null(result);
//    }

//    [Fact]
//    //Test case 6 : Login When Email Is Missing
//    public async Task LoginAsync_ShouldReturnNull_WhenEmailIsMissing()
//    {
//        //Arrage
//        var loginRequest = new LoginRequestDto { Email = "", Password = "Password123" };

//        //Act
//        var result = await _authService.LoginAsync(loginRequest, _mockConfiguration.Object);

//        //Assert
//        Assert.Null(result);
//    }

//    [Fact]
//    //Test case 7 : Login When User Does Not Exist
//    public async Task LoginAsync_ShouldReturnNull_WhenUserDoesNotExist()
//    {
//        //Arrange
//        var loginRequest = new LoginRequestDto { Email = "nonexistent@example.com", Password = "Password123" };
//        _unitOfWorkMock.Setup(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
//            .ReturnsAsync((User?)null);
//        //Act
//        var result = await _authService.LoginAsync(loginRequest, _mockConfiguration.Object);
//        //Assert
//        Assert.Null(result);
//    }

//    [Fact]
//    // Test case 8 : Login When Password Is Incorrect
//    public async Task LoginAsync_ShouldReturnNull_WhenPasswordIsIncorrect()
//    {
//        // Arrange
//        var loginRequest = new LoginRequestDto { Email = "testuser@example.com", Password = "WrongPassword" };

//        var user = new User { Email = "testuser@example.com", PasswordHash = "hashedCorrectPassword" };

//        _unitOfWorkMock.Setup(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
//            .ReturnsAsync(user);

//        // Act
//        var result = await _authService.LoginAsync(loginRequest, _mockConfiguration.Object);

//        // Assert
//        Assert.Null(result);
//    }

//    [Fact]
//    //Test case 9: Login When User Is Deleted
//    public async Task LoginAsync_ShouldReturnNull_WhenUserIsDeleted()
//    {
//        // Arrange
//        var loginRequest = new LoginRequestDto { Email = "deleteduser@example.com", Password = "Password123" };
//        var user = new User { Email = "deleteduser@example.com", IsDeleted = true };
//        _unitOfWorkMock.Setup(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
//            .ReturnsAsync(user);

//        // Act
//        var result = await _authService.LoginAsync(loginRequest, _mockConfiguration.Object);

//        // Assert
//        Assert.Null(result);
//    }

//    [Fact]
//    //Test case 10 : Logout When User Exists
//    public async Task LogoutAsync_ShouldReturnTrue_WhenUserExists()
//    {
//        // Arrange
//        var userId = Guid.NewGuid();
//        var user = new User
//        {
//            Id = userId,
//            IsDeleted = false,
//            RefreshToken = "old_refresh_token",
//            RefreshTokenExpiryTime = DateTime.UtcNow.AddHours(1)
//        };

//        _unitOfWorkMock
//            .Setup(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
//            .ReturnsAsync(user);

//        _unitOfWorkMock.Setup(u => u.UserRepository.Update(user));
//        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

//        // Act
//        var result = await _authService.LogoutAsync(userId);

//        // Assert
//        Assert.True(result);
//        Assert.Null(user.RefreshToken);
//        Assert.Null(user.RefreshTokenExpiryTime);
//        _unitOfWorkMock.Verify(u => u.UserRepository.Update(user), Times.Once);
//        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
//    }

//    [Fact]
//    //Test case 11 : Logout When Have Exception
//    public async Task LogoutAsync_ShouldThrowException_WhenUnexpectedErrorOccurs()
//    {
//        // Arrange
//        var userId = Guid.NewGuid();

//        // Giả lập lỗi khi tìm user
//        _unitOfWorkMock
//            .Setup(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
//            .ThrowsAsync(new Exception("Database error"));

//        // Act & Assert
//        await Assert.ThrowsAsync<Exception>(() => _authService.LogoutAsync(userId));
//        _loggerMock.Verify(l => l.Error(It.IsAny<string>()), Times.AtLeastOnce);
//    }

//    [Fact]
//    //Test case 12 : Logout When User Does Not Exist
//    public async Task LogoutAsync_ShouldReturnFalse_WhenUserDoesNotExist()
//    {
//        // Arrange
//        var userId = Guid.NewGuid();

//        _unitOfWorkMock
//            .Setup(u => u.UserRepository.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
//            .ReturnsAsync((User?)null);

//        // Act
//        var result = await _authService.LogoutAsync(userId);

//        // Assert
//        Assert.False(result);
//        _unitOfWorkMock.Verify(u => u.UserRepository.Update(It.IsAny<User>()), Times.Never);
//        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
//    }
//}