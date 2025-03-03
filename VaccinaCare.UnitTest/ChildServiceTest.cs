using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Service;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;
using System.Collections.Generic;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.NotificationDTOs;
using VaccinaCare.Domain.Enums;
using Org.BouncyCastle.Asn1.Ocsp;
using VaccinaCare.Domain.DTOs.ChildDTOs;

namespace VaccinaCare.UnitTest;

public class ChildServiceTest
{
    private readonly Mock<ILoggerService> _loggerMock;
    private readonly Mock<IClaimsService> _claimsMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<INotificationService> _notificationService;
    private readonly Mock<IVaccineSuggestionService> _suggestionService;
    private readonly IChildService _childService;

    public ChildServiceTest()
    {
        _loggerMock = new Mock<ILoggerService>();
        _claimsMock = new Mock<IClaimsService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _notificationService = new Mock<INotificationService>();
        _suggestionService = new Mock<IVaccineSuggestionService>();

        _childService = new ChildService(_loggerMock.Object, _unitOfWorkMock.Object, _claimsMock.Object,
            _suggestionService.Object, _notificationService.Object);
    }

    [Fact]
    public async Task CreateChildAsync_Success()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childDto = new CreateChildDto
        {
            FullName = "John Doe",
            DateOfBirth = DateOnly.FromDateTime(new DateTime(2015, 5, 20)),
            Gender = true,
            MedicalHistory = "No major issues",
            BloodType = BloodType.A,
            HasChronicIllnesses = false,
            ChronicIllnessesDescription = "",
            HasAllergies = true,
            AllergiesDescription = "Peanuts",
            HasRecentMedication = false,
            RecentMedicationDescription = "",
            HasOtherSpecialCondition = true,
            OtherSpecialConditionDescription = "Asthma"
        };

        var parent = new User { Id = parentId };
        var child = new Child
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            FullName = childDto.FullName,
            DateOfBirth = childDto.DateOfBirth,
            Gender = childDto.Gender,
            MedicalHistory = childDto.MedicalHistory,
            BloodType = childDto.BloodType,
            HasChronicIllnesses = childDto.HasChronicIllnesses,
            ChronicIllnessesDescription = childDto.ChronicIllnessesDescription,
            HasAllergies = childDto.HasAllergies,
            AllergiesDescription = childDto.AllergiesDescription,
            HasRecentMedication = childDto.HasRecentMedication,
            RecentMedicationDescription = childDto.RecentMedicationDescription,
            HasOtherSpecialCondition = childDto.HasOtherSpecialCondition,
            OtherSpecialConditionDescription = childDto.OtherSpecialConditionDescription
        };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);
        _unitOfWorkMock.Setup(u => u.UserRepository.GetByIdAsync(parentId)).ReturnsAsync(parent);
        _unitOfWorkMock.Setup(u => u.ChildRepository.AddAsync(It.IsAny<Child>())).ReturnsAsync(child);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _notificationService
            .Setup(n => n.PushNotificationWhenUserUseService(It.IsAny<Guid>(), It.IsAny<NotificationForUserDTO>()))
            .ReturnsAsync(new Notification
            {
                Id = Guid.NewGuid(),
                Title = "Child Profile Created!",
                Content = "Your child's profile has been successfully created.",
                Url = "",
                UserId = parentId
            });

        // Act
        var result = await _childService.CreateChildAsync(childDto);

        Assert.NotNull(result);
        Assert.Equal(childDto.FullName, result.FullName);
        Assert.Equal(childDto.DateOfBirth, result.DateOfBirth);
        Assert.Equal(childDto.Gender, result.Gender);
        Assert.Equal(childDto.MedicalHistory, result.MedicalHistory);
        Assert.Equal(childDto.BloodType, result.BloodType);
        Assert.Equal(childDto.HasChronicIllnesses, result.HasChronicIllnesses);
        Assert.Equal(childDto.ChronicIllnessesDescription, result.ChronicIllnessesDescription);
        Assert.Equal(childDto.HasAllergies, result.HasAllergies);
        Assert.Equal(childDto.AllergiesDescription, result.AllergiesDescription);
        Assert.Equal(childDto.HasRecentMedication, result.HasRecentMedication);
        Assert.Equal(childDto.RecentMedicationDescription, result.RecentMedicationDescription);
        Assert.Equal(childDto.HasOtherSpecialCondition, result.HasOtherSpecialCondition);
        Assert.Equal(childDto.OtherSpecialConditionDescription, result.OtherSpecialConditionDescription);

        // Kiểm tra gọi đúng số lần
        _unitOfWorkMock.Verify(u => u.ChildRepository.AddAsync(It.IsAny<Child>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notificationService.Verify(
            n => n.PushNotificationWhenUserUseService(It.IsAny<Guid>(), It.IsAny<NotificationForUserDTO>()),
            Times.Once);
    }
}