namespace AadharLocation.Shared.DTOs.Activation;

public record VerifyUninstallCodeRequest(string DeviceKey, string Code);
