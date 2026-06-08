using System.IO;
using Microsoft.Web.WebView2.Core;

namespace AadharLocation.AdminDashboard.Infrastructure;

public static class WebViewEnvironment
{
    private static CoreWebView2Environment? _instance;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly string UserDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AadharLocation", "WebView2Cache");

    public static async Task<CoreWebView2Environment> GetAsync()
    {
        if (_instance is not null) return _instance;
        await _lock.WaitAsync();
        try
        {
            _instance ??= await CoreWebView2Environment.CreateAsync(userDataFolder: UserDataFolder);
            return _instance;
        }
        finally { _lock.Release(); }
    }
}
