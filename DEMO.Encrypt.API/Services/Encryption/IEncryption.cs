namespace DEMO.Encrypt.API.Services.Encryption
{
    public interface IEncryption
    {
        Task<bool> EncryptFolderParallel(string folderPath, string password);
        Task<bool> EncryptFilesParallel(string[] filePaths, string password);
        Task<bool> EncryptFileAsync(string filePath, string password);
    }
}
