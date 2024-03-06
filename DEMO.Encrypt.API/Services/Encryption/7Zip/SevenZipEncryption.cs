using System.Diagnostics;

namespace DEMO.Encrypt.API.Services.Encryption
{
    public class SevenZipEncryption : ISevenZipEncryption
    {
        public async Task<bool> EncryptFilesInChunks(string[] filePaths, string password)
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

                            // Create an encrypted file for the current chunk
                            string encryptedFileName = $"{Path.GetFileNameWithoutExtension(filePath)}_{chunkIndex}.7z";
                            string encryptedFilePath = Path.Combine(Path.GetDirectoryName(filePath), encryptedFileName);

                            // Encrypt the chunk file
                            await EncryptChunkAsync(encryptedFilePath, buffer, password);

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

        public async Task<bool> EncryptFolderParallel(string folderPath, string password)
        {
            // Search for all .csv files within the directory
            string[] files = Directory.GetFiles(folderPath, "*.csv");

            return await EncryptFilesParallel(files, password);
        }

        public async Task<bool> EncryptFilesParallel(string[] filePaths, string password)
        {
            var failedFiles = new List<string>();
            var encryptedFiles = new List<string>();

            try
            {
                var encryptTasks = new List<Task>();

                foreach (var filePath in filePaths)
                {
                    encryptTasks.Add(Task.Run(async () =>
                    {
                        string encryptedFilePath = $"{filePath}.7z";
                        if (await EncryptFileAsync(filePath, password))
                        {
                            encryptedFiles.Add(encryptedFilePath);
                        }
                        else
                        {
                            failedFiles.Add(filePath);
                        }
                    }));
                }

                await Task.WhenAll(encryptTasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // rollback: Delete encrypted files if an error occurs
                await RollbackAsync(encryptedFiles);
                return false;
            }

            if (failedFiles.Any())
            {
                await RollbackAsync(encryptedFiles);
                return false;
            }

            return true;
        }

        public async Task<bool> EncryptFileAsync(string filePath, string password)
        {
            // Create a subfolder named "Encrypted" within the folder of the original file
            string outputFolder = Path.Combine(Path.GetDirectoryName(filePath), "Encrypted");
            Directory.CreateDirectory(outputFolder);

            // Build the command to encrypt the file with 7-Zip
            string encryptedFileName = Path.GetFileNameWithoutExtension(filePath) + ".7z";
            string encryptedFilePath = Path.Combine(outputFolder, encryptedFileName);
            string command = $"a -p{password} -mhe \"{encryptedFilePath}\" \"{filePath}\"";

            // Execute the 7-Zip command asynchronously
            return await ExecuteCommandAsync(command);
        }

        public async Task<bool> EncryptChunkAsync(string filePath, byte[] data, string password)
        {
            // Create a subfolder named "Encrypted" within the folder of the original file
            string outputFolder = Path.Combine(Path.GetDirectoryName(filePath), "Encrypted");
            Directory.CreateDirectory(outputFolder);

            // Build the command to encrypt the file with 7-Zip
            string encryptedFileName = Path.GetFileNameWithoutExtension(filePath) + ".7z";
            string encryptedFilePath = Path.Combine(outputFolder, encryptedFileName);
            string command = $"a -p{password} -mhe \"{encryptedFilePath}\" \"{filePath}\"";

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

        private async Task RollbackAsync(List<string> encryptedFilesToDelete)
        {
            var deleteTasks = new List<Task>();

            foreach (var file in encryptedFilesToDelete)
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
