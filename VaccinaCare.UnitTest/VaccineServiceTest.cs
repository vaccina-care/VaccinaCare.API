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

        //Test case 1 : Create Vaccine Successfully
        [Fact]
        public async Task CreateVaccine_Successfully()
        {
            //Arrage
            var vaccineDTO = new VaccineDTO
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
    }
}
