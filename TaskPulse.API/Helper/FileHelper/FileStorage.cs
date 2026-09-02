namespace TaskPulse.API.Helper.FileHelper
{
    // Puts files on disk and takes them off again. Nothing else in the app
    // knows where they live, so swapping this for blob storage later is one
    // class, not a hunt through the services.
    public class FileStorage
    {
        private readonly FileUploadOptions _options;

        public FileStorage(FileUploadOptions options)
        {
            _options = options;
        }

        // Saves under uploads/tasks/{taskId}/ with a generated name, so two
        // people uploading "report.pdf" don't overwrite each other and nothing
        // the user typed ever becomes a real path. Returns the stored path.
        public async Task<string> SaveAsync(IFormFile file, int taskId)
        {
            var folder = Path.Combine(_options.StorageRoot, "tasks", taskId.ToString());
            Directory.CreateDirectory(folder);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var storedName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, storedName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fullPath;
        }

        // Used when a reupload replaces a file. A missing file isn't an error —
        // the row going away is what matters.
        public void Delete(string storedPath)
        {
            if (!string.IsNullOrWhiteSpace(storedPath) && File.Exists(storedPath))
                File.Delete(storedPath);
        }

        public Stream OpenRead(string storedPath)
        {
            return new FileStream(storedPath, FileMode.Open, FileAccess.Read);
        }

        public bool Exists(string storedPath)
        {
            return !string.IsNullOrWhiteSpace(storedPath) && File.Exists(storedPath);
        }
    }
}
