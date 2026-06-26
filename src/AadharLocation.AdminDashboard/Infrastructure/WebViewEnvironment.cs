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
            if (_instance is not null) return _instance;
            try
            {
                _instance = await CoreWebView2Environment.CreateAsync(userDataFolder: UserDataFolder);
            }
            catch (UnauthorizedAccessException)
            {
                // Cache folder is locked by a previous or concurrent process; fall back to a temp directory.
                _instance = await CoreWebView2Environment.CreateAsync(userDataFolder: null);
            }
            return _instance;
        }
        finally { _lock.Release(); }
    }
}
