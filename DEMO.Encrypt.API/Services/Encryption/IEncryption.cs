namespace DEMO.Encrypt.API.Services.Encryption
{
    public interface IEncryption
    {
        Task<bool> EncryptFilesInChunks(string[] filePaths, string password);
        Task<bool> EncryptFolderParallel(string folderPath, string password);
        Task<bool> EncryptFilesParallel(string[] filePaths, string password);
        Task<bool> EncryptChunkAsync(string filePath, byte[] data, string password);
        Task<bool> EncryptFileAsync(string filePath, string password);
    }
}
