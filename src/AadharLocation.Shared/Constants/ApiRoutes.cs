namespace AadharLocation.Shared.Constants;

public static class ApiRoutes
{
    private const string Base = "/api";

    public static class Auth
    {
        public const string Login = $"{Base}/auth/login";
        public const string TrackerLogin = $"{Base}/auth/tracker-login";
        public const string TrackerProfile = $"{Base}/auth/tracker-profile";
        public const string TrackerLogout = $"{Base}/auth/tracker-logout";
        public const string ChangePassword = $"{Base}/auth/change-password";
    }

    public static class Operators
    {
        public const string Base = $"{ApiRoutes.Base}/operators";
        public const string ById = $"{Base}/{{id}}";
        public const string GenerateUninstallCode = $"{Base}/{{id}}/generate-uninstall-code";
    }

    public static class Machines
    {
        public const string Base = $"{ApiRoutes.Base}/machines";
        public const string ById = $"{Base}/{{id}}";
        public const string Live = $"{Base}/live";
    }

    public static class Geofences
    {
        public const string Base = $"{ApiRoutes.Base}/geofences";
        public const string ById = $"{Base}/{{id}}";
    }

    public static class Location
    {
        public const string Ping = $"{Base}/location/ping";
    }

    public static class Alerts
    {
        public const string Base = $"{ApiRoutes.Base}/alerts";
        public const string Acknowledge = $"{Base}/{{id}}/acknowledge";
        public const string Summary = $"{Base}/summary";
    }

    public static class Reports
    {
        public const string Device        = $"{Base}/reports/device";
        public const string Alerts        = $"{Base}/reports/alerts";
        public const string AlertsExport  = $"{Base}/reports/alerts/export";
        public const string AlertsEmail   = $"{Base}/reports/alerts/email";
    }

    public static class Activation
    {
        public const string Devices = $"{Base}/activation/devices";
        public const string GenerateUninstallCode = $"{Base}/activation/{{deviceKey}}/generate-uninstall-code";
        public const string VerifyUninstallCode = $"{Base}/activation/verify-uninstall-code";
        public const string Deactivate = $"{Base}/activation/deactivate";
    }

    public static class Settings
    {
        public const string Base = $"{ApiRoutes.Base}/settings";
    }
}
