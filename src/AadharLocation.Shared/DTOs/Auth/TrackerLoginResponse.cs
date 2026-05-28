namespace AadharLocation.Shared.DTOs.Auth;

public record TrackerLoginResponse(string Token, string DeviceKey, int OperatorId, int MachineId);
