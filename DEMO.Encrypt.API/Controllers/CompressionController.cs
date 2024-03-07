using DEMO.Encrypt.API.Models;
using DEMO.Encrypt.API.Services.Compression;
using DEMO.Encrypt.API.Services.Splitter;
using Microsoft.AspNetCore.Mvc;

namespace DEMO.Encrypt.API.Controllers
{
    [ApiController]
    [Route("compression")]
    public class CompressionController : ControllerBase
    {
        private readonly ISevenZipCompression _sevenZipCompressionService;
        private readonly IExporterService _splitterService;

        public CompressionController(ISevenZipCompression sevenZipCompressionService, IExporterService splitterService)
        {
            _sevenZipCompressionService = sevenZipCompressionService;
            _splitterService = splitterService;
        }

        [HttpGet("split_and_compress_data")]
        public async Task<IActionResult> SplitAndCompressData(int numberOfRecords)
        {
            List<RecordDto> records = SeedRecords(numberOfRecords);

            var folderpath = await _splitterService.SplitAndExportToCsvAsync(records);

            await _sevenZipCompressionService.CompressFolderParallel(folderpath, "pedro");

            return Ok();
        }

        [HttpGet("write_csv_and_compress_data_in_chunks")]
        public async Task<IActionResult> WriteCsvAndCompressDataInChunks()
        {
            List<RecordDto> records = SeedRecords(99999);

            var inputFilePath = await _splitterService.ExportRecordsToCsvAsync(records);

            await _sevenZipCompressionService.CompressFilesInChunks(new string[] { inputFilePath }, "pedro");

            return Ok();
        }

        [HttpGet("estimate_ratio")]
        public async Task<IActionResult> EstimateRatio(int numberOfRecords)
        {
            List<RecordDto> records = SeedRecords(numberOfRecords);

            var inputFilePath = await _splitterService.ExportRecordsToCsvAsync(records);

            var result = await _sevenZipCompressionService.EstimateCompressedSize(inputFilePath);

            return Ok($"The reatio is : {result.Item1} and the compression percentage is : {result.Item2}% for {numberOfRecords} records.");
        }


        /// <summary>
        /// Seed a list with dummy records.
        /// </summary>
        /// <returns>A list of dummy records.</returns>
        [NonAction]
        public List<RecordDto> SeedRecords(int numberOfRecords)
        {
            List<RecordDto> records = new List<RecordDto>();

            // Populate the list with dummy records
            foreach (var i in Enumerable.Range(1, numberOfRecords)) // For example, 10000 dummy records
            {
                records.Add(new RecordDto(
                    Name: $"Name{i}",
                    Code: $"{i * 5}",
                    Field3: $"Field3_{i}",
                    Field4: $"Field4_{i}",
                    Field5: $"Field5_{i}",
                    Field6: $"Field6_{i}",
                    Field7: $"Field7_{i}",
                    Field8: $"Field8_{i}",
                    Field9: $"Field9_{i}",
                    Field10: $"Field10_{i}",
                    Field11: $"Field11_{i}",
                    Field12: $"Field12_{i}",
                    Field13: $"Field13_{i}",
                    Field14: $"Field14_{i}",
                    Field15: $"Field15_{i}",
                    Field16: $"Field16_{i}",
                    Field17: $"Field17_{i}",
                    Field18: $"Field18_{i}",
                    Field19: $"Field19_{i}",
                    Field20: $"Field20_{i}"
                )); // Create a new record and add it to the list
            }

            return records; // Return the populated list
        }
    }
}
