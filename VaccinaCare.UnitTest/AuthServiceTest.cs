using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Application.Interface.Common;
using Moq;
using VaccinaCare.Application.Service;
using VaccinaCare.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using VaccinaCare.Domain.DTOs.AuthDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using System.Linq.Expressions;

namespace VaccinaCare.UnitTest
{
    public class AuthServiceTest
    {
        private readonly Mock<ILoggerService> _loggerMock;
        private readonly AuthService _authService;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        public AuthServiceTest()
        {
            _loggerMock = new Mock<ILoggerService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _authService = new AuthService(_unitOfWorkMock.Object, _loggerMock.Object);
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

            var newUser = new User
            {
                Email = registerRequest.Email.Trim(),
                FullName = registerRequest.FullName.Trim(),
                PasswordHash = "hashedPassword123",
                PhoneNumber = registerRequest.PhoneNumber.Trim(),
                Gender = registerRequest.Gender,
                DateOfBirth = registerRequest.DateOfBirth,
                Address = registerRequest.Address.Trim(),
                ImageUrl = registerRequest.ImageUrl,
                RoleName = RoleType.Customer
            };


            _unitOfWorkMock.Setup(u => u.UserRepository.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _authService.RegisterAsync(registerRequest);

            // Assert
            Assert.NotNull(result); 
            Assert.Equal(registerRequest.Email, result.Email); 
            Assert.Equal(registerRequest.FullName, result.FullName); 
            Assert.Equal(RoleType.Customer, result.RoleName); 
        }
        [Fact]
        //Test case 2 : Create Account When Email Already Exists
        public async Task CreateAccount_ShouldReturnNull_WhenEmailAlreadyExists()
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

            _unitOfWorkMock.Setup(u => u.UserRepository.FirstOrDefaultAsync(It.Is<Expression<Func<User, bool>>>(expr => expr.Compile().Invoke(It.IsAny<User>()))))
                           .ReturnsAsync(new User { Email = registerRequest.Email });

            // Act
            var result = await _authService.RegisterAsync(registerRequest);

            // Assert
            Assert.Null(result);
        }
        [Fact]
        //Test case 3: Create Account When Missing Email
        public async Task CreateAccount_ShouldReturnNull_WhenEmailIsMissing()
        {
            // Arrange
            var registerRequest = new RegisterRequestDTO
            {
                Email = "", // Email null 
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
        }
        [Fact]
        //Test case 4 : Create Account When Missing Password
        public async Task CreateAccount_ShouldReturnNull_WhenPasswordIsMissing()
        {
            // Arrange
            var registerRequest = new RegisterRequestDTO
            {
                Email = "newuser@example.com",
                FullName = "Bob Brown",
                Password = "", // Password null
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
        }
        [Fact]
        //Test case 5 : Create Account When Email Is Invalid
        public async Task CreateAccount_ShouldReturnNull_WhenEmailIsInvalid()
        {
            // Arrange
            var registerRequest = new RegisterRequestDTO
            {
                Email = "invalidemail.com", // Email is invalid
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
        }

    }
}
