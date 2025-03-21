using System.Linq.Expressions;
using System.Text;
using Microsoft.AspNetCore.Http;
using Moq;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Service;
using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.UnitTest;

public class VaccineServiceTest
{
    private readonly Mock<IClaimsService> _claimsMock;
    private readonly Mock<IBlobService> _lobMock;
    private readonly Mock<ILoggerService> _loggerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IVaccineService _vaccineService;

    public VaccineServiceTest()
    {
        _loggerMock = new Mock<ILoggerService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _claimsMock = new Mock<IClaimsService>();
        _lobMock = new Mock<IBlobService>();
        _vaccineService =
            new VaccineService(_unitOfWorkMock.Object, _loggerMock.Object, _claimsMock.Object, _lobMock.Object);
    }

    [Fact]
    //Test case 1 : Create Vaccine Successfully
    public async Task CreateVaccine_Successfully()
    {
        //Arrange
        var vaccineDTO = new CreateVaccineDto
        {
            VaccineName = "Test Vaccine",
            Description = "Test Description",
            Type = "COVID-19",
            Price = 100,
            RequiredDoses = 2
        };

        var vaccine = new Vaccine
        {
            VaccineName = vaccineDTO.VaccineName,
            Description = vaccineDTO.Description,
            Type = vaccineDTO.Type,
            Price = vaccineDTO.Price
        };

        //Mock IFormFile
        var fileMock = new Mock<IFormFile>();
        var content = "Fake image content";
        var fileName = "testImage.jpg";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        fileMock.Setup(_ => _.OpenReadStream()).Returns(stream);
        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(stream.Length);

        var mockFile = fileMock.Object;

        _unitOfWorkMock.Setup(u => u.VaccineRepository.AddAsync(It.IsAny<Vaccine>()))
            .ReturnsAsync((Vaccine v) => v);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _lobMock.Setup(b => b.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>()))
            .Returns(Task.CompletedTask);
        _lobMock.Setup(b => b.GetFileUrlAsync(It.IsAny<string>()))
            .ReturnsAsync("https://test.com/image.jpg");

        // Act

        var result = await _vaccineService.CreateVaccine(vaccineDTO, mockFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(vaccineDTO.VaccineName, result.VaccineName);
        Assert.Equal(vaccineDTO.Description, result.Description);
        Assert.Equal(vaccineDTO.Type, result.Type);
        Assert.Equal(vaccineDTO.Price, result.Price);
        Assert.Equal("https://test.com/image.jpg", result.PicUrl);

        _unitOfWorkMock.Verify(u => u.VaccineRepository.AddAsync(It.IsAny<Vaccine>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _lobMock.Verify(b => b.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>()), Times.Once);
    }

    [Fact]
    // Test case 2 : Create Vaccine - Missing Required Fields
    public async Task CreateVaccine_MissingRequiredFields()
    {
        // Arrange
        var vaccineDTO = new CreateVaccineDto
        {
            VaccineName = "",
            Description = "Test Description",
            Type = "COVID-19",
            Price = 100
        };

        // Mock an empty file 
        var fileMock = new Mock<IFormFile>();
        var fileName = "testImage.jpg";
        var stream = new MemoryStream();

        fileMock.Setup(_ => _.OpenReadStream()).Returns(stream);
        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(0);

        var mockFile = fileMock.Object;

        // Act & Assert
        var result = await _vaccineService.CreateVaccine(vaccineDTO, mockFile);
        Assert.Null(result);
    }

    [Fact]
    // Test case 3 : Create Vaccine - Negative Price
    public async Task CreateVaccine_NegativePrice_ReturnsNull()
    {
        // Arrange
        var vaccineDTO = new CreateVaccineDto
        {
            VaccineName = "COVID-19 Vaccine",
            Description = "Effective for COVID-19",
            Type = "COVID-19",
            Price = -10
        };

        // Mock an empty file 
        var fileMock = new Mock<IFormFile>();
        var fileName = "covid19pic.jpg";
        var stream = new MemoryStream(new byte[1]);

        fileMock.Setup(_ => _.OpenReadStream()).Returns(stream);
        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(1);

        var mockFile = fileMock.Object;

        // Act
        var result = await _vaccineService.CreateVaccine(vaccineDTO, mockFile);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    // Test case 4 : Update Vaccine - UpdateVaccineDto Null
    public async Task UpdateVaccine_VaccineDTOIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var vaccineId = Guid.NewGuid();
        UpdateVaccineDto updateDto = null;
        IFormFile? vaccinePictureFile = null;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _vaccineService.UpdateVaccine(vaccineId, updateDto, vaccinePictureFile)
        );

        Assert.Equal("updateDto", exception.ParamName);
    }

    [Fact]
    // Test case 5 : Update Vaccine - Vaccine Not Found
    public async Task UpdateVaccine_VaccineNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var vaccineId = Guid.NewGuid();
        var updateDto = new UpdateVaccineDto
        {
            VaccineName = "COVID-19 Updated Vaccine",
            Description = "Updated Description",
            Type = "COVID-19",
            Price = 150
        };
        IFormFile? vaccinePictureFile = null;

        _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(vaccineId))
            .ReturnsAsync((Vaccine?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _vaccineService.UpdateVaccine(vaccineId, updateDto, vaccinePictureFile)
        );

        Assert.Equal($"Vaccine with ID {vaccineId} not found.", exception.Message);
    }

    [Fact]
    // Test case 6 : Update Vaccine - Valid VaccineDTO
    public async Task UpdateVaccine_ValidVaccineDTO_ReturnsUpdatedVaccineDTO()
    {
        // Arrange
        var vaccineId = Guid.NewGuid();
        var existingVaccine = new Vaccine
        {
            Id = vaccineId,
            VaccineName = "COVID-19 Vaccine",
            Description = "Old Description",
            PicUrl = "oldpic.jpg",
            Type = "COVID-19",
            Price = 100
        };

        var updateDto = new UpdateVaccineDto
        {
            VaccineName = "COVID-19 Updated Vaccine",
            Description = "Updated Description",
            Type = "COVID-19",
            Price = 150
        };

        IFormFile? vaccinePictureFile = null;

        _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(vaccineId))
            .ReturnsAsync(existingVaccine);
        _unitOfWorkMock.Setup(u => u.VaccineRepository.Update(It.IsAny<Vaccine>())).Verifiable();
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _vaccineService.UpdateVaccine(vaccineId, updateDto, vaccinePictureFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateDto.VaccineName, result.VaccineName);
        Assert.Equal(updateDto.Description, result.Description);
        Assert.Equal(existingVaccine.PicUrl, result.PicUrl);
        Assert.Equal(updateDto.Type, result.Type);
        Assert.Equal(updateDto.Price, result.Price);
    }

    [Fact]
    // Test case 7 : Update Vaccine - Partial Update (Some Fields Null)
    public async Task UpdateVaccine_PartialUpdate_ReturnsUpdatedVaccineDTO()
    {
        // Arrange
        var vaccineId = Guid.NewGuid();
        var existingVaccine = new Vaccine
        {
            Id = vaccineId,
            VaccineName = "COVID-19 Vaccine",
            Description = "Old Description",
            PicUrl = "oldpic.jpg",
            Type = "COVID-19",
            Price = 100
        };

        var updateDto = new UpdateVaccineDto
        {
            VaccineName = "",
            Description = "Updated Description",
            Type = "",
            Price = 150
        };

        IFormFile? vaccinePictureFile = null;

        _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(vaccineId))
            .ReturnsAsync(existingVaccine);
        _unitOfWorkMock.Setup(u => u.VaccineRepository.Update(It.IsAny<Vaccine>())).Verifiable();
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _vaccineService.UpdateVaccine(vaccineId, updateDto, vaccinePictureFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingVaccine.VaccineName, result.VaccineName);
        Assert.Equal(updateDto.Description, result.Description);
        Assert.Equal(existingVaccine.PicUrl, result.PicUrl);
        Assert.Equal(existingVaccine.Type, result.Type);
        Assert.Equal(updateDto.Price, result.Price);
    }

    [Fact]
    // Test case 8: Update Vaccine - Database Error
    public async Task UpdateVaccine_DatabaseError_ThrowsException()
    {
        // Arrange
        var vaccineId = Guid.NewGuid();
        var existingVaccine = new Vaccine
        {
            Id = vaccineId,
            VaccineName = "COVID-19 Vaccine",
            Description = "Old Description",
            PicUrl = "oldpic.jpg",
            Type = "COVID-19",
            Price = 100
        };

        var updateDto = new UpdateVaccineDto
        {
            VaccineName = "COVID-19 Updated Vaccine",
            Description = "Updated Description",
            PicUrl = "updatedpic.jpg",
            Type = "COVID-19",
            Price = 150
        };

        var mockFile = new Mock<IFormFile>(); // Mock file ảnh
        mockFile.Setup(f => f.Length).Returns(1024); // Giả sử file có kích thước 1KB
        mockFile.Setup(f => f.FileName).Returns("vaccine.jpg");
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[1024]));

        _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(vaccineId))
            .ReturnsAsync(existingVaccine);

        // Simulate database error on SaveChangesAsync
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Mock blob service nhưng không cần gọi thực sự
        _lobMock.Setup(b => b.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>()))
            .Returns(Task.CompletedTask);
        _lobMock.Setup(b => b.GetFileUrlAsync(It.IsAny<string>()))
            .ReturnsAsync("newpic.jpg");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _vaccineService.UpdateVaccine(vaccineId, updateDto, mockFile.Object));

        // Verify the correct error message
        Assert.Equal("Database error", exception.Message);
    }

    [Fact]
    // Test case 9 : Delete Vaccine - Successfully
    public async Task DeleteVaccine_Successfully()
    {
        // Arrange
        var vaccineId = Guid.NewGuid();
        var vaccine = new Vaccine
        {
            Id = vaccineId,
            VaccineName = "Pfizer",
            Description = "Effective against COVID-19",
            PicUrl = "testpicurl.jpg",
            Type = "COVID-19",
            Price = 100,
            IsDeleted = false,
            RequiredDoses = 2,
            ForBloodType = BloodType.O,
            AvoidChronic = true,
            AvoidAllergy = false,
            HasDrugInteraction = false,
            HasSpecialWarning = true
        };

        var vaccinePackageDetails = new List<VaccinePackageDetail>
        {
            new() { Id = Guid.NewGuid(), VaccineId = vaccineId },
            new() { Id = Guid.NewGuid(), VaccineId = vaccineId }
        };

        _unitOfWorkMock
            .Setup(u => u.VaccineRepository.GetByIdAsync(It.Is<Guid>(id => id == vaccineId),
                It.IsAny<Expression<Func<Vaccine, object>>[]>())).ReturnsAsync(vaccine);

        _unitOfWorkMock
            .Setup(u =>
                u.VaccinePackageDetailRepository.GetAllAsync(It.IsAny<Expression<Func<VaccinePackageDetail, bool>>>()))
            .ReturnsAsync(vaccinePackageDetails);

        _unitOfWorkMock
            .Setup(u => u.VaccinePackageDetailRepository.SoftRemoveRange(
                It.Is<List<VaccinePackageDetail>>(list => list.All(v => v.VaccineId == vaccineId)))).ReturnsAsync(true);

        _unitOfWorkMock.Setup(u => u.VaccineRepository.SoftRemove(It.Is<Vaccine>(v => v.Id == vaccineId)))
            .ReturnsAsync(true);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _vaccineService.DeleteVaccine(vaccineId);

        // Assert
        _unitOfWorkMock.Verify(
            u => u.VaccineRepository.GetByIdAsync(It.Is<Guid>(id => id == vaccineId),
                It.IsAny<Expression<Func<Vaccine, object>>[]>()), Times.Once);
        _unitOfWorkMock.Verify(
            u => u.VaccinePackageDetailRepository.GetAllAsync(It.IsAny<Expression<Func<VaccinePackageDetail, bool>>>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            u => u.VaccinePackageDetailRepository.SoftRemoveRange(
                It.Is<List<VaccinePackageDetail>>(list => list.All(v => v.VaccineId == vaccineId))), Times.Once);
        _unitOfWorkMock.Verify(u => u.VaccineRepository.SoftRemove(It.Is<Vaccine>(v => v.Id == vaccineId)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);

        Assert.NotNull(result);
        Assert.Equal(vaccine.VaccineName, result.VaccineName);
        Assert.Equal(vaccine.Description, result.Description);
        Assert.Equal(vaccine.PicUrl, result.PicUrl);
        Assert.Equal(vaccine.Type, result.Type);
        Assert.Equal(vaccine.Price, result.Price);
        Assert.Equal(vaccine.RequiredDoses, result.RequiredDoses);
        Assert.Equal(vaccine.ForBloodType, result.ForBloodType);
        Assert.Equal(vaccine.AvoidChronic, result.AvoidChronic);
        Assert.Equal(vaccine.AvoidAllergy, result.AvoidAllergy);
        Assert.Equal(vaccine.HasDrugInteraction, result.HasDrugInteraction);
        Assert.Equal(vaccine.HasSpecialWarning, result.HasSpecialWarning);
    }

    [Fact]
    //Test case 10 : Delete Vaccine - Not Found
    public async Task DeleteVaccine_VaccineNotFound()
    {
        // Arrange
        var vaccineId = Guid.NewGuid();

        _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(vaccineId)).ReturnsAsync((Vaccine)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _vaccineService.DeleteVaccine(vaccineId));
        Assert.Equal($"Vaccine with ID {vaccineId} not found or already deleted.", exception.Message);
    }

    [Fact]
    //Test case 11 : Delete Vaccine - Vaccine Already Deleted
    public async Task DeleteVaccine_VaccineAlreadyDeleted()
    {
        // Arrange
        var vaccineId = Guid.NewGuid();
        var vaccine = new Vaccine
        {
            Id = vaccineId,
            VaccineName = "Test Vaccine",
            Description = "Test Description",
            PicUrl = "testpicurl.jpg",
            Type = "COVID-19",
            Price = 100,
            IsDeleted = true
        };

        _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(vaccineId)).ReturnsAsync(vaccine);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _vaccineService.DeleteVaccine(vaccineId));
        Assert.Equal($"Vaccine with ID {vaccineId} not found or already deleted.", exception.Message);
    }

    [Fact]
    //Test case 12 : Delete Vaccine - Have Exception
    public async Task DeleteVaccine_ExceptionInDeletionProcess()
    {
        // Arrange
        var vaccineId = Guid.NewGuid();
        var vaccine = new Vaccine
        {
            Id = vaccineId,
            VaccineName = "Test Vaccine",
            Description = "Test Description",
            PicUrl = "testpicurl.jpg",
            Type = "COVID-19",
            Price = 100,
            IsDeleted = false
        };

        _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(vaccineId)).ReturnsAsync(vaccine);
        _unitOfWorkMock.Setup(u => u.VaccineRepository.SoftRemove(It.IsAny<Vaccine>()))
            .ThrowsAsync(new Exception($"Vaccine with ID {vaccineId} not found or already deleted."));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _vaccineService.DeleteVaccine(vaccineId));
        Assert.Equal($"Vaccine with ID {vaccineId} not found or already deleted.", exception.Message);
    }

    [Fact]
    //Test case 13 : Delete Vaccine - Fails
    public async Task DeleteVaccine_SoftRemoveFails()
    {
        // Arrange
        var vaccineId = Guid.NewGuid();
        var vaccine = new Vaccine
        {
            Id = vaccineId,
            VaccineName = "Test Vaccine",
            Description = "Test Description",
            PicUrl = "testpicurl.jpg",
            Type = "COVID-19",
            Price = 100,
            IsDeleted = false
        };

        _unitOfWorkMock.Setup(u =>
                u.VaccineRepository.GetByIdAsync(vaccineId, It.IsAny<Expression<Func<Vaccine, object>>[]>()))
            .ReturnsAsync(vaccine);

        _unitOfWorkMock.Setup(u =>
                u.VaccinePackageDetailRepository.GetAllAsync(It.IsAny<Expression<Func<VaccinePackageDetail, bool>>>()))
            .ReturnsAsync(new List<VaccinePackageDetail>());

        _unitOfWorkMock.Setup(u => u.VaccineRepository.SoftRemove(It.IsAny<Vaccine>()))
            .ReturnsAsync(false);

        // Act
        var result = await _vaccineService.DeleteVaccine(vaccineId);

        // Assert
        Assert.Null(result);
    }
}