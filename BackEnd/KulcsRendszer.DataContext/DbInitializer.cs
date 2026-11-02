using BCrypt.Net;
using KulcsRendszer.DataContext.Context;
using KulcsRendszer.DataContext.Entities;
using KulcsRendszer.DataContext.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DbInitializer {
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration) {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KulcsRendszerDbContext>();

        if (!await context.Roles.AnyAsync()) {
            foreach (var roleEnum in Enum.GetValues<UserRole>()) {
                context.Roles.Add(new Role { Name = roleEnum });
            }
            await context.SaveChangesAsync();
        }

        string adminEmail = configuration["SeedData:AdminEmail"] ?? "admin@example.com";
        string adminPassword = configuration["SeedData:AdminPassword"] ?? throw new InvalidOperationException("Admin password not configured.");

        if (!await context.Users.AnyAsync(u => u.Email == adminEmail)) {
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == UserRole.Admin);

            var user = new User {
                Name = "Rendszergazda",
                Email = adminEmail,
                RoleId = adminRole?.Id ?? 1
            };

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);

            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
    }
}
