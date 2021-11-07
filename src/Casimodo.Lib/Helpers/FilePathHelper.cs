using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Casimodo.Lib
{
    public static class FileHelper
    {
        public static string ReadTextFile(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static async Task<string> ReadTextFileAsync(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
    }

    public static class FileSystemHelper
    {
        static readonly string _invalidFilesystemPathChars;
        static readonly Regex _invalidFilesystemPathRegex;

        static FileSystemHelper()
        {
            _invalidFilesystemPathChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            _invalidFilesystemPathRegex = new Regex(string.Format("[{0}]", Regex.Escape(_invalidFilesystemPathChars)));
        }

        public static string EnsureValidPath(string path)
        {
            return _invalidFilesystemPathRegex.Replace(path, "").Trim();
        }

        public static string GetFileNameWithoutExtension(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            return Path.GetFileNameWithoutExtension(filePath);
        }

        public static string GetExtension(string filePath, string defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return defaultValue;

            var ext = Path.GetExtension(filePath);
            if (string.IsNullOrWhiteSpace(ext))
                return defaultValue;

            ext = ext.ToLowerInvariant();

            if (ext.StartsWith("."))
                return ext[1..];

            return ext;
        }
    }
}