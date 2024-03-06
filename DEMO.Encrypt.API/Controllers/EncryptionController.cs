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
        private readonly IExporterService _splitterService;
            
        public EncryptionController(ISevenZipEncryption sevenZipEncryptionService, IExporterService splitterService)
        {
            _sevenZipEncryptionService = sevenZipEncryptionService;
            _splitterService = splitterService;
        }

        [HttpGet("split_and_encrypt_data")]
        public async Task<IActionResult> SplitAndEncryptData()
        {
            List<RecordDto> records = SeedRecords();

            var folderpath = await _splitterService.SplitAndExportToCsvAsync(records);

            await _sevenZipEncryptionService.EncryptFolderParallel(folderpath, "pedro");

            return Ok();
        }

        [HttpGet("write_csv_and_encrypt_data_in_chunks")]
        public async Task<IActionResult> WriteCsvAndEncryptDataInChunks()
        {
            List<RecordDto> records = SeedRecords();

            var inputFilePath = await _splitterService.ExportRecordsToCsvAsync(records);

            await _sevenZipEncryptionService.EncryptFilesInChunks(new string[] { inputFilePath }, "pedro");

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
            foreach (var i in Enumerable.Range(1, 99999)) // For example, 10000 dummy records
            {
                records.Add(new RecordDto($"Name{i}", $"{i * 5}")); // Create a new record and add it to the list
            }

            return records; // Return the populated list
        }
    }
}
