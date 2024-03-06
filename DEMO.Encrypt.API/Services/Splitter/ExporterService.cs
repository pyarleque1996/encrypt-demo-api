using System.Text;

namespace DEMO.Encrypt.API.Services.Splitter
{
    public class ExporterService : IExporterService
    {
        private const int MaxFileSizeBytes = 1024;

        private IConfiguration _configuration;

        public ExporterService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> SplitAndExportToCsvAsync<T>(IEnumerable<T> records)
        {
            try
            {
                int currentFileIndex = 1;
                int currentFileSize = 0;

                // Concatenate the project directory path with the output folder path
                var outputFolderPath = Path.Combine(Path.Combine(AppContext.BaseDirectory, _configuration["Encryption:OutputFolderPath"]), Guid.NewGuid().ToString());

                // Create the output directory if it doesn't exist
                Directory.CreateDirectory(outputFolderPath);

                // Create path for file in the output directory
                string currentFilePath = GetFilePath(outputFolderPath, currentFileIndex);

                StreamWriter writer = null;

                try
                {
                    foreach (var record in records)
                    {
                        string csvLine = ConvertRecordToCsvLine(record);

                        // Calculate the size of the CSV line in bytes
                        int csvLineSize = System.Text.Encoding.UTF8.GetByteCount(csvLine);

                        // Check if adding this line would exceed the file size limit
                        if (currentFileSize + csvLineSize >= MaxFileSizeBytes)
                        {
                            // Close the current file and move to the next one
                            writer.Flush(); // Flush any buffered data before closing
                            writer.Dispose(); // Dispose the current writer
                            currentFileIndex++;
                            currentFileSize = 0;

                            // Create the next CSV file
                            currentFilePath = GetFilePath(outputFolderPath, currentFileIndex);
                            writer = new StreamWriter(currentFilePath);
                        }

                        // Create a new StreamWriter if it doesn't exist
                        if (writer == null)
                        {
                            writer = new StreamWriter(currentFilePath);
                        }

                        // Write the CSV line to the file
                        await writer.WriteLineAsync(csvLine);
                        currentFileSize += csvLineSize;
                    }

                    return outputFolderPath;
                }
                finally
                {
                    // Ensure that the StreamWriter is properly disposed
                    writer?.Flush(); // Flush any buffered data before disposing
                    writer?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting to CSV: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Write records to a CSV file asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of records.</typeparam>
        /// <param name="records">The collection of records to write.</param>
        /// <param name="filePath">The file path where the CSV file will be saved.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task<string> ExportRecordsToCsvAsync<T>(IEnumerable<T> records)
        {
            try
            {
                // Concatenate the project directory path with the output folder path
                var outputFolderPath = Path.Combine(AppContext.BaseDirectory, _configuration["Encryption:OutputFolderPath"]);

                // Create the output directory if it doesn't exist
                Directory.CreateDirectory(outputFolderPath);

                // Create a CSV file in the output directory
                string outputfilePath = GenereFilePath(outputFolderPath);

                // Create a StreamWriter to write to the CSV file
                using (StreamWriter writer = new StreamWriter(outputfilePath))
                {
                    // Write each record to the CSV file
                    foreach (var record in records)
                    {
                        string csvLine = ConvertRecordToCsvLine(record);
                        await writer.WriteLineAsync(csvLine);
                    }
                }

                return outputfilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing records to CSV: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert a record to a CSV line.
        /// </summary>
        /// <typeparam name="T">The type of record.</typeparam>
        /// <param name="record">The record.</param>
        /// <returns>The CSV line.</returns>
        private string ConvertRecordToCsvLine<T>(T record)
        {
            // Here you should implement logic to convert a record to a CSV line
            // For simplicity, we assume that type T has properties representing column names
            return string.Join(",", typeof(T).GetProperties().Select(p => p.GetValue(record)?.ToString() ?? ""));
        }

        /// <summary>
        /// Estimate the size of a CSV record in bytes.
        /// </summary>
        /// <returns>The estimated size of a CSV record in bytes.</returns>
        private int EstimateCsvRecordSize()
        {
            // This function should estimate the approximate size in bytes of a CSV line
            // You can calculate it based on the expected size of your records
            // For simplicity, we will return a constant value
            return 100; // Estimated size of a CSV line in bytes
        }

        /// <summary>
        /// Get the file path for a CSV file.
        /// </summary>
        /// <param name="outputPath">The output directory path.</param>
        /// <param name="index">The index of the CSV file.</param>
        /// <returns>The file path.</returns>
        private string GetFilePath(string outputPath, int index)
        {
            return Path.Combine(outputPath, $"output_{index}.csv");
        }

        private string GenereFilePath(string folderpath) => Path.Combine(folderpath, $"{Guid.NewGuid()}.csv");
    }
}
