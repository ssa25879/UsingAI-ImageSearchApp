using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ImageSearch.Data.Pixabay.DataSource
{
    internal sealed class PixabaySearchCache
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);
        private readonly string _directory;

        public PixabaySearchCache(string directory) => _directory = directory;

        public bool TryRead(string requestKey, DateTime utcNow, out string json)
        {
            json = null;
            try
            {
                var path = PathFor(requestKey);
                if (!File.Exists(path)) return false;
                var age = utcNow - File.GetLastWriteTimeUtc(path);
                if (age < TimeSpan.Zero || age >= Lifetime) return false;
                json = File.ReadAllText(path);
                return !string.IsNullOrWhiteSpace(json);
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        public void Store(string requestKey, string json, DateTime utcNow)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                var path = PathFor(requestKey);
                File.WriteAllText(path, json);
                File.SetLastWriteTimeUtc(path, utcNow);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private string PathFor(string requestKey)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(requestKey));
                return Path.Combine(_directory, BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant() + ".json");
            }
        }
    }
}
