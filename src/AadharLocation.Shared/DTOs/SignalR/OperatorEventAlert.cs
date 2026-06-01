using AadharLocation.Shared.Enums;

namespace AadharLocation.Shared.DTOs.SignalR;

public record OperatorEventAlert(
    int AlertId,
    int MachineId,
    string MachineName,
    int OperatorId,
    string OperatorName,
    AlertType AlertType,
    DateTime CreatedAt);
