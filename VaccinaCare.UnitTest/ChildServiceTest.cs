using Moq;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Service;
using VaccinaCare.Domain.DTOs.ChildDTOs;
using VaccinaCare.Domain.DTOs.NotificationDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.UnitTest;

public class ChildServiceTest
{
    private readonly ChildService _childService;
    private readonly Mock<IClaimsService> _claimsMock;
    private readonly Mock<ILoggerService> _loggerMock;
    private readonly Mock<INotificationService> _notificationService;
    private readonly Mock<IVaccineSuggestionService> _suggestionService;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

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
    //Test case 1: Create Child Success
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

    [Fact]
    //Test case 2: Create Child When Parent Does Not Exist
    public async Task CreateChildAsync_ShouldThrowKeyNotFoundException_WhenParentDoesNotExist()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childDto = new CreateChildDto
            { FullName = "John Doe", DateOfBirth = DateOnly.FromDateTime(new DateTime(2015, 5, 20)), Gender = true };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);
        _unitOfWorkMock.Setup(u => u.UserRepository.GetByIdAsync(parentId)).ReturnsAsync((User)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _childService.CreateChildAsync(childDto));

        _loggerMock.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains("does not exist"))), Times.Once);
    }

    [Fact]
    //Test case 3: Create Child When Save Changes Fails
    public async Task CreateChildAsync_ShouldThrowException_WhenSaveChangesFails()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childDto = new CreateChildDto
            { FullName = "John Doe", DateOfBirth = DateOnly.FromDateTime(new DateTime(2015, 5, 20)), Gender = true };
        var parent = new User { Id = parentId };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);
        _unitOfWorkMock.Setup(u => u.UserRepository.GetByIdAsync(parentId)).ReturnsAsync(parent);
        _unitOfWorkMock.Setup(u => u.ChildRepository.AddAsync(It.IsAny<Child>())).ReturnsAsync((Child child) => child);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _childService.CreateChildAsync(childDto));

        _loggerMock.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Database error"))), Times.Once);
    }

    [Fact]
    //Test case 4: Create Child When Notification Have Fails
    public async Task CreateChildAsync_ShouldFail_WhenNotificationFails()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childDto = new CreateChildDto
            { FullName = "Test Child", DateOfBirth = DateOnly.FromDateTime(new DateTime(2015, 5, 20)) };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);
        _unitOfWorkMock.Setup(u => u.UserRepository.GetByIdAsync(parentId))
            .ReturnsAsync(new User { Id = parentId });

        _unitOfWorkMock.Setup(u => u.ChildRepository.AddAsync(It.IsAny<Child>())).ReturnsAsync((Child child) => child);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _notificationService
            .Setup(n => n.PushNotificationWhenUserUseService(It.IsAny<Guid>(), It.IsAny<NotificationForUserDTO>()))
            .ThrowsAsync(new Exception("Notification service failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => { await _childService.CreateChildAsync(childDto); });
    }

    [Fact]
    //Test case 5: Get Children When Have Error
    public async Task GetChildrenByParentAsync_ShouldThrowException_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);

        _unitOfWorkMock
            .Setup(u => u.ChildRepository.GetQueryable())
            .Throws(new Exception("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _childService.GetChildrenByParentAsync());

        Assert.Equal("An error occurred while fetching children. Please try again later.", exception.Message);
        _loggerMock.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Error while fetching children"))),
            Times.Once);
    }

    [Fact]
    //Test case 6: Delete Successfully
    public async Task DeleteChildrenByParentIdAsync_ShouldDeleteSuccessfully_WhenChildExistsAndBelongsToParent()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var child = new Child { Id = childId, ParentId = parentId };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);
        _unitOfWorkMock.Setup(u => u.ChildRepository.GetByIdAsync(childId)).ReturnsAsync(child);
        _unitOfWorkMock.Setup(u => u.ChildRepository.SoftRemove(It.IsAny<Child>()))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _childService.DeleteChildrenByParentIdAsync(childId);

        // Assert
        _unitOfWorkMock.Verify(u => u.ChildRepository.SoftRemove(It.Is<Child>(c => c.Id == childId)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _loggerMock.Verify(l => l.Success(It.Is<string>(msg => msg.Contains("successfully deleted"))), Times.Once);
    }

    [Fact]
    //Test case 7: Delete Children Does Not Exist
    public async Task DeleteChildrenByParentIdAsync_ShouldThrowKeyNotFoundException_WhenChildDoesNotExist()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);
        _unitOfWorkMock.Setup(u => u.ChildRepository.GetByIdAsync(childId)).ReturnsAsync((Child)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _childService.DeleteChildrenByParentIdAsync(childId));

        _loggerMock.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains("not found or does not belong"))), Times.Once);
    }

    [Fact]
    //Test case 8: Delete Childen When Child Does Not Be Long To Parent
    public async Task DeleteChildrenByParentIdAsync_ShouldThrowKeyNotFoundException_WhenChildDoesNotBelongToParent()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var anotherParentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var child = new Child { Id = childId, ParentId = anotherParentId };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);
        _unitOfWorkMock.Setup(u => u.ChildRepository.GetByIdAsync(childId)).ReturnsAsync(child);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _childService.DeleteChildrenByParentIdAsync(childId));

        _loggerMock.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains("not found or does not belong"))), Times.Once);
    }

    [Fact]
    //Test case 9: Delete Children When Unexpected Error
    public async Task DeleteChildrenByParentIdAsync_ShouldThrowException_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var child = new Child { Id = childId, ParentId = parentId };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);
        _unitOfWorkMock.Setup(u => u.ChildRepository.GetByIdAsync(childId)).ReturnsAsync(child);
        _unitOfWorkMock.Setup(u => u.ChildRepository.SoftRemove(child)).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _childService.DeleteChildrenByParentIdAsync(childId));

        _loggerMock.Verify(l => l.Error(It.Is<string>(msg => msg.Contains("Error deleting child profile"))),
            Times.Once);
    }

    [Fact]
    //Test case 10: Update Children Successfully
    public async Task UpdateChildrenAsync_ShouldUpdateSuccessfully_WhenValidDataProvided()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var childDto = new UpdateChildDto { FullName = "Updated Name", Gender = true };
        var existingChild = new Child { Id = childId, ParentId = parentId, FullName = "Old Name", Gender = false };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);
        _unitOfWorkMock.Setup(u => u.UserRepository.GetByIdAsync(parentId)).ReturnsAsync(new User());
        _unitOfWorkMock.Setup(u => u.ChildRepository.GetByIdAsync(childId)).ReturnsAsync(existingChild);
        _unitOfWorkMock.Setup(u => u.ChildRepository.Update(It.IsAny<Child>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _childService.UpdateChildrenAsync(childId, childDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(childDto.FullName, result.FullName);
        Assert.Equal(childDto.Gender, result.Gender);
    }

    [Fact]
    //Test case 11: Update Children When Parent Does Not Exist
    public async Task UpdateChildrenAsync_ShouldThrowKeyNotFoundException_WhenParentDoesNotExist()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var childDto = new UpdateChildDto { FullName = "Updated Name" };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);
        _unitOfWorkMock.Setup(u => u.UserRepository.GetByIdAsync(parentId)).ReturnsAsync((User)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _childService.UpdateChildrenAsync(childId, childDto));
    }

    [Fact]
    //Test case 12: Update Children When Children Does Not Exist
    public async Task UpdateChildrenAsync_ShouldThrowKeyNotFoundException_WhenChildDoesNotExist()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var childDto = new UpdateChildDto { FullName = "Updated Name" };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);
        _unitOfWorkMock.Setup(u => u.UserRepository.GetByIdAsync(parentId)).ReturnsAsync(new User());
        _unitOfWorkMock.Setup(u => u.ChildRepository.GetByIdAsync(childId)).ReturnsAsync((Child)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _childService.UpdateChildrenAsync(childId, childDto));
    }

    [Fact]
    //Test case 13: Update Children When Children Does Not Be Long To Parent
    public async Task UpdateChildrenAsync_ShouldThrowKeyNotFoundException_WhenChildDoesNotBelongToParent()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var anotherParentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var childDto = new UpdateChildDto { FullName = "Updated Name" };
        var existingChild = new Child { Id = childId, ParentId = anotherParentId };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);
        _unitOfWorkMock.Setup(u => u.UserRepository.GetByIdAsync(parentId)).ReturnsAsync(new User());
        _unitOfWorkMock.Setup(u => u.ChildRepository.GetByIdAsync(childId)).ReturnsAsync(existingChild);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _childService.UpdateChildrenAsync(childId, childDto));
    }

    [Fact]
    //Test case 14: Update Children When Some Fields Are Null
    public async Task UpdateChildrenAsync_ShouldUpdatePartially_WhenSomeFieldsAreNull()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var childDto = new UpdateChildDto { FullName = "Updated Name" };
        var existingChild = new Child { Id = childId, ParentId = parentId, FullName = "Old Name", Gender = true };

        _claimsMock.Setup(c => c.GetCurrentUserId).Returns(parentId);
        _unitOfWorkMock.Setup(u => u.UserRepository.GetByIdAsync(parentId)).ReturnsAsync(new User());
        _unitOfWorkMock.Setup(u => u.ChildRepository.GetByIdAsync(childId)).ReturnsAsync(existingChild);
        _unitOfWorkMock.Setup(u => u.ChildRepository.Update(It.IsAny<Child>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _childService.UpdateChildrenAsync(childId, childDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(childDto.FullName, result.FullName);
        Assert.Equal(existingChild.Gender, result.Gender);
    }
}