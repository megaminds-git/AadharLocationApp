using AadharLocation.Shared.Enums;

namespace AadharLocation.Shared.DTOs.Alerts;

public record AlertDto(
    int Id,
    int MachineId,
    string MachineName,
    int OperatorId,
    string OperatorName,
    AlertType AlertType,
    string Message,
    double? Latitude,
    double? Longitude,
    DateTime CreatedAt,
    bool IsAcknowledged,
    DateTime? AcknowledgedAt);
