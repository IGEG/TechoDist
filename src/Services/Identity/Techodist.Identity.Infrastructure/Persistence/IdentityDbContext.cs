using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Techodist.Identity.Infrastructure.Persistence;

/// <summary>
/// БД Identity Service: таблицы ASP.NET Core Identity (AspNetUsers/AspNetRoles/...) и
/// таблицы OpenIddict (OpenIddictApplications/Authorizations/Scopes/Tokens) в одной базе.
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<AdminUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Подключаем модель OpenIddict к этому же DbContext (OpenIddict.EntityFrameworkCore).
        builder.UseOpenIddict();

        builder.Entity<AdminUser>(user =>
        {
            user.Property(u => u.DisplayName)
                .HasMaxLength(150)
                .IsRequired();

            user.Property(u => u.IsActive)
                .HasDefaultValue(true);
        });
    }
}
