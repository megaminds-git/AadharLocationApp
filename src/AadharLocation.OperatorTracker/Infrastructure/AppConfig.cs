using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AadharLocation.OperatorTracker.Infrastructure;

public static class AppConfig
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AadharLocation",
        "tracker.dat");

    public static TrackerCredentials? Load()
    {
        if (!File.Exists(StorePath))
            return null;

        try
        {
            var encrypted = File.ReadAllBytes(StorePath);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<TrackerCredentials>(Encoding.UTF8.GetString(decrypted));
        }
        catch
        {
            return null;
        }
    }

    public static void Save(TrackerCredentials credentials)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
        var json = JsonSerializer.Serialize(credentials);
        var bytes = Encoding.UTF8.GetBytes(json);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(StorePath, encrypted);
    }

    public static void Clear()
    {
        if (File.Exists(StorePath))
            File.Delete(StorePath);
    }
}
