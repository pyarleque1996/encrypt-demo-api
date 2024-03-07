using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace DEMO.Encrypt.API.Services.Compression
{
    public class SevenZipCompression : ISevenZipCompression
    {
        public async Task<(double, double)> EstimateCompressedSize(string csvFilePath)
        {
            // Read the content of the CSV file
            string csvContent = await File.ReadAllTextAsync(csvFilePath);

            // Convert the CSV content to a byte array
            byte[] uncompressedData = Encoding.UTF8.GetBytes(csvContent);

            // Estimate the size of the uncompressed data
            long uncompressedSize = uncompressedData.Length;

            var compressedFilePath = await CompressFileAsync(csvFilePath);

            if (string.IsNullOrEmpty(compressedFilePath))
            {
                // Error occurred during compression
                return (-1, -1); // Return an error code
            }

            // Read the content of the compressed file
            string compressedFileContent = await File.ReadAllTextAsync(compressedFilePath);

            // Convert the CSV content to a byte array
            byte[] compressedData = Encoding.UTF8.GetBytes(compressedFileContent);

            // Estimate the size of the compressed data
            long compressedSize = compressedData.Length;

            // Delete the temporary compressed file
            File.Delete(Path.GetFileNameWithoutExtension(csvFilePath) + ".7z");

            // Calculate the compression ratio
            double compressionRatio = (double)compressedSize / uncompressedSize;
            double compressionPercentage = ((double)(uncompressedSize - compressedSize) / uncompressedSize) * 100;


            return (compressionRatio, compressionPercentage);
        }

        public async Task<bool> CompressFilesInChunks(string[] filePaths, string password)
        {
            try
            {
                foreach (var filePath in filePaths)
                {
                    long fileSize = new FileInfo(filePath).Length; // Original file size
                    long chunkSize = 2 * 1024 * 1024; // Desired size of each chunk (in bytes)
                    int chunkIndex = 1;

                    using (FileStream fileStream = File.OpenRead(filePath))
                    {
                        while (fileStream.Position < fileSize)
                        {
                            // Read the original file in chunks
                            byte[] buffer = new byte[chunkSize];
                            int bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);

                            // Create an compressed file for the current chunk
                            string compressedFileName = $"{Path.GetFileNameWithoutExtension(filePath)}_{chunkIndex}.7z";
                            string compressedFilePath = Path.Combine(Path.GetDirectoryName(filePath), compressedFileName);

                            // Compress the chunk file
                            await CompressDataAsync(compressedFilePath, buffer, password);

                            chunkIndex++;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CompressFolderParallel(string folderPath, string password)
        {
            // Search for all .csv files within the directory
            string[] files = Directory.GetFiles(folderPath, "*.csv");

            return await CompressFilesParallel(files, password);
        }

        public async Task<bool> CompressFilesParallel(string[] filePaths, string password)
        {
            var failedFiles = new List<string>();
            var compressedFiles = new List<string>();

            try
            {
                var compressTasks = new List<Task>();

                foreach (var filePath in filePaths)
                {
                    compressTasks.Add(Task.Run(async () =>
                    {
                        string compressedFilePath = $"{filePath}.7z";
                        if (await CompressFileWithPasswordAsync(filePath, password))
                        {
                            compressedFiles.Add(compressedFilePath);
                        }
                        else
                        {
                            failedFiles.Add(filePath);
                        }
                    }));
                }

                await Task.WhenAll(compressTasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // rollback: Delete compressed files if an error occurs
                await RollbackAsync(compressedFiles);
                return false;
            }

            if (failedFiles.Any())
            {
                await RollbackAsync(compressedFiles);
                return false;
            }

            return true;
        }

        public async Task<bool> CompressFileWithPasswordAsync(string filePath, string password)
        {
            // Create a subfolder named "Compressed" within the folder of the original file
            string outputFolder = Path.Combine(Path.GetDirectoryName(filePath), "Compressed");
            Directory.CreateDirectory(outputFolder);

            // Build the command to compress the file with 7-Zip
            string compressedFileName = Path.GetFileNameWithoutExtension(filePath) + ".7z";
            string compressedFilePath = Path.Combine(outputFolder, compressedFileName);
            string command = $"a -v1m -p{password} -mhe \"{compressedFilePath}\" \"{filePath}\"";

            // Execute the 7-Zip command asynchronously
            return await ExecuteCommandAsync(command);
        }

        public async Task<string> CompressFileAsync(string filePath)
        {
            // Create a subfolder named "Compressed" within the folder of the original file
            string outputFolder = Path.Combine(Path.GetDirectoryName(filePath), "Compressed");
            Directory.CreateDirectory(outputFolder);

            // Build the command to compress the file with 7-Zip
            string compressedFileName = Path.GetFileNameWithoutExtension(filePath) + ".7z";
            string compressedFilePath = Path.Combine(outputFolder, compressedFileName);
            string command = $"a -v1m -mhe \"{compressedFilePath}\" \"{filePath}\"";

            // Execute the 7-Zip command asynchronously
            var result = await ExecuteCommandAsync(command);

            if (result)
            {
                return compressedFilePath;
            }

            return string.Empty;
        }

        public async Task<bool> CompressDataAsync(string filePath, byte[] data, string password)
        {
            // Create a subfolder named "Compressed" within the folder of the original file
            string outputFolder = Path.Combine(Path.GetDirectoryName(filePath), "Compressed");
            Directory.CreateDirectory(outputFolder);

            // Build the command to compress the file with 7-Zip
            string compressedFileName = Path.GetFileNameWithoutExtension(filePath) + ".7z";
            string compressedFilePath = Path.Combine(outputFolder, compressedFileName);
            string command = $"a -p{password} -mhe \"{compressedFilePath}\" \"{filePath}\"";

            // Execute the 7-Zip command asynchronously
            return await ExecuteCommandAsync(data, command);
        }

        private async Task<bool> ExecuteCommandAsync(byte[] data, string command)
        {
            try
            {
                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    // Start the process and write the data to 7-Zip's standard input
                    using (Process process = new Process())
                    {
                        process.StartInfo.FileName = "7z.exe";
                        process.StartInfo.Arguments = command;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardInput = true;
                        process.Start();

                        // Write data to 7-Zip's standard input
                        await memoryStream.CopyToAsync(process.StandardInput.BaseStream);
                        process.StandardInput.Close();

                        // Wait for the process to exit
                        await process.WaitForExitAsync();

                        if (process.ExitCode != 0)
                        {
                            Console.WriteLine($"Error: 7-Zip process exited with code {process.ExitCode}");
                            return false;
                        }

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message} - {ex.StackTrace}");
                return false;
            }
        }

        private async Task<MemoryStream> CompressDataAsync(byte[] data, string outputFile)
        {
            try
            {
                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    // Start the process to execute 7-Zip
                    using (Process process = new Process())
                    {
                        // Configure the process start info
                        process.StartInfo.FileName = "7z.exe"; // Path to 7-Zip executable
                        process.StartInfo.Arguments = $"a \"{outputFile}\" -si"; // Command for compression
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardInput = true;
                        process.StartInfo.RedirectStandardOutput = true;

                        // Start the process
                        process.Start();

                        // Write data to 7-Zip's standard input
                        await memoryStream.CopyToAsync(process.StandardInput.BaseStream);
                        process.StandardInput.Close();

                        // Wait for the process to exit
                        await process.WaitForExitAsync();

                        // Read the compressed data from 7-Zip's standard output
                        MemoryStream compressedStream = new MemoryStream();
                        await process.StandardOutput.BaseStream.CopyToAsync(compressedStream);

                        // Reset the stream position to the beginning
                        compressedStream.Position = 0;

                        return compressedStream;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error compressing data with 7-Zip: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> ExecuteCommandAsync(string command)
        {
            try
            {
                // Configure the process start info
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "7z.exe",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Start the process and wait for it to finish
                using (Process process = Process.Start(startInfo))
                {
                    // Read the standard output and error output (optional)
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    // Wait for the process to exit
                    await process.WaitForExitAsync();

                    // If there are errors, you can handle them here
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine($"Error: {error}");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message} - {ex.StackTrace}");
                return false;
            }
        }

        private async Task RollbackAsync(List<string> compressedFilesToDelete)
        {
            var deleteTasks = new List<Task>();

            foreach (var file in compressedFilesToDelete)
            {
                deleteTasks.Add(DeleteFileAsync(file));
            }

            await Task.WhenAll(deleteTasks);
        }

        private async Task DeleteFileAsync(string filePath)
        {
            try
            {
                await Task.Run(() => File.Delete(filePath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar archivo encriptado {filePath}: {ex.Message}");
            }
        }
    }
}
