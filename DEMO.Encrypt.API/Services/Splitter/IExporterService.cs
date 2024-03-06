namespace DEMO.Encrypt.API.Services.Splitter
{
    public interface IExporterService
    {
        Task<string> SplitAndExportToCsvAsync<T>(IEnumerable<T> records);
        Task<string> ExportRecordsToCsvAsync<T>(IEnumerable<T> records);
    }
}
