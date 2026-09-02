namespace TaskPulse.API.Helper.FileHelper
{
    // Reads the FileUpload block from appsettings so the limits are config, not
    // constants buried in code:
    //
    //   "FileUpload": {
    //     "StorageRoot": "uploads",
    //     "MaxFileSizeMb": "10",
    //     "AllowedExtensions": ".pdf,.docx,.png,..."
    //   }
    public class FileUploadOptions
    {
        public string StorageRoot { get; }
        public long MaxFileSizeBytes { get; }
        public IReadOnlyCollection<string> AllowedExtensions { get; }

        public FileUploadOptions(IConfiguration configuration)
        {
            StorageRoot = configuration["FileUpload:StorageRoot"] ?? "uploads";

            var maxMb = int.TryParse(configuration["FileUpload:MaxFileSizeMb"], out var mb) ? mb : 10;
            MaxFileSizeBytes = maxMb * 1024L * 1024L;

            AllowedExtensions = (configuration["FileUpload:AllowedExtensions"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(extension => extension.ToLowerInvariant())
                .ToHashSet();
        }
    }
}
