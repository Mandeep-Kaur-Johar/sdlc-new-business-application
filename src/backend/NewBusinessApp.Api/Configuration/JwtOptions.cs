namespace NewBusinessApp.Api.Configuration;

/// <summary>
/// JWT options bound from the "Jwt" configuration section. The signing key must be supplied by
/// the hosting environment (Azure Key Vault reference, environment variable or user secrets).
/// No key material is committed to source control.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "https://newbusinessapp.local";

    public string Audience { get; set; } = "newbusinessapp-api";

    /// <summary>Supplied at runtime. Empty in committed configuration by design.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access token lifetime. Approved design: 60 minutes.</summary>
    public int AccessTokenMinutes { get; set; } = 60;

    /// <summary>Refresh token lifetime. Approved design: 7 days.</summary>
    public int RefreshTokenDays { get; set; } = 7;
}

/// <summary>Application behaviour limits taken from the approved API and NFR design.</summary>
public class ApplicationOptions
{
    public const string SectionName = "Application";

    /// <summary>Maximum page size accepted by the records API.</summary>
    public int MaxPageSize { get; set; } = 100;

    public int DefaultPageSize { get; set; } = 20;

    /// <summary>Dashboard cache-aside time to live in seconds.</summary>
    public int DashboardCacheSeconds { get; set; } = 60;

    /// <summary>Maximum number of rows allowed in a single export.</summary>
    public int MaxExportRows { get; set; } = 50_000;

    /// <summary>Idempotency key retention in hours.</summary>
    public int IdempotencyRetentionHours { get; set; } = 24;

    /// <summary>Export download link validity in minutes.</summary>
    public int ExportLinkMinutes { get; set; } = 60;

    /// <summary>Seed demonstration accounts outside production only.</summary>
    public bool SeedDemoData { get; set; } = true;

    public string[] AllowedCorsOrigins { get; set; } = Array.Empty<string>();
}
