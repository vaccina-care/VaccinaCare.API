using Moq;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Service;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Interfaces;

//using System.Data.Entity;

namespace VaccinaCare.UnitTest;

public class AppoitmentServiceTest
{
    private readonly AppointmentService _appointmentService;
    private readonly Mock<IClaimsService> _claimsServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILoggerService> _loggerMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IVaccineService> _vaccineServiceMock;

    public AppoitmentServiceTest()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILoggerService>();
        _vaccineServiceMock = new Mock<IVaccineService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _emailServiceMock = new Mock<IEmailService>();
        _claimsServiceMock = new Mock<IClaimsService>();
    }

    [Fact]
    //Test case 1: Generate Appointments For Single Vaccine When Vaccine Not Found
    public async Task GenerateAppointmentsForSingleVaccine_ShouldThrowException_WhenVaccineNotFound()
    {
        // Arrange
        var request = new CreateAppointmentSingleVaccineDto
            { VaccineId = Guid.NewGuid(), ChildId = Guid.NewGuid(), StartDate = DateTime.UtcNow };
        _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(request.VaccineId))
            .ReturnsAsync((Vaccine)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _appointmentService.GenerateAppointmentsForSingleVaccine(request, Guid.NewGuid()));
    }

    [Fact]
    //Test case 2: Generate Appointments For Single Vaccine When Child Not Eligible
    public async Task GenerateAppointmentsForSingleVaccine_ShouldThrowException_WhenChildNotEligible()
    {
        // Arrange
        var request = new CreateAppointmentSingleVaccineDto
            { VaccineId = Guid.NewGuid(), ChildId = Guid.NewGuid(), StartDate = DateTime.UtcNow };
        var vaccine = new Vaccine { Id = request.VaccineId, VaccineName = "Test Vaccine", RequiredDoses = 2 };
        _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(request.VaccineId))
            .ReturnsAsync(vaccine);
        _vaccineServiceMock.Setup(v => v.CanChildReceiveVaccine(request.ChildId, request.VaccineId))
            .ReturnsAsync((false, "Child is not eligible"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _appointmentService.GenerateAppointmentsForSingleVaccine(request, Guid.NewGuid()));
    }

    [Fact]
    //Test case 4: Generate Appointment For Single Vaccine When System Error
    public async Task GenerateAppointmentsForSingleVaccine_ShouldThrowException_WhenSystemErrorOccurs()
    {
        // Arrange
        var request = new CreateAppointmentSingleVaccineDto
        {
            VaccineId = Guid.NewGuid(),
            ChildId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(1)
        };
        var parentId = Guid.NewGuid();

        // Mock vaccine repository gây lỗi hệ thống
        _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(request.VaccineId))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _appointmentService.GenerateAppointmentsForSingleVaccine(request, parentId));

        Assert.Equal("Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.", exception.Message);
    }

    [Fact]
    //Test case 5: Update Appointment When Appointment Not Found
    public async Task UpdateAppointmentDate_ShouldReturnNull_WhenAppointmentNotFound()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetQueryable())
            .Returns(Enumerable.Empty<Appointment>().AsQueryable());

        // Act
        var result = await _appointmentService.UpdateAppointmentDate(appointmentId, DateTime.UtcNow.AddDays(3));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    //Test case 6: Update Appointment When Appointment Has No Valid Date
    public async Task UpdateAppointmentDate_ShouldReturnNull_WhenAppointmentHasNoValidDate()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var appointment = new Appointment { Id = appointmentId, AppointmentDate = null };

        _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetQueryable())
            .Returns(new List<Appointment> { appointment }.AsQueryable());

        // Act
        var result = await _appointmentService.UpdateAppointmentDate(appointmentId, DateTime.UtcNow.AddDays(3));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    //Test case 7: Update Appointment When Previous Appointment Not Confirmed
    public async Task UpdateAppointmentDate_ShouldReturnNull_WhenPreviousAppointmentNotConfirmed()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var appointment = new Appointment
            { Id = appointmentId, ChildId = childId, AppointmentDate = DateTime.UtcNow.AddDays(5) };
        var previousAppointment = new Appointment
        {
            Id = Guid.NewGuid(), ChildId = childId, AppointmentDate = DateTime.UtcNow.AddDays(2),
            Status = AppointmentStatus.Pending
        };

        _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetQueryable())
            .Returns(new List<Appointment> { appointment, previousAppointment }.AsQueryable());

        // Act
        var result = await _appointmentService.UpdateAppointmentDate(appointmentId, DateTime.UtcNow.AddDays(7));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    //Test case 8: Update Appointment When Vaccine Id Not Found
    public async Task UpdateAppointmentDate_ShouldReturnNull_WhenVaccineIdNotFound()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var appointment = new Appointment
        {
            Id = appointmentId, AppointmentDate = DateTime.UtcNow.AddDays(5),
            AppointmentsVaccines = new List<AppointmentsVaccine>()
        };

        _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetQueryable())
            .Returns(new List<Appointment> { appointment }.AsQueryable());

        // Act
        var result = await _appointmentService.UpdateAppointmentDate(appointmentId, DateTime.UtcNow.AddDays(7));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    //Test case 9: Update Appointment When Date Is In Past
    public async Task UpdateAppointmentDate_ShouldReturnNull_WhenNewDateIsInPast()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var appointment = new Appointment
        {
            Id = appointmentId,
            AppointmentDate = DateTime.UtcNow.AddDays(5),
            AppointmentsVaccines = new List<AppointmentsVaccine>
            {
                new() { VaccineId = Guid.NewGuid() }
            }
        };

        _unitOfWorkMock.Setup(u => u.AppointmentRepository.GetQueryable())
            .Returns(new List<Appointment> { appointment }.AsQueryable());

        // Act
        var result = await _appointmentService.UpdateAppointmentDate(appointmentId, DateTime.UtcNow.AddDays(-3));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    //Test case 10: Get List Appointment By Child Id When Child Does Not Exist
    public async Task GetListAppointmentsByChildIdAsync_ShouldThrowException_WhenChildDoesNotExist()
    {
        // Arrange
        var childId = Guid.NewGuid();

        _unitOfWorkMock.Setup(u => u.ChildRepository.GetByIdAsync(childId))
            .ReturnsAsync((Child)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _appointmentService.GetListlAppointmentsByChildIdAsync(childId));

        Assert.Equal("Child not found.", exception.Message);
    }
}