using Microsoft.EntityFrameworkCore;
using NewBusinessApp.Api.Data;
using NewBusinessApp.Api.Models;

namespace NewBusinessApp.Api.Repositories;

// ---------------- Users, roles and permissions (Stories 2864, 2865) ----------------

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetPermissionKeysAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalised = (email ?? string.Empty).Trim().ToLowerInvariant();
        return _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalised, ct);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken ct = default)
        => await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetPermissionKeysAsync(Guid userId, CancellationToken ct = default)
        => await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (_, rp) => rp.PermissionId)
            .Join(_db.Permissions, pid => pid, p => p.Id, (_, p) => p.Key)
            .Distinct()
            .OrderBy(key => key)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
        => await _db.Users.OrderBy(u => u.DisplayName).ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetAllWithPermissionsAsync(CancellationToken ct = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Permission>> GetPermissionsAsync(CancellationToken ct = default);
    Task ReplaceRolePermissionsAsync(Role role, IEnumerable<Permission> permissions, CancellationToken ct = default);
    Task ReplaceUserRolesAsync(Guid userId, IEnumerable<Role> roles, CancellationToken ct = default);
}

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _db;

    public RoleRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Role>> GetAllWithPermissionsAsync(CancellationToken ct = default)
        => await _db.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
        => _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Name == name, ct);

    public async Task<IReadOnlyList<Permission>> GetPermissionsAsync(CancellationToken ct = default)
        => await _db.Permissions.OrderBy(p => p.Key).ToListAsync(ct);

    public async Task ReplaceRolePermissionsAsync(Role role, IEnumerable<Permission> permissions, CancellationToken ct = default)
    {
        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == role.Id).ToListAsync(ct);
        _db.RolePermissions.RemoveRange(existing);

        foreach (var permission in permissions)
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task ReplaceUserRolesAsync(Guid userId, IEnumerable<Role> roles, CancellationToken ct = default)
    {
        var existing = await _db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(ct);
        _db.UserRoles.RemoveRange(existing);

        foreach (var role in roles)
        {
            _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });
        }

        await _db.SaveChangesAsync(ct);
    }
}

// ---------------- Business records (Stories 2867, 2868) ----------------

public interface IRecordRepository
{
    Task<BusinessRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BusinessRecord?> GetByIdempotencyKeyAsync(string key, DateTimeOffset notBeforeUtc, CancellationToken ct = default);
    Task<BusinessRecord> AddAsync(BusinessRecord record, AuditEvent auditEvent, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    IQueryable<BusinessRecord> Query();
}

public class RecordRepository : IRecordRepository
{
    private readonly AppDbContext _db;

    public RecordRepository(AppDbContext db) => _db = db;

    public Task<BusinessRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.BusinessRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<BusinessRecord?> GetByIdempotencyKeyAsync(string key, DateTimeOffset notBeforeUtc, CancellationToken ct = default)
        => _db.BusinessRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IdempotencyKey == key && r.CreatedUtc >= notBeforeUtc, ct);

    /// <summary>
    /// Persists the record and its audit event in a single transaction so that no untracked
    /// data change can exist, as required by the approved security design.
    /// </summary>
    public async Task<BusinessRecord> AddAsync(BusinessRecord record, AuditEvent auditEvent, CancellationToken ct = default)
    {
        _db.BusinessRecords.Add(record);
        _db.AuditEvents.Add(auditEvent);
        await _db.SaveChangesAsync(ct);
        return record;
    }

    public Task<int> CountAsync(CancellationToken ct = default) => _db.BusinessRecords.CountAsync(ct);

    public IQueryable<BusinessRecord> Query() => _db.BusinessRecords.AsNoTracking();
}

// ---------------- Audit (Story 2873) ----------------

public interface IAuditRepository
{
    Task AddAsync(AuditEvent auditEvent, CancellationToken ct = default);
    IQueryable<AuditEvent> Query();
}

public class AuditRepository : IAuditRepository
{
    private readonly AppDbContext _db;

    public AuditRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(AuditEvent auditEvent, CancellationToken ct = default)
    {
        _db.AuditEvents.Add(auditEvent);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>The audit table is append only; no update or delete path is exposed.</summary>
    public IQueryable<AuditEvent> Query() => _db.AuditEvents.AsNoTracking();
}

// ---------------- System configuration (Story 2874) ----------------

public interface IConfigRepository
{
    Task<SystemConfiguration?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<SystemConfiguration>> GetAllAsync(CancellationToken ct = default);
    Task UpdateAsync(SystemConfiguration configuration, AuditEvent auditEvent, CancellationToken ct = default);
}

public class ConfigRepository : IConfigRepository
{
    private readonly AppDbContext _db;

    public ConfigRepository(AppDbContext db) => _db = db;

    public Task<SystemConfiguration?> GetByKeyAsync(string key, CancellationToken ct = default)
        => _db.SystemConfigurations.FirstOrDefaultAsync(c => c.Key == key, ct);

    public async Task<IReadOnlyList<SystemConfiguration>> GetAllAsync(CancellationToken ct = default)
        => await _db.SystemConfigurations.AsNoTracking().OrderBy(c => c.Key).ToListAsync(ct);

    public async Task UpdateAsync(SystemConfiguration configuration, AuditEvent auditEvent, CancellationToken ct = default)
    {
        _db.SystemConfigurations.Update(configuration);
        _db.AuditEvents.Add(auditEvent);
        await _db.SaveChangesAsync(ct);
    }
}

// ---------------- Report exports (Story 2871) ----------------

public interface IExportRepository
{
    Task AddAsync(ReportExport export, CancellationToken ct = default);
    Task UpdateAsync(ReportExport export, CancellationToken ct = default);
    Task<ReportExport?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ReportExport>> GetRecentAsync(Guid userId, int take, CancellationToken ct = default);
}

public class ExportRepository : IExportRepository
{
    private readonly AppDbContext _db;

    public ExportRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(ReportExport export, CancellationToken ct = default)
    {
        _db.ReportExports.Add(export);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ReportExport export, CancellationToken ct = default)
    {
        _db.ReportExports.Update(export);
        await _db.SaveChangesAsync(ct);
    }

    public Task<ReportExport?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ReportExports.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<ReportExport>> GetRecentAsync(Guid userId, int take, CancellationToken ct = default)
        => await _db.ReportExports
            .AsNoTracking()
            .Where(e => e.RequestedByUserId == userId)
            .OrderByDescending(e => e.RequestedUtc)
            .Take(take)
            .ToListAsync(ct);
}
