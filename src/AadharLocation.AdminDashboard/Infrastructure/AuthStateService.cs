using System.IO;
using System.Text.Json;

namespace AadharLocation.AdminDashboard.Infrastructure;

public class AuthSession
{
    public string Token { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class AdminPrefs
{
    public bool IsDarkTheme { get; set; } = true;
}

public class AuthStateService
{
    private static readonly string AppDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AadharLocation");

    private static readonly string AuthFile  = Path.Combine(AppDataDir, "admin-auth.json");
    private static readonly string PrefsFile = Path.Combine(AppDataDir, "admin-prefs.json");

    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public string Token  { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string UserEmail { get; private set; } = string.Empty;
    public bool IsDarkTheme { get; private set; } = true;

    public bool IsAuthenticated =>
        !string.IsNullOrEmpty(Token) && DateTime.UtcNow < _expiresAt;

    private DateTime _expiresAt;

    public void LoadFromDisk()
    {
        try
        {
            Directory.CreateDirectory(AppDataDir);

            if (File.Exists(AuthFile))
            {
                var session = JsonSerializer.Deserialize<AuthSession>(File.ReadAllText(AuthFile));
                if (session != null)
                {
                    Token       = session.Token;
                    UserName    = session.Name;
                    UserEmail   = session.Email;
                    _expiresAt  = session.ExpiresAt;
                }
            }

            if (File.Exists(PrefsFile))
            {
                var prefs = JsonSerializer.Deserialize<AdminPrefs>(File.ReadAllText(PrefsFile));
                if (prefs != null)
                    IsDarkTheme = prefs.IsDarkTheme;
            }
        }
        catch { /* ignore — fresh start */ }
    }

    public void SetSession(string token, string name, string email)
    {
        Token     = token;
        UserName  = name;
        UserEmail = email;

        // Parse JWT expiry from claims (or default to 8 h)
        _expiresAt = TryParseJwtExpiry(token) ?? DateTime.UtcNow.AddHours(8);

        try
        {
            Directory.CreateDirectory(AppDataDir);
            var session = new AuthSession { Token = Token, Name = UserName, Email = UserEmail, ExpiresAt = _expiresAt };
            File.WriteAllText(AuthFile, JsonSerializer.Serialize(session, _json));
        }
        catch { /* ignore */ }
    }

    public void ClearSession()
    {
        Token     = string.Empty;
        UserName  = string.Empty;
        UserEmail = string.Empty;
        _expiresAt = DateTime.MinValue;

        try { if (File.Exists(AuthFile)) File.Delete(AuthFile); }
        catch { /* ignore */ }
    }

    public void SetTheme(bool isDark)
    {
        IsDarkTheme = isDark;
        try
        {
            Directory.CreateDirectory(AppDataDir);
            File.WriteAllText(PrefsFile, JsonSerializer.Serialize(new AdminPrefs { IsDarkTheme = isDark }, _json));
        }
        catch { /* ignore */ }
    }

    private static DateTime? TryParseJwtExpiry(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;
            var payload = parts[1];
            // Pad base64
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=')
                             .Replace('-', '+').Replace('_', '/');
            var bytes = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(bytes);
            if (doc.RootElement.TryGetProperty("exp", out var exp))
                return DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()).UtcDateTime;
        }
        catch { /* ignore */ }
        return null;
    }
}
