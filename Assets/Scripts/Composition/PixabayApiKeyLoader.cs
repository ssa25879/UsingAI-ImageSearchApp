using System;
using System.IO;

namespace ImageSearch.Composition
{
    public static class PixabayApiKeyLoader
    {
        public static bool TryRead(string path, out string key)
        {
            key = null;
            try
            {
                if (!File.Exists(path)) return false;
                var value = File.ReadAllText(path).Trim();
                if (value.Length == 0) return false;
                key = value;
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
    }
}
