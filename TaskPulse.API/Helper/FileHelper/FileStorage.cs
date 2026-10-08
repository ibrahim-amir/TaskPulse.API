namespace TaskPulse.API.Helper.FileHelper
{
    // Puts files on disk and takes them off again. Nothing else in the app
    // knows where they live, so swapping this for blob storage later is one
    // class, not a hunt through the services.
    public class FileStorage
    {
        private readonly FileUploadOptions _options;
        private readonly IWebHostEnvironment _env;

        public FileStorage(FileUploadOptions options, IWebHostEnvironment env)
        {
            _options = options;
            _env = env;
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

        // Avatars live under wwwroot so ASP.NET's static file middleware can
        // serve them directly at /avatars/{name} — no controller, no auth
        // header needed, so a plain <img src> just works. Named with the
        // user's id so a stale reference is easy to spot while debugging.
        public async Task<string> SaveAvatarAsync(IFormFile file, int userId)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var folder = Path.Combine(webRoot, "avatars");
            Directory.CreateDirectory(folder);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var storedName = $"{userId}-{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, storedName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/avatars/{storedName}";
        }

        // Same idea as SaveAvatarAsync — a plain static URL, no auth needed to
        // view it inline in a chat bubble.
        public async Task<string> SaveChatImageAsync(IFormFile file, int senderId)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var folder = Path.Combine(webRoot, "chat-images");
            Directory.CreateDirectory(folder);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var storedName = $"{senderId}-{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, storedName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/chat-images/{storedName}";
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
