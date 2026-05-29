using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AadharLocation.Api.Services;

public class EmailService(
    IOptions<EmailSettings> settings,
    ILogger<EmailService> logger)
{
    private bool IsConfigured =>
        !string.IsNullOrWhiteSpace(settings.Value.Username) &&
        !string.IsNullOrWhiteSpace(settings.Value.Password) &&
        settings.Value.AdminRecipients.Count > 0;

    public async Task SendGeofenceBreachAlertAsync(
        string machineName, string operatorName,
        double lat, double lon, double distanceMeters, DateTime breachedAt)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Email not configured — skipping geofence breach alert for {Machine}", machineName);
            return;
        }

        var subject = $"[AadharLocation] Geofence Breach — {machineName}";
        var body    = BuildGeofenceBreachHtml(machineName, operatorName, lat, lon, distanceMeters, breachedAt);
        await SendAsync(subject, body);
    }

    public async Task SendMachineOfflineAlertAsync(
        string machineName, DateTime lastSeenAt, int minutesOffline)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Email not configured — skipping offline alert for {Machine}", machineName);
            return;
        }

        var subject = $"[AadharLocation] Machine Offline — {machineName}";
        var body    = BuildMachineOfflineHtml(machineName, lastSeenAt, minutesOffline);
        await SendAsync(subject, body);
    }

    private async Task SendAsync(string subject, string htmlBody)
    {
        var cfg = settings.Value;

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(cfg.FromName, cfg.FromAddress));
        foreach (var recipient in cfg.AdminRecipients)
            msg.To.Add(MailboxAddress.Parse(recipient));
        msg.Subject = subject;
        msg.Body    = new TextPart("html") { Text = htmlBody };

        try
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(cfg.SmtpHost, cfg.SmtpPort,
                cfg.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            await smtp.AuthenticateAsync(cfg.Username, cfg.Password);
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);
            logger.LogInformation("Alert email sent: {Subject}", subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send alert email: {Subject}", subject);
        }
    }

    private static string BuildGeofenceBreachHtml(
        string machineName, string operatorName,
        double lat, double lon, double distanceMeters, DateTime breachedAt) =>
        $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:sans-serif;background:#f4f4f4;padding:24px;margin:0">
          <div style="max-width:600px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.12)">
            <div style="background:#EF4444;padding:20px 24px">
              <h2 style="margin:0;color:#fff;font-size:18px;font-weight:600">&#9888; Geofence Breach Detected</h2>
            </div>
            <div style="padding:24px">
              <table style="width:100%;border-collapse:collapse;font-size:14px">
                <tr>
                  <td style="padding:8px 0;color:#6B7280;width:140px;vertical-align:top">Machine</td>
                  <td style="padding:8px 0;font-weight:600;color:#111827">{machineName}</td>
                </tr>
                <tr>
                  <td style="padding:8px 0;color:#6B7280;vertical-align:top">Operator</td>
                  <td style="padding:8px 0;color:#111827">{operatorName}</td>
                </tr>
                <tr>
                  <td style="padding:8px 0;color:#6B7280;vertical-align:top">Time (UTC)</td>
                  <td style="padding:8px 0;color:#111827">{breachedAt:dd MMM yyyy HH:mm:ss}</td>
                </tr>
                <tr>
                  <td style="padding:8px 0;color:#6B7280;vertical-align:top">Coordinates</td>
                  <td style="padding:8px 0;color:#111827">{lat:F6}, {lon:F6}</td>
                </tr>
                <tr>
                  <td style="padding:8px 0;color:#6B7280;vertical-align:top">Distance Outside</td>
                  <td style="padding:8px 0;font-weight:600;color:#EF4444">{distanceMeters:F0} m beyond boundary</td>
                </tr>
              </table>
              <div style="margin-top:24px;padding-top:16px;border-top:1px solid #E5E7EB">
                <p style="margin:0;font-size:13px;color:#6B7280">
                  Log in to the AadharLocation Admin Dashboard to acknowledge this alert and view the live map.
                </p>
              </div>
            </div>
          </div>
        </body>
        </html>
        """;

    private static string BuildMachineOfflineHtml(
        string machineName, DateTime lastSeenAt, int minutesOffline) =>
        $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:sans-serif;background:#f4f4f4;padding:24px;margin:0">
          <div style="max-width:600px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.12)">
            <div style="background:#F59E0B;padding:20px 24px">
              <h2 style="margin:0;color:#fff;font-size:18px;font-weight:600">&#9889; Machine Offline</h2>
            </div>
            <div style="padding:24px">
              <table style="width:100%;border-collapse:collapse;font-size:14px">
                <tr>
                  <td style="padding:8px 0;color:#6B7280;width:140px;vertical-align:top">Machine</td>
                  <td style="padding:8px 0;font-weight:600;color:#111827">{machineName}</td>
                </tr>
                <tr>
                  <td style="padding:8px 0;color:#6B7280;vertical-align:top">Last Seen (UTC)</td>
                  <td style="padding:8px 0;color:#111827">{lastSeenAt:dd MMM yyyy HH:mm:ss}</td>
                </tr>
                <tr>
                  <td style="padding:8px 0;color:#6B7280;vertical-align:top">Offline Duration</td>
                  <td style="padding:8px 0;font-weight:600;color:#F59E0B">{minutesOffline} minutes</td>
                </tr>
              </table>
              <div style="margin-top:24px;padding-top:16px;border-top:1px solid #E5E7EB">
                <p style="margin:0;font-size:13px;color:#6B7280">
                  Log in to the AadharLocation Admin Dashboard to investigate and acknowledge this alert.
                </p>
              </div>
            </div>
          </div>
        </body>
        </html>
        """;
}
