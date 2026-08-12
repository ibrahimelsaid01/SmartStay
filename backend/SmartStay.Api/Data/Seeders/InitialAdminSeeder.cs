using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartStayBLL;
using SmartStayDAL;

namespace SmartStay.Api
{
    public sealed class InitialAdminSeeder
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOptions<InitialAdminSettings> _adminOptions;
        private readonly ILogger<InitialAdminSeeder> _logger;

        public InitialAdminSeeder(
            UserManager<ApplicationUser> userManager,
            IOptions<InitialAdminSettings> adminOptions,
            ILogger<InitialAdminSeeder> logger)
        {
            ArgumentNullException.ThrowIfNull(userManager);
            ArgumentNullException.ThrowIfNull(adminOptions);
            ArgumentNullException.ThrowIfNull(logger);

            _userManager = userManager;
            _adminOptions = adminOptions;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            var settings = _adminOptions.Value;

            var normalizedEmail =
                settings.Email.Trim().ToLowerInvariant();

            var admin =
                await _userManager.FindByEmailAsync(
                    normalizedEmail);

            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    Id = Guid.NewGuid(),

                    Email = normalizedEmail,
                    UserName = normalizedEmail,

                    FirstName = settings.FirstName.Trim(),
                    LastName = settings.LastName.Trim(),

                    EmailConfirmed = true,
                    IsActive = true,
                    IsProfileCompleted = true,

                    CreatedAt = DateTimeOffset.UtcNow
                };

                var createResult =
                    await _userManager.CreateAsync(admin);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(
                        " | ",
                        createResult.Errors.Select(
                            error => error.Description));

                    _logger.LogCritical(
                        "Initial admin creation failed. Errors: {Errors}",
                        errors);

                    throw new InvalidOperationException(
                        "The initial admin account could not be created.");
                }

                _logger.LogInformation(
                    "Initial admin user was created.");
            }

            await EnsureRoleAsync(
                admin,
                RoleNames.User);

            await EnsureRoleAsync(
                admin,
                RoleNames.Admin);
        }

        private async Task EnsureRoleAsync(
            ApplicationUser user,
            string roleName)
        {
            var hasRole =
                await _userManager.IsInRoleAsync(
                    user,
                    roleName);

            if (hasRole)
            {
                return;
            }

            var addRoleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    roleName);

            if (!addRoleResult.Succeeded)
            {
                var errors = string.Join(
                    " | ",
                    addRoleResult.Errors.Select(
                        error => error.Description));

                _logger.LogCritical(
                    "Adding role {RoleName} to the initial admin failed. " +
                    "Errors: {Errors}",
                    roleName,
                    errors);

                throw new InvalidOperationException(
                    $"The {roleName} role could not be assigned " +
                    "to the initial admin.");
            }

            _logger.LogInformation(
                "Role {RoleName} was assigned to the initial admin.",
                roleName);
        }
    }
}