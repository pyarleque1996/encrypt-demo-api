using System.Diagnostics;

namespace DEMO.Encrypt.API.Services.Encryption
{
    public class SevenZipEncryption : ISevenZipEncryption
    {
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
            string command = $"a -p {password} -mhe \"{encryptedFilePath}\" \"{filePath}\"";

            // Execute the 7-Zip command asynchronously
            return await ExecuteCommandAsync(command);
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
