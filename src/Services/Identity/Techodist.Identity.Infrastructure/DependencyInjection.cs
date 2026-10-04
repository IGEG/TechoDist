using Techodist.Identity.Application.Abstractions;
using Techodist.Identity.Domain.ValueObjects;
using Techodist.Identity.Infrastructure.Identity;
using Techodist.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Techodist.Identity.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Регистрация инфраструктуры Identity: PostgreSQL, ASP.NET Core Identity (без cookie-схем),
    /// OpenIddict Server на том же DbContext, health-check БД.
    /// </summary>
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var connectionString = configuration.GetConnectionString("IdentityDb")
            ?? throw new InvalidOperationException("Не задана строка подключения 'IdentityDb'.");

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName)));

        // Identity без cookie-схем: аутентификация API — только по JWT (access token).
        services.AddIdentityCore<AdminUser>(options =>
            {
                options.Password.RequiredLength = PasswordPolicy.MinimumLength;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;

                options.User.RequireUniqueEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                // Регистрации и подтверждения e-mail нет (ADR 0004).
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddEntityFrameworkStores<IdentityDbContext>();

        services.AddHttpContextAccessor();
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddOpenIddict()
            .AddCore(options => options
                .UseEntityFrameworkCore()
                .UseDbContext<IdentityDbContext>())
            .AddServer(options => OpenIddictServerConfiguration.Configure(
                options,
                configuration,
                environment.IsDevelopment()));

        services.AddHealthChecks()
            .AddDbContextCheck<IdentityDbContext>("identity-db");

        return services;
    }
}
