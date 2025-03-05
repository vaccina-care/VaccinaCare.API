using Microsoft.EntityFrameworkCore;
using Moq;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Service;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.DTOs.FeedbackDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Commons;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.UnitTest;

public class FeedbackServiceTest
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILoggerService> _loggerMock;
    private readonly Mock<IClaimsService> _claimsMock;
    private readonly FeedbackService _feedbackService;

    public FeedbackServiceTest()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILoggerService>();
        _claimsMock = new Mock<IClaimsService>();

        _feedbackService = new FeedbackService(_unitOfWorkMock.Object, _loggerMock.Object, _claimsMock.Object);
    }

    [Fact]
    //Test case 1 : Create feedback successfully
    public async Task CreateFeedback_Successfully()
    {
        //Arrange
        var userId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();

        var feedbackDto = new FeedbackDTO { AppointmentId = appointmentId, Rating = 5, Comments = "Great service!" };
        var appointment = new Appointment
            { Id = appointmentId, ParentId = userId, Status = AppointmentStatus.Completed };
        var feedback = new Feedback
        {
            Id = Guid.NewGuid(), AppointmentId = appointmentId, Rating = 5, Comments = "Great service!",
            CreatedBy = userId
        };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(userId);
        _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetByIdAsync(appointmentId)).ReturnsAsync(appointment);
        _unitOfWorkMock.Setup(u => u.FeedbackRepository.AddAsync(It.IsAny<Feedback>())).ReturnsAsync(feedback);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _feedbackService.CreateFeedbackAsync(feedbackDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(feedbackDto.AppointmentId, result.AppointmentId);
        Assert.Equal(feedbackDto.Rating, result.Rating);
        Assert.Equal(feedbackDto.Comments, result.Comments);
    }

    [Fact]
    //Test case 2 : Create feedback when appointment not found
    public async Task CreateFeedback_AppointmentNotFound_ThorwsException()
    {
        //Arrange
        var appointmentId = Guid.NewGuid();
        var feedbackDto = new FeedbackDTO { AppointmentId = appointmentId, Rating = 5, Comments = "Great service!" };
        _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetByIdAsync(appointmentId)).ReturnsAsync((Appointment)null);

        //Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _feedbackService.CreateFeedbackAsync(feedbackDto));
    }

    [Fact]
    //Test case 3 : Create feedback witt unauthorized user
    public async Task CreateFeedback_UnauthorizedUser_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();

        var feedbackDto = new FeedbackDTO { AppointmentId = appointmentId, Rating = 5, Comments = "Great service!" };
        var appointment = new Appointment
            { Id = appointmentId, ParentId = otherUserId, Status = AppointmentStatus.Completed };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(userId);
        _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetByIdAsync(appointmentId)).ReturnsAsync(appointment);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _feedbackService.CreateFeedbackAsync(feedbackDto));
    }

    [Fact]
    //Test case 4: Create feedback with appointment not completed
    public async Task CreateFeedback_AppointmentNotCompleted_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        var feedbackDto = new FeedbackDTO { AppointmentId = appointmentId, Rating = 4, Comments = "Test" };
        var appointment = new Appointment { Id = appointmentId, ParentId = userId, Status = AppointmentStatus.Pending };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(userId);
        _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetByIdAsync(appointmentId)).ReturnsAsync(appointment);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _feedbackService.CreateFeedbackAsync(feedbackDto));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    //Test case 5-6: Create feedback with invalid rating
    public async Task CreateFeedback_InvalidRating_ThrowsException(int invalidRating)
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var feedbackDto = new FeedbackDTO { AppointmentId = appointmentId, Rating = invalidRating };
        var userId = Guid.NewGuid();
        var appointment = new Appointment
            { Id = appointmentId, ParentId = userId, Status = AppointmentStatus.Completed };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(userId);
        _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetByIdAsync(appointmentId)).ReturnsAsync(appointment);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _feedbackService.CreateFeedbackAsync(feedbackDto));
    }

    [Fact]
    //Test case 7: Get feedback by id when feedback exists
    public async Task GetFeedbackByIdAsync_ShouldReturnFeedbackDTO_WhenFeedbackExists()
    {
        // Arrange
        var feedbackId = Guid.NewGuid();
        var feedback = new Feedback
        {
            Id = feedbackId,
            AppointmentId = Guid.NewGuid(),
            Rating = 5,
            Comments = "Great service!"
        };

        _unitOfWorkMock.Setup(u => u.FeedbackRepository.GetByIdAsync(feedbackId)).ReturnsAsync(feedback);

        // Act
        var result = await _feedbackService.GetFeedbackByIdAsync(feedbackId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(feedback.AppointmentId, result.AppointmentId);
        Assert.Equal(feedback.Rating, result.Rating);
        Assert.Equal(feedback.Comments, result.Comments);
    }

    [Fact]
    //Test case 8: Get feedback when feedback not found
    public async Task GetFeedbackByIdAsync_ShouldThrowKeyNotFoundException_WhenFeedbackDoesNotExist()
    {
        //Arrange
        var feedbackId = Guid.NewGuid();

        _unitOfWorkMock.Setup(u => u.FeedbackRepository.GetByIdAsync(feedbackId)).ReturnsAsync((Feedback)null);

        //Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _feedbackService.GetFeedbackByIdAsync(feedbackId));
    }

    [Fact]
    //Test case 9: Get feedback when database error
    public async Task GetFeedbackByIdAsync_ShouldThrowException_WhenRepositoryFails()
    {
        //Arrange
        var feedbackId = Guid.NewGuid();

        _unitOfWorkMock.Setup(u => u.FeedbackRepository.GetByIdAsync(feedbackId))
            .ThrowsAsync(new Exception("Database error"));

        //Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _feedbackService.GetFeedbackByIdAsync(feedbackId));
        Assert.Equal("Database error", exception.Message);
    }

    [Fact]
    //Test case 10: Get feedback when feedback have null comments
    public async Task GetFeedbackByIdAsync_ShouldReturnFeedbackDTO_WhenCommentsAreNull()
    {
        // Arrange
        var feedbackId = Guid.NewGuid();
        var feedback = new Feedback
        {
            Id = feedbackId,
            AppointmentId = Guid.NewGuid(),
            Rating = 4,
            Comments = null
        };

        _unitOfWorkMock.Setup(u => u.FeedbackRepository.GetByIdAsync(feedbackId)).ReturnsAsync(feedback);

        // Act
        var result = await _feedbackService.GetFeedbackByIdAsync(feedbackId);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Comments);
    }

    [Fact]
    //Test case 11: Delete feedback successfully
    public async Task DeleteFeedbackAsync_ShouldDeleteFeedback_WhenFeedbackExists()
    {
        // Arrange
        var feedbackId = Guid.NewGuid();
        var feedback = new Feedback { Id = feedbackId };

        _unitOfWorkMock
            .Setup(u => u.FeedbackRepository.GetByIdAsync(feedbackId))
            .ReturnsAsync(feedback);

        _unitOfWorkMock
            .Setup(u => u.FeedbackRepository.SoftRemove(feedback))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _feedbackService.DeleteFeedbackAsync(feedbackId);

        // Assert
        _unitOfWorkMock.Verify(u => u.FeedbackRepository.SoftRemove(feedback), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    //Test case 12: Delete feedback not found
    public async Task DeleteFeedbackAsync_ShouldThrowKeyNotFoundException_WhenFeedbackDoesNotExist()
    {
        // Arrange
        var feedbackId = Guid.NewGuid();

        _unitOfWorkMock
            .Setup(u => u.FeedbackRepository.GetByIdAsync(feedbackId))
            .ReturnsAsync((Feedback)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _feedbackService.DeleteFeedbackAsync(feedbackId));

        _unitOfWorkMock.Verify(u => u.FeedbackRepository.SoftRemove(It.IsAny<Feedback>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    //Test case 13: Delete feedback when soft remove fails
    public async Task DeleteFeedbackAsync_ShouldThrowException_WhenSoftRemoveFails()
    {
        // Arrange
        var feedbackId = Guid.NewGuid();
        var feedback = new Feedback { Id = feedbackId };

        _unitOfWorkMock
            .Setup(u => u.FeedbackRepository.GetByIdAsync(feedbackId))
            .ReturnsAsync(feedback);

        _unitOfWorkMock
            .Setup(u => u.FeedbackRepository.SoftRemove(feedback))
            .ThrowsAsync(new Exception("SoftRemove failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _feedbackService.DeleteFeedbackAsync(feedbackId));
        Assert.Contains("SoftRemove failed", exception.Message);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    //Test case 14: Delete feedback when save changes fails
    public async Task DeleteFeedbackAsync_ShouldThrowException_WhenSaveChangesFails()
    {
        // Arrange
        var feedbackId = Guid.NewGuid();
        var feedback = new Feedback { Id = feedbackId };

        _unitOfWorkMock
            .Setup(u => u.FeedbackRepository.GetByIdAsync(feedbackId))
            .ReturnsAsync(feedback);

        _unitOfWorkMock
            .Setup(u => u.FeedbackRepository.SoftRemove(feedback))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new Exception("SaveChanges failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _feedbackService.DeleteFeedbackAsync(feedbackId));
        Assert.Contains("SaveChanges failed", exception.Message);
    }

    [Fact]
    //Test case 15: Update feedback successfully
    public async Task UpdateFeedbackAsync_Success()
    {
        //Arrange
        var feedbackId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        var feedbackDto = new FeedbackDTO { AppointmentId = appointmentId, Rating = 5, Comments = "Update feedback" };

        var feedback = new Feedback
        {
            Id = feedbackId,
            AppointmentId = appointmentId,
            Rating = 4,
            Comments = "Old feedback",
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var appointment = new Appointment
        {
            Id = appointmentId,
            ParentId = userId,
            Status = AppointmentStatus.Completed
        };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(userId);
        _unitOfWorkMock.Setup(u => u.FeedbackRepository.GetByIdAsync(feedbackId)).ReturnsAsync(feedback);
        _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetByIdAsync(appointmentId)).ReturnsAsync(appointment);
        _unitOfWorkMock.Setup(u => u.FeedbackRepository.Update(It.IsAny<Feedback>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _feedbackService.UpdateFeedbackAsync(feedbackId, feedbackDto);

        //Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Rating);
        Assert.Equal("Update feedback", result.Comments);
    }

    [Fact]
    //Test case 16: Update feedback not found
    public async Task UpdateFeedbackAsync_FeedbackNotFound_ThrowsException()
    {
        //Arrange
        var feedbackId = Guid.NewGuid();
        var feedbackDto = new FeedbackDTO { Rating = 5, Comments = "Updated feedback" };

        _unitOfWorkMock.Setup(u => u.FeedbackRepository.GetByIdAsync(feedbackId)).ReturnsAsync((Feedback)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _feedbackService.UpdateFeedbackAsync(feedbackId, feedbackDto));
    }

    [Fact]
    //Test case 17: Update feedback when user not owner
    public async Task UpdateFeedbackAsync_UserNotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var feedbackId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var feedback = new Feedback { AppointmentId = Guid.NewGuid() };
        var appointment = new Appointment { ParentId = Guid.NewGuid() };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(userId);
        _unitOfWorkMock.Setup(u => u.FeedbackRepository.GetByIdAsync(feedbackId)).ReturnsAsync(feedback);
        _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetByIdAsync(feedback.AppointmentId.Value))
            .ReturnsAsync(appointment);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _feedbackService.UpdateFeedbackAsync(feedbackId, new FeedbackDTO()));
    }

    [Fact]
    //Test case 18: Update feedback after 24 hours
    public async Task UpdateFeedbackAsync_UpdatedAfter24Hours_ThrowsInvalidOperationException()
    {
        // Arrange
        var feedbackId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var feedback = new Feedback
        {
            AppointmentId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddHours(-25)
        };
        var appointment = new Appointment { ParentId = userId };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(userId);
        _unitOfWorkMock.Setup(u => u.FeedbackRepository.GetByIdAsync(feedbackId)).ReturnsAsync(feedback);
        _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetByIdAsync(feedback.AppointmentId.Value))
            .ReturnsAsync(appointment);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _feedbackService.UpdateFeedbackAsync(feedbackId, new FeedbackDTO()));
    }
}