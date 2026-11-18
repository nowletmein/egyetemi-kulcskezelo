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

        string adminEmail = configuration["SeedData:AdminEmail"] ?? configuration["SEEDADMINEMAIL"] ?? "admin@example.com";
        string adminPassword = configuration["SeedData:AdminPassword"] ?? configuration["SEEDADMINPASSWORD"] ?? throw new InvalidOperationException("Admin password not configured.");

        string portasEmail = configuration["SeedData:PortasEmail"] ?? configuration["SEEDPORTASEMAIL"] ?? "portas@example.com";
        string portasPassword = configuration["SeedData:PortasPassword"] ?? configuration["SEEDPORTASPASSWORD"] ?? "Password123!";

        string igazgatoEmail = configuration["SeedData:IgazgatoEmail"] ?? configuration["SEEDIGAZGATOEMAIL"] ?? "igazgato@example.com";
        string igazgatoPassword = configuration["SeedData:IgazgatoPassword"] ?? configuration["SEEDIGAZGATOPASSWORD"] ?? "Password123!";

        string oktatoEmail = configuration["SeedData:OktatoEmail"] ?? configuration["SEEDOKTATOEMAIL"] ?? "oktato@example.com";
        string oktatoPassword = configuration["SeedData:OktatoPassword"] ?? configuration["SEEDOKTATOPASSWORD"] ?? "Password123!";

        await SeedUserIfNotExists(context, adminEmail, adminPassword, "Rendszergazda", UserRole.Admin);
        await SeedUserIfNotExists(context, portasEmail, portasPassword, "Portás", UserRole.Portas);
        await SeedUserIfNotExists(context, igazgatoEmail, igazgatoPassword, "Igazgató", UserRole.Igazgato);
        await SeedUserIfNotExists(context, oktatoEmail, oktatoPassword, "Oktató", UserRole.Oktato);
    }

    private static async Task SeedUserIfNotExists(KulcsRendszerDbContext context, string email, string password, string name, UserRole roleEnum) {
        if (!await context.Users.AnyAsync(u => u.Email == email)) {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleEnum);

            var user = new User {
                Name = name,
                Email = email,
                RoleId = role?.Id ?? 1
            };

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
    }
}
