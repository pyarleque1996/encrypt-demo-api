namespace DEMO.Encrypt.API.Services.Compression
{
    public interface ICompression
    {
        Task<(double, double)> EstimateCompressedSize(string csvFilePath);
        Task<bool> CompressFilesInChunks(string[] filePaths, string password);
        Task<bool> CompressFolderParallel(string folderPath, string password);
        Task<bool> CompressFilesParallel(string[] filePaths, string password);
        Task<bool> CompressDataAsync(string filePath, byte[] data, string password);
        Task<bool> CompressFileWithPasswordAsync(string filePath, string password);
    }
}
