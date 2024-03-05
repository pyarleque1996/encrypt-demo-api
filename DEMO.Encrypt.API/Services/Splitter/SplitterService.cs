using System.Text;

namespace DEMO.Encrypt.API.Services.Splitter
{
    public class SplitterService : ISplitterService
    {
        private const int MaxFileSizeBytes = 1024;

        public async Task SplitToCsvAsync<T>(List<T> records, string outputPath)
        {
            try
            {
                int currentFileIndex = 1;
                int currentFileSize = 0;

                string currentFilePath = GetFilePath(outputPath, currentFileIndex);
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
                            currentFilePath = GetFilePath(outputPath, currentFileIndex);
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
    }
}
