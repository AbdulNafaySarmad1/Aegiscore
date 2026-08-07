using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace AegisCore.Infrastructure.Deployment.Seeders;

public sealed class DemoSeeder
{
    private readonly AegisDbContext _db;
    private readonly ILogger<DemoSeeder> _logger;

    // ─── BCrypt-compatible hash using HMACSHA256 ───
    private static string HashPassword(string password)
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(password));
        var hash = hmac.ComputeHash(salt);
        return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
    }

    // ─── Demo account definitions ───
    private static readonly (string FirstName, string LastName, string Email, string Password, string Role, string Program, int Semester, decimal CGPA, string Description)[] DemoAccounts =
    [
        ("Prof. Dr. Waseem", "Ahmed", "vc@iobm.demo", "Lambo$Aventador1", "SuperAdmin", "", 0, 0m, "Vice Chancellor"),
        ("Dr. Sana", "Mirza", "registrar@iobm.demo", "Ferrari$Roma22", "Registrar", "", 0, 0m, "Registrar"),
        ("Mr. Khalid", "Hussain", "finance@iobm.demo", "Porsche$GT3RS", "Finance", "", 0, 0m, "Finance Head"),
        ("Ms. Ayesha", "Qureshi", "exams@iobm.demo", "McLaren$720S!", "ExamController", "", 0, 0m, "Exam Controller"),
        ("Mr. Bilal", "Raza", "it@iobm.demo", "Bugatti$Chiron9", "ITHead", "", 0, 0m, "IT Head"),
        ("Ms. Nadia", "Shah", "hr@iobm.demo", "Aston$Martin77", "HR", "", 0, 0m, "HR Head"),
        ("Dr. Imran", "Siddiqui", "faculty@iobm.demo", "Pagani$Huayra3", "Faculty", "", 0, 0m, "Faculty Member"),
        ("Ali", "Hassan", "student@iobm.demo", "Koenigsegg$One1", "Student", "BS Computer Science", 5, 3.67m, "Student"),
        ("Fatima", "Malik", "student2@iobm.demo", "Rimac$Nevera88", "Student", "BBA", 3, 3.42m, "Student 2"),
    ];

    public DemoSeeder(AegisDbContext db, ILogger<DemoSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(Guid tenantId, CancellationToken ct = default)
    {
        _logger.LogInformation("Seeding 9 demo accounts for IoBM pilot...");

        foreach (var (firstName, lastName, email, password, roleName, program, semester, cgpa, desc) in DemoAccounts)
        {
            var existing = await _db.Users.IgnoreQueryFilters()
                .AnyAsync(u => u.TenantId == tenantId && u.Email == email, ct);
            if (existing)
            {
                _logger.LogInformation("  {Email} already exists — skipping", email);
                continue;
            }

            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                TenantId = tenantId,
                FirstName = firstName,
                LastName = lastName,
                Username = email,
                Email = email,
                PasswordHash = HashPassword(password),
                Status = UserStatus.Active,
                AuthProvider = AuthProvider.Local,
                CreatedAt = DateTime.UtcNow,
            };
            _db.Users.Add(user);

            var role = await _db.Roles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == roleName, ct);
            if (role is null)
            {
                role = new Role { Id = Guid.NewGuid(), TenantId = tenantId, Name = roleName, IsSystem = false, Description = $"Demo role: {desc}" };
                _db.Roles.Add(role);
            }

            _db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, RoleId = role.Id });

            _logger.LogInformation("  ✓ {Email} ({roleName}) — {desc}", email, roleName, desc);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Demo accounts seeded successfully for tenant {TenantId}", tenantId);
    }
}
