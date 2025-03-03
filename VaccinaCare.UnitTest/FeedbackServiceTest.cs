using Moq;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Service;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.DTOs.FeedbackDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.UnitTest
{
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

            _feedbackService = new FeedbackService(_unitOfWorkMock.Object,_loggerMock.Object,_claimsMock.Object);
        }

        [Fact]
        //Test case 1 : Create feedback successfully
        public async Task CreateFeedback_Successfully()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var appointmentId = Guid.NewGuid();

            var feedbackDto = new FeedbackDTO { AppointmentId = appointmentId, Rating = 5, Comments = "Great service!" };
            var appointment = new Appointment { Id = appointmentId, ParentId = userId, Status = AppointmentStatus.Completed};
            var feedback = new Feedback { Id = Guid.NewGuid(), AppointmentId = appointmentId, Rating = 5, Comments = "Great service!", CreatedBy = userId };

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
            await Assert.ThrowsAsync<KeyNotFoundException>(()=> _feedbackService.CreateFeedbackAsync(feedbackDto));
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
            var appointment = new Appointment { Id = appointmentId, ParentId = otherUserId, Status = AppointmentStatus.Completed };

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
            var appointment = new Appointment { Id = appointmentId, ParentId = userId, Status = AppointmentStatus.Completed };

            _claimsMock.Setup(c => c.GetCurrentUserId).Returns(userId);
            _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetByIdAsync(appointmentId)).ReturnsAsync(appointment);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _feedbackService.CreateFeedbackAsync(feedbackDto));
        }
    }
}
