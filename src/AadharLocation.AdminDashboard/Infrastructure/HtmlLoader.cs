using System.IO;
using System.Reflection;

namespace AadharLocation.AdminDashboard.Infrastructure;

public static class HtmlLoader
{
    public static string Load(string fileName)
    {
        var assembly     = Assembly.GetExecutingAssembly();
        var resourceName = $"AadharLocation.AdminDashboard.Assets.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
