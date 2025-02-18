using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Service;
using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.UnitTest
{
    public class VaccineServiceTest
    {
        private readonly Mock<ILoggerService> _loggerMock;
        private readonly IVaccineService _vaccineService;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IClaimsService> _claimsMock;

        public VaccineServiceTest()
        {
            _loggerMock = new Mock<ILoggerService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _claimsMock = new Mock<IClaimsService>();

            _vaccineService = new VaccineService(_unitOfWorkMock.Object, _loggerMock.Object, _claimsMock.Object);
        }


        [Fact]
        //Test case 1 : Create Vaccine Successfully
        public async Task CreateVaccine_Successfully()
        {
            //Arrange
            var vaccineDTO = new CreateVaccineDTO()
            {
                VaccineName = "Test Vaccine",
                Description = "Test Description",
                PicUrl = "testpicurl.jpg",
                Type = "COVID-19",
                Price = 100
            };

            var vaccine = new Vaccine
            {
                VaccineName = vaccineDTO.VaccineName,
                Description = vaccineDTO.Description,
                Type = vaccineDTO.Type,
                Price = vaccineDTO.Price,
                PicUrl = vaccineDTO.PicUrl
            };

            _unitOfWorkMock.Setup(u => u.VaccineRepository.AddAsync(It.IsAny<Vaccine>()))
     .ReturnsAsync((Vaccine v) => v);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act

            var result = await _vaccineService.CreateVaccine(vaccineDTO);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vaccineDTO.VaccineName, result.VaccineName);
            Assert.Equal(vaccineDTO.Description, result.Description);
            Assert.Equal(vaccineDTO.Type, result.Type);
            Assert.Equal(vaccineDTO.Price, result.Price);
            Assert.Equal(vaccineDTO.PicUrl, result.PicUrl);

            _unitOfWorkMock.Verify(u => u.VaccineRepository.AddAsync(It.IsAny<Vaccine>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
        [Fact]
        //Test case 2 : Create Vaccine - Missing Required Fields
        public async Task CreateVaccine_MissingRequiredFields()
        {
            //Arrange
            var vaccineDTO = new CreateVaccineDTO
            {
                VaccineName = "", //Missing VaccineName
                Description = "Test Description",
                PicUrl = "testpicurl.jpg",
                Type = "COVID-19",
                Price = 100
            };

            //Act
            var result = await _vaccineService.CreateVaccine(vaccineDTO);

            //Assert
            Assert.Null(result);

        }
        [Fact]
        //Test case 3 : Create Vaccine - Negative Price
        public async Task CreateVaccine_NegativePrice_ReturnsNull()
        {
            //Arrange
            var vaccineDTO = new CreateVaccineDTO
            {
                VaccineName = "COVID-19 Vaccine",
                Description = "Effective for COVID-19",
                PicUrl = "covid19pic.jpg",
                Type = "COVID-19",
                Price = -10 //Price < 0
            };
            //Act
            var result = await _vaccineService.CreateVaccine(vaccineDTO);
            //Assert
            Assert.Null(result);
        }
        [Fact]
        //Test case 4 : Update Vaccine - VaccineDTO Null
        public async Task UpdateVaccine_VaccineDTOIsNull_ThrowsArgumentNullException()
        {
            //Arrange
            Guid vaccineId = Guid.NewGuid();
            VaccineDTO vaccineDTO = null; // VaccineDTO is null

            //Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _vaccineService.UpdateVaccine(vaccineId, vaccineDTO));
            Assert.Equal("Value cannot be null.", exception.Message);
        }
        [Fact]
        //Test case 5 : Update Vaccine - VaccineDTO Not Found
        public async Task UpdateVaccine_VaccineNotFound_ThrowsKeyNotFoundException()
        {
            //Arrange
            Guid vaccineId = Guid.NewGuid();
            var vaccineDTO = new VaccineDTO
            {
                VaccineName = "COVID-19 Updated Vaccine",
                Description = "Updated Description",
                PicUrl = "updatedpic.jpg",
                Type = "COVID-19",
                Price = 150
            };

            _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(vaccineId)).ReturnsAsync((Vaccine?)null);

            //Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _vaccineService.UpdateVaccine(vaccineId, vaccineDTO));
            Assert.Equal($"Vaccine with ID {vaccineId} not found.", exception.Message);
        }
        [Fact]
        //Test case 6 : Update Vaccine - VaccineDTO Valid
        public async Task UpdateVaccine_ValidVaccineDTO_ReturnsUpdatedVaccineDTO()
        {
            //Arrange
            Guid vaccineId = Guid.NewGuid();
            var existingVaccine = new Vaccine
            {
                Id = vaccineId,
                VaccineName = "COVID-19 Vaccine",
                Description = "Old Description",
                PicUrl = "oldpic.jpg",
                Type = "COVID-19",
                Price = 100

            };

            var vaccineDTO = new VaccineDTO
            {
                VaccineName = "COVID-19 Updated Vaccine",
                Description = "Updated Description",
                PicUrl = "updatedpic.jpg",
                Type = "COVID-19",
                Price = 150
            };

            _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(vaccineId)).ReturnsAsync(existingVaccine);
            _unitOfWorkMock.Setup(u => u.VaccineRepository.Update(It.IsAny<Vaccine>())).Verifiable();
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            //Act
            var result = await _vaccineService.UpdateVaccine(vaccineId, vaccineDTO);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(vaccineDTO.VaccineName, result.VaccineName);
            Assert.Equal(vaccineDTO.Description, result.Description);
            Assert.Equal(vaccineDTO.PicUrl, result.PicUrl);
            Assert.Equal(vaccineDTO.Type, result.Type);
            Assert.Equal(vaccineDTO.Price, result.Price);
        }
        [Fact]
        //Test case 7 : Update Vaccine - VaccineDTO Have Null Required
        public async Task UpdateVaccine_PartialUpdate_ReturnsUpdatedVaccineDTO()
        {
            // Arrange
            Guid vaccineId = Guid.NewGuid();
            var existingVaccine = new Vaccine
            {
                Id = vaccineId,
                VaccineName = "COVID-19 Vaccine",
                Description = "Old Description",
                PicUrl = "oldpic.jpg",
                Type = "COVID-19",
                Price = 100
            };

            var vaccineDTO = new VaccineDTO
            {
                VaccineName = "",
                Description = "Updated Description",
                PicUrl = "",
                Type = "",
                Price = 150
            };

            _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(vaccineId))
                           .ReturnsAsync(existingVaccine);
            _unitOfWorkMock.Setup(u => u.VaccineRepository.Update(It.IsAny<Vaccine>())).Verifiable();
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _vaccineService.UpdateVaccine(vaccineId, vaccineDTO);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingVaccine.VaccineName, result.VaccineName);
            Assert.Equal(vaccineDTO.Description, result.Description);
            Assert.Equal(existingVaccine.PicUrl, result.PicUrl);
            Assert.Equal(existingVaccine.Type, result.Type);
            Assert.Equal(vaccineDTO.Price, result.Price);
        }
        [Fact]
        //Test case 8 : Update Vaccine - Negavite Price
        public async Task UpdateVaccine_NegativePrice_ReturnsUpdatedVaccineDTO()
        {
            // Arrange
            Guid vaccineId = Guid.NewGuid();
            var existingVaccine = new Vaccine
            {
                Id = vaccineId,
                VaccineName = "COVID-19 Vaccine",
                Description = "Old Description",
                PicUrl = "oldpic.jpg",
                Type = "COVID-19",
                Price = 100
            };

            var vaccineDTO = new VaccineDTO
            {
                VaccineName = "COVID-19 Updated Vaccine",
                Description = "Updated Description",
                PicUrl = "updatedpic.jpg",
                Type = "COVID-19",
                Price = -10
            };

            _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(vaccineId))
                   .ReturnsAsync(existingVaccine);
            _unitOfWorkMock.Setup(u => u.VaccineRepository.Update(It.IsAny<Vaccine>())).Verifiable();
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _vaccineService.UpdateVaccine(vaccineId, vaccineDTO);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingVaccine.Price, result.Price);
        }
        [Fact]
        //Test case 9 : Update Vaccine - Database Error
        public async Task UpdateVaccine_DatabaseError_ThrowsException()
        {
            // Arrange
            Guid vaccineId = Guid.NewGuid();
            var vaccineDTO = new VaccineDTO
            {
                VaccineName = "COVID-19 Updated Vaccine",
                Description = "Updated Description",
                PicUrl = "updatedpic.jpg",
                Type = "COVID-19",
                Price = 150
            };

            _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(vaccineId))
                           .ReturnsAsync(new Vaccine { Id = vaccineId });
            _unitOfWorkMock.Setup(u => u.VaccineRepository.Update(It.IsAny<Vaccine>())).ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _vaccineService.UpdateVaccine(vaccineId, vaccineDTO));
            Assert.Equal("Database error", exception.Message);
        }
        [Fact]
        //Test case 10 : Delete Vaccine - Successfully
        public async Task DeleteVaccine_Successfully()
        {
            //Arrange
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
            _unitOfWorkMock.Setup(u => u.VaccineRepository.SoftRemove(It.IsAny<Vaccine>())).ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _vaccineService.DeleteVaccine(vaccineId);

            // Assert
            _unitOfWorkMock.Verify(u => u.VaccineRepository.SoftRemove(It.IsAny<Vaccine>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
            Assert.NotNull(result);
            Assert.Equal(vaccine.VaccineName, result.VaccineName);
        }
        [Fact]
        //Test case 11 : Delete Vaccine - Not Found
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
        //Test case 12 : Delete Vaccine - Vaccine Already Deleted
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
        //Test case 13 : Delete Vaccine - Have Exception
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
            _unitOfWorkMock.Setup(u => u.VaccineRepository.SoftRemove(It.IsAny<Vaccine>())).ThrowsAsync(new Exception("Deletion failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _vaccineService.DeleteVaccine(vaccineId));
            Assert.Equal("Deletion failed", exception.Message);
        }
        [Fact]
        //Test case 14 : Delete Vaccine - Fails
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

            _unitOfWorkMock.Setup(u => u.VaccineRepository.GetByIdAsync(vaccineId)).ReturnsAsync(vaccine);
            _unitOfWorkMock.Setup(u => u.VaccineRepository.SoftRemove(It.IsAny<Vaccine>())).ReturnsAsync(false); 
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _vaccineService.DeleteVaccine(vaccineId);

            // Assert
            Assert.Null(result); 
        }


    }
}
