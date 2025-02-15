using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;

namespace VaccinaCare.API.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FileController : ControllerBase
    {
        private readonly IBlobService _blobService;

        public FileController(IBlobService blobService)
        {
            _blobService = blobService;
        }


        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is not valid");

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    await _blobService.UploadFileAsync(file.FileName, stream);
                }

                var fileUrl = _blobService.GetFileUrlAsync(file.FileName);

                return Ok(new
                {
                    message = " Upload successfully",
                    fileName = file.FileName,
                    fileUrl = fileUrl.Result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }

}
