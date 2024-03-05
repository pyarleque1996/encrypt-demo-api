using DEMO.Encrypt.API.Models;
using DEMO.Encrypt.API.Services.Encryption;
using DEMO.Encrypt.API.Services.Splitter;
using Microsoft.AspNetCore.Mvc;

namespace DEMO.Encrypt.API.Controllers
{
    [ApiController]
    [Route("encryption")]
    public class EncryptionController : ControllerBase
    {
        private readonly ISevenZipEncryption _sevenZipEncryptionService;
        private readonly ISplitterService _splitterService;
            
        public EncryptionController(ISevenZipEncryption sevenZipEncryptionService, ISplitterService splitterService)
        {
            _sevenZipEncryptionService = sevenZipEncryptionService;
            _splitterService = splitterService;
        }

        [HttpGet("encrypt_file")]
        public async Task<IActionResult> EncryptFile()
        {
            var folderpath = @"C:\Users\pexyarl\source\repos\DEMO.Encrypt.API\DEMO.Encrypt.API\Files\Data";

            return Ok(await _sevenZipEncryptionService.EncryptFolderParallel(folderpath, "pedro"));
        }

        [HttpGet("split_and_encrypt_data")]
        public async Task<IActionResult> SplitAndEncryptData()
        {
            var folderpath = @"C:\Users\pexyarl\source\repos\DEMO.Encrypt.API\DEMO.Encrypt.API\Files\Data";

            List<RecordDto> records = SeedRecords();

            await _splitterService.SplitToCsvAsync(records, folderpath);

            await _sevenZipEncryptionService.EncryptFolderParallel(folderpath, "pedro");

            return Ok();
        }


        /// <summary>
        /// Seed a list with dummy records.
        /// </summary>
        /// <returns>A list of dummy records.</returns>
        [NonAction]
        public List<RecordDto> SeedRecords()
        {
            List<RecordDto> records = new List<RecordDto>();

            // Populate the list with dummy records
            foreach (var i in Enumerable.Range(1, 10000)) // For example, 10000 dummy records
            {
                records.Add(new RecordDto($"Name{i}", $"{i * 5}")); // Create a new record and add it to the list
            }

            return records; // Return the populated list
        }
    }
}
