using System.ComponentModel.DataAnnotations;

namespace NewBusinessApp.Api.Models;

/// <summary>Application roles defined by the approved security design (Epic 2862).</summary>
public static class ApplicationRoles
{
    public const string BusinessUser = "BusinessUser";
    public const string Manager = "Manager";
    public const string Auditor = "Auditor";
    public const string PlatformAdministrator = "PlatformAdministrator";

    public static readonly string[] All =
    {
        BusinessUser, Manager, Auditor, PlatformAdministrator
    };
}

/// <summary>Permission keys evaluated deny-by-default by <c>PermissionEvaluator</c>.</summary>
public static class Permissions
{
    public const string RecordsRead = "records.read";
    public const string RecordsCreate = "records.create";
    public const string DashboardRead = "dashboard.read";
    public const string ReportsExport = "reports.export";
    public const string AuditRead = "audit.read";
    public const string ConfigurationRead = "configuration.read";
    public const string ConfigurationWrite = "configuration.write";
    public const string RolesRead = "roles.read";
    public const string RolesWrite = "roles.write";

    public static readonly string[] All =
    {
        RecordsRead, RecordsCreate, DashboardRead, ReportsExport,
        AuditRead, ConfigurationRead, ConfigurationWrite, RolesRead, RolesWrite
    };
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // PII column per data design.
    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    // PII column per data design.
    [Required, MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int FailedSignInCount { get; set; }

    public DateTimeOffset? LastSignInUtc { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<UserRole> UserRoles { get; set; } = new();
}

public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Description { get; set; } = string.Empty;

    public List<UserRole> UserRoles { get; set; } = new();

    public List<RolePermission> RolePermissions { get; set; } = new();
}

public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(64)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Description { get; set; } = string.Empty;

    public List<RolePermission> RolePermissions { get; set; } = new();
}

public class UserRole
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public DateTimeOffset AssignedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }
}

public static class RecordStatuses
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Closed = "Closed";

    public static readonly string[] All = { Draft, Active, Closed };
}

public class BusinessRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(32)]
    public string Reference { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string Status { get; set; } = RecordStatuses.Draft;

    [Required, MaxLength(64)]
    public string Category { get; set; } = string.Empty;

    public Guid OwnerUserId { get; set; }

    [MaxLength(128)]
    public string OwnerDisplayName { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? IdempotencyKey { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(64)]
    public string EventType { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string EntityType { get; set; } = string.Empty;

    [MaxLength(64)]
    public string EntityId { get; set; } = string.Empty;

    public Guid? ActorUserId { get; set; }

    [MaxLength(256)]
    public string ActorDisplayName { get; set; } = "system";

    // PII column per data design.
    [MaxLength(64)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string Details { get; set; } = string.Empty;

    public bool Succeeded { get; set; } = true;

    public DateTimeOffset OccurredUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class SystemConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(1024)]
    public string Value { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(512)]
    public string LastChangeReason { get; set; } = string.Empty;

    [MaxLength(128)]
    public string LastChangedBy { get; set; } = string.Empty;

    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public static class ExportStatuses
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public class ReportExport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(16)]
    public string Format { get; set; } = "csv";

    [Required, MaxLength(32)]
    public string Status { get; set; } = ExportStatuses.Pending;

    public int RowCount { get; set; }

    [MaxLength(512)]
    public string FailureReason { get; set; } = string.Empty;

    public Guid RequestedByUserId { get; set; }

    [MaxLength(128)]
    public string RequestedByDisplayName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    [MaxLength(256)]
    public string FileName { get; set; } = string.Empty;

    public DateTimeOffset RequestedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedUtc { get; set; }

    /// <summary>Download link validity is 60 minutes per the approved API design.</summary>
    public DateTimeOffset ExpiresUtc { get; set; } = DateTimeOffset.UtcNow.AddMinutes(60);
}
