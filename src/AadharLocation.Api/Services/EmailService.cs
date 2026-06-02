using Microsoft.Extensions.Options;
using Resend;

namespace AadharLocation.Api.Services;

public class EmailService(
    IResend resend,
    IOptions<EmailSettings> settings,
    ILogger<EmailService> logger)
{
    private bool IsConfigured =>
        !string.IsNullOrWhiteSpace(settings.Value.ApiKey) &&
        !string.IsNullOrWhiteSpace(settings.Value.FromAddress);

    public async Task SendAlertReportAsync(byte[] csvBytes, string fileName, string toEmail)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Email service is not configured. Set Email:ApiKey in appsettings.");

        var cfg = settings.Value;
        var message = new EmailMessage
        {
            From        = $"{cfg.FromName} <{cfg.FromAddress}>",
            Subject     = $"[AadharLocation] Alert Report — {DateTime.UtcNow:dd MMM yyyy}",
            HtmlBody    = BuildReportEmailHtml(fileName),
            Attachments = [new EmailAttachment { Filename = fileName, Content = csvBytes, ContentType = "text/csv" }],
        };
        message.To.Add(toEmail);

        await resend.EmailSendAsync(message);
        logger.LogInformation("Report email sent to {Email}", toEmail);
    }

    public async Task SendGeofenceBreachAlertAsync(
        string machineName, string operatorName,
        double lat, double lon, double distanceMeters, DateTime breachedAt,
        IEnumerable<string> adminEmails)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Email not configured — skipping geofence breach alert for {Machine}", machineName);
            return;
        }

        var subject = $"[AadharLocation] Geofence Breach — {machineName}";
        var body    = BuildGeofenceBreachHtml(machineName, operatorName, lat, lon, distanceMeters, breachedAt);
        await SendAsync(subject, body, adminEmails);
    }

    public async Task SendMachineOfflineAlertAsync(
        string machineName, DateTime lastSeenAt, int minutesOffline,
        IEnumerable<string> adminEmails)
    {
        if (!IsConfigured)
        {
            logger.LogWarning("Email not configured — skipping offline alert for {Machine}", machineName);
            return;
        }

        var subject = $"[AadharLocation] Machine Offline — {machineName}";
        var body    = BuildMachineOfflineHtml(machineName, lastSeenAt, minutesOffline);
        await SendAsync(subject, body, adminEmails);
    }

    private async Task SendAsync(string subject, string htmlBody, IEnumerable<string> adminEmails)
    {
        var cfg        = settings.Value;
        var recipients = adminEmails.ToList();

        if (recipients.Count == 0)
        {
            logger.LogWarning("No admin recipients found — skipping alert email: {Subject}", subject);
            return;
        }

        var message = new EmailMessage
        {
            From     = $"{cfg.FromName} <{cfg.FromAddress}>",
            Subject  = subject,
            HtmlBody = htmlBody,
        };

        foreach (var recipient in recipients)
            message.To.Add(recipient);

        try
        {
            await resend.EmailSendAsync(message);
            logger.LogInformation("Alert email sent via Resend: {Subject}", subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send alert email: {Subject}", subject);
        }
    }

    private static string BuildReportEmailHtml(string fileName) =>
        $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:sans-serif;background:#f4f4f4;padding:24px;margin:0">
          <div style="max-width:600px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.12)">
            <div style="background:#2DD4BF;padding:20px 24px">
              <h2 style="margin:0;color:#fff;font-size:18px;font-weight:600">&#128196; Alert Report</h2>
            </div>
            <div style="padding:24px">
              <p style="margin:0 0 16px;font-size:14px;color:#374151">
                Please find the attached alert report <strong>{fileName}</strong> as requested.
              </p>
              <p style="margin:0;font-size:13px;color:#6B7280">
                Log in to the AadharLocation Admin Dashboard to view and filter alerts in real time.
              </p>
            </div>
          </div>
        </body>
        </html>
        """;

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
