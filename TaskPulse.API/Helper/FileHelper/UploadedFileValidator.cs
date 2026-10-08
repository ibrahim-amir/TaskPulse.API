namespace TaskPulse.API.Helper.FileHelper
{
    // Everything that decides whether a file is allowed in, in one place.
    // Returns the list of problems — empty means the file is fine — so it drops
    // into a validator the same way the existence checker does.
    public class UploadedFileValidator
    {
        private static readonly HashSet<string> ImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];

        private readonly FileUploadOptions _options;

        public UploadedFileValidator(FileUploadOptions options)
        {
            _options = options;
        }

        // Same size/name checks as Validate, but restricted to image
        // extensions regardless of what FileUpload:AllowedExtensions permits
        // generally (an avatar being a .zip makes no sense).
        public List<string> ValidateImage(IFormFile? file)
        {
            var errors = Validate(file);
            if (file is not null && file.Length > 0)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!ImageExtensions.Contains(extension))
                    errors.Add("Profile pictures must be a PNG, JPG or GIF.");
            }
            return errors;
        }

        public List<string> Validate(IFormFile? file)
        {
            var errors = new List<string>();

            if (file is null || file.Length == 0)
            {
                errors.Add("No file was uploaded.");
                return errors;
            }

            if (file.Length > _options.MaxFileSizeBytes)
                errors.Add($"File is larger than the {_options.MaxFileSizeBytes / (1024 * 1024)} MB limit.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(extension))
                errors.Add("File must have an extension.");
            else if (!_options.AllowedExtensions.Contains(extension))
                errors.Add($"'{extension}' files are not allowed.");

            // A name like "..\..\web.config" or one carrying a second extension
            // ("invoice.pdf.exe") is rejected outright rather than sanitised.
            var fileName = Path.GetFileName(file.FileName);
            if (fileName != file.FileName || fileName.Contains("..") || string.IsNullOrWhiteSpace(fileName))
                errors.Add("File name is not valid.");

            return errors;
        }
    }
}
