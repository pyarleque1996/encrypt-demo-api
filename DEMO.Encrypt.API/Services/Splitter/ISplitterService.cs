namespace DEMO.Encrypt.API.Services.Splitter
{
    public interface ISplitterService
    {
        Task SplitToCsvAsync<T>(List<T> records, string outputPath);
    }
}
