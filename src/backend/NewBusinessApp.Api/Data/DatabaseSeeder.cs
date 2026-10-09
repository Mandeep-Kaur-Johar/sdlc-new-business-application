using Microsoft.EntityFrameworkCore;
using NewBusinessApp.Api.Models;
using NewBusinessApp.Api.Services;

namespace NewBusinessApp.Api.Data;

/// <summary>
/// Seeds the reference data required by the approved security design:
/// four application roles, the role-to-permission map, baseline configuration keys
/// and, for non-production environments only, demonstration accounts.
/// </summary>
public static class DatabaseSeeder
{
    public static readonly IReadOnlyDictionary<string, string[]> RolePermissionMap =
        new Dictionary<string, string[]>
        {
            [ApplicationRoles.BusinessUser] = new[]
            {
                Permissions.RecordsRead, Permissions.RecordsCreate, Permissions.DashboardRead
            },
            [ApplicationRoles.Manager] = new[]
            {
                Permissions.RecordsRead, Permissions.RecordsCreate, Permissions.DashboardRead,
                Permissions.ReportsExport
            },
            [ApplicationRoles.Auditor] = new[]
            {
                Permissions.RecordsRead, Permissions.DashboardRead, Permissions.ReportsExport,
                Permissions.AuditRead, Permissions.ConfigurationRead
            },
            [ApplicationRoles.PlatformAdministrator] = Permissions.All
        };

    public static readonly IReadOnlyDictionary<string, (string Value, string Description)> DefaultConfiguration =
        new Dictionary<string, (string, string)>
        {
            ["records.pageSize.default"] = ("20", "Default page size for record search results."),
            ["records.pageSize.max"] = ("100", "Maximum page size accepted by the records API."),
            ["dashboard.cache.ttlSeconds"] = ("60", "Dashboard metrics cache time to live in seconds."),
            ["reports.export.maxRows"] = ("50000", "Maximum number of rows allowed in a report export."),
            ["audit.retentionDays"] = ("2555", "Audit event retention period in days (7 years).")
        };

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, bool seedDemoUsers, CancellationToken ct = default)
    {
        await EnsurePermissionsAsync(db, ct);
        await EnsureRolesAsync(db, ct);
        await EnsureConfigurationAsync(db, ct);

        if (seedDemoUsers)
        {
            await EnsureDemoUsersAsync(db, hasher, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsurePermissionsAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = await db.Permissions.Select(p => p.Key).ToListAsync(ct);
        foreach (var key in Permissions.All.Where(k => !existing.Contains(k)))
        {
            db.Permissions.Add(new Permission { Key = key, Description = $"Permission {key}" });
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureRolesAsync(AppDbContext db, CancellationToken ct)
    {
        var permissions = await db.Permissions.ToListAsync(ct);

        foreach (var (roleName, permissionKeys) in RolePermissionMap)
        {
            var role = await db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Name == roleName, ct);

            if (role is null)
            {
                role = new Role { Name = roleName, Description = $"{roleName} application role." };
                db.Roles.Add(role);
                await db.SaveChangesAsync(ct);
            }

            foreach (var key in permissionKeys)
            {
                var permission = permissions.First(p => p.Key == key);
                var alreadyMapped = role.RolePermissions.Any(rp => rp.PermissionId == permission.Id);
                if (!alreadyMapped)
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureConfigurationAsync(AppDbContext db, CancellationToken ct)
    {
        var existingKeys = await db.SystemConfigurations.Select(c => c.Key).ToListAsync(ct);

        foreach (var (key, definition) in DefaultConfiguration.Where(c => !existingKeys.Contains(c.Key)))
        {
            db.SystemConfigurations.Add(new SystemConfiguration
            {
                Key = key,
                Value = definition.Value,
                Description = definition.Description,
                LastChangeReason = "Initial platform baseline.",
                LastChangedBy = "system"
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureDemoUsersAsync(AppDbContext db, IPasswordHasher hasher, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        // Non-production demonstration credentials. The password is read from configuration at
        // startup and is never persisted in clear text; only the PBKDF2 hash is stored.
        var demoPassword = Environment.GetEnvironmentVariable("SEED_DEMO_PASSWORD") ?? "ChangeMe123!";

        var roles = await db.Roles.ToListAsync(ct);

        var seeds = new[]
        {
            ("admin@example.com", "Platform Administrator", ApplicationRoles.PlatformAdministrator),
            ("manager@example.com", "Business Manager", ApplicationRoles.Manager),
            ("auditor@example.com", "Compliance Auditor", ApplicationRoles.Auditor),
            ("user@example.com", "Business User", ApplicationRoles.BusinessUser)
        };

        foreach (var (email, displayName, roleName) in seeds)
        {
            var user = new User
            {
                Email = email,
                DisplayName = displayName,
                PasswordHash = hasher.Hash(demoPassword),
                IsActive = true
            };

            db.Users.Add(user);
            db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roles.First(r => r.Name == roleName).Id
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
