namespace AadharLocation.Api.Services;

public class EmailSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "AadharLocation Alerts";
    public List<string> AdminRecipients { get; set; } = [];
}
