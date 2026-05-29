namespace AadharLocation.Shared.DTOs.Activation;

public record DeviceDto(
    int Id,
    string DeviceKey,
    int OperatorId,
    string OperatorName,
    int MachineId,
    string MachineName,
    DateTime? LastPingAt,
    bool IsActive,
    bool HasActiveUninstallCode);
