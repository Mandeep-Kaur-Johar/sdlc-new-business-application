using System.ComponentModel.DataAnnotations;

namespace NewBusinessApp.Api.DTOs;

// ---------- Shared ----------

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

// ---------- Story 2864: User Sign-In ----------

public class SignInRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;
}

public class SignInResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    /// <summary>Access token lifetime in seconds (3600 = 60 minutes).</summary>
    public int ExpiresInSeconds { get; set; }
    public UserProfileDto User { get; set; } = new();
}

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
}

// ---------- Story 2865: Role-Based Access ----------

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
}

public class UpdateRolePermissionsRequest
{
    [Required]
    public string RoleName { get; set; } = string.Empty;

    [Required]
    public List<string> Permissions { get; set; } = new();
}

public class AssignUserRolesRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one role must be supplied.")]
    public List<string> Roles { get; set; } = new();
}

// ---------- Stories 2867 / 2868: Business Records ----------

public class CreateRecordRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(64, ErrorMessage = "Category cannot exceed 64 characters.")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = "Draft";
}

public class RecordDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}

public class RecordSearchQuery
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public DateTimeOffset? CreatedFromUtc { get; set; }
    public DateTimeOffset? CreatedToUtc { get; set; }
    public string SortBy { get; set; } = "createdUtc";
    public string SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    /// <summary>Server enforces a maximum of 100 items per page.</summary>
    public int PageSize { get; set; } = 20;
}

// ---------- Story 2870: Operational Dashboard ----------

public class DashboardMetricsDto
{
    public int TotalRecords { get; set; }
    public int DraftRecords { get; set; }
    public int ActiveRecords { get; set; }
    public int ClosedRecords { get; set; }
    public int RecordsCreatedLast7Days { get; set; }
    public int RecordsCreatedLast30Days { get; set; }
    public IReadOnlyList<CategoryCountDto> TopCategories { get; set; } = Array.Empty<CategoryCountDto>();
    public IReadOnlyList<TrendPointDto> DailyTrend { get; set; } = Array.Empty<TrendPointDto>();
    public DateTimeOffset GeneratedUtc { get; set; }
    public bool FromCache { get; set; }
}

public class CategoryCountDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TrendPointDto
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}

// ---------- Story 2871: Export Reports ----------

public class ExportRequest
{
    /// <summary>Supported formats: csv, pdf.</summary>
    [Required]
    public string Format { get; set; } = "csv";

    public RecordSearchQuery Filter { get; set; } = new();
}

public class ExportJobDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public DateTimeOffset RequestedUtc { get; set; }
    public DateTimeOffset? CompletedUtc { get; set; }
    public DateTimeOffset ExpiresUtc { get; set; }
}

// ---------- Story 2873: Audit Activity ----------

public class AuditEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ActorDisplayName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public DateTimeOffset OccurredUtc { get; set; }
}

public class AuditSearchQuery
{
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public string? Actor { get; set; }
    public string? EventType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

// ---------- Story 2874: System Configuration ----------

public class ConfigurationDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LastChangeReason { get; set; } = string.Empty;
    public string LastChangedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedUtc { get; set; }
}

public class UpdateConfigurationRequest
{
    [Required(ErrorMessage = "Value is required.")]
    [StringLength(1024, ErrorMessage = "Value cannot exceed 1024 characters.")]
    public string Value { get; set; } = string.Empty;

    [Required(ErrorMessage = "A change reason is required for every configuration update.")]
    [StringLength(512, MinimumLength = 5, ErrorMessage = "Change reason must be between 5 and 512 characters.")]
    public string ChangeReason { get; set; } = string.Empty;
}
