using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOtpService _otpService;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IExternalAuthService _externalAuthService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IOtpService otpService,
            IJwtService jwtService,
            IRefreshTokenService refreshTokenService,
            IExternalAuthService externalAuthService)
        {
            _userManager = userManager;
            _otpService = otpService;
            _jwtService = jwtService;
            _refreshTokenService = refreshTokenService;
            _externalAuthService = externalAuthService;
        }

        public Task<SendOtpResult> SendOtpAsync(
            SendOtpRequest request,
            CancellationToken cancellationToken = default)
        {
            return _otpService.SendAsync(
                request.Email,
                OtpPurpose.Authentication,
                cancellationToken: cancellationToken);
        }

        public async Task<AuthResult> VerifyOtpAsync(
            VerifyOtpRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var otpVerification =
                await _otpService.VerifyAsync(
                    request.Email,
                    request.Code,
                    OtpPurpose.Authentication,
                    cancellationToken);

            if (!otpVerification.IsValid)
            {
                throw new UnauthorizedAccessException(
                    otpVerification.ErrorMessage
                    ?? "The verification code is invalid.");
            }

            var email =
                request.Email.Trim();

            var user =
                await _userManager.FindByEmailAsync(
                    email);

            var isNewUser =
                user is null;

            if (user is null)
            {
                user =
                    new ApplicationUser
                    {
                        Id =
                            Guid.NewGuid(),

                        Email =
                            email,

                        UserName =
                            email,

                        EmailConfirmed =
                            true,

                        IsActive =
                            true,

                        IsProfileCompleted =
                            false,

                        CreatedAt =
                            DateTimeOffset.UtcNow
                    };

                /*
                 * Passwordless account creation.
                 *
                 * No password is provided to Identity.
                 */
                var createResult =
                    await _userManager.CreateAsync(
                        user);

                EnsureIdentitySucceeded(
                    createResult,
                    "Unable to create the user account.");

                var roleResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        RoleNames.User);

                EnsureIdentitySucceeded(
                    roleResult,
                    "Unable to assign the User role.");
            }
            else
            {
                if (!user.IsActive)
                {
                    throw new UnauthorizedAccessException(
                        "This account is inactive.");
                }

                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed =
                        true;

                    user.UpdatedAt =
                        DateTimeOffset.UtcNow;

                    var updateResult =
                        await _userManager.UpdateAsync(
                            user);

                    EnsureIdentitySucceeded(
                        updateResult,
                        "Unable to confirm the email address.");
                }
            }

            return await CreateAuthenticationResultAsync(
                user,
                ipAddress,
                isNewUser,
                cancellationToken);
        }

        public async Task<AuthResult> ExternalLoginAsync(
            ExternalLoginRequest request,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var externalUser =
                await _externalAuthService.ValidateAsync(
                    request.Provider,
                    request.Token,
                    cancellationToken);

            if (!externalUser.EmailVerified)
            {
                throw new UnauthorizedAccessException(
                    "The external provider did not verify this email address.");
            }

            var user =
                await _userManager.FindByLoginAsync(
                    externalUser.Provider,
                    externalUser.ProviderKey);

            var isNewUser =
                false;

            if (user is null)
            {
                user =
                    await _userManager.FindByEmailAsync(
                        externalUser.Email);

                if (user is null)
                {
                    isNewUser =
                        true;

                    user =
                        new ApplicationUser
                        {
                            Id =
                                Guid.NewGuid(),

                            Email =
                                externalUser.Email,

                            UserName =
                                externalUser.Email,

                            EmailConfirmed =
                                true,

                            FirstName =
                                externalUser.FirstName,

                            LastName =
                                externalUser.LastName,

                            ProfileImageUrl =
                                externalUser.ProfileImageUrl,

                            IsActive =
                                true,

                            IsProfileCompleted =
                                !string.IsNullOrWhiteSpace(
                                    externalUser.FirstName)
                                &&
                                !string.IsNullOrWhiteSpace(
                                    externalUser.LastName),

                            CreatedAt =
                                DateTimeOffset.UtcNow
                        };

                    var createResult =
                        await _userManager.CreateAsync(
                            user);

                    EnsureIdentitySucceeded(
                        createResult,
                        "Unable to create the external user.");

                    var roleResult =
                        await _userManager.AddToRoleAsync(
                            user,
                            RoleNames.User);

                    EnsureIdentitySucceeded(
                        roleResult,
                        "Unable to assign the User role.");
                }

                var loginInfo =
                    new UserLoginInfo(
                        externalUser.Provider,
                        externalUser.ProviderKey,
                        externalUser.Provider);

                var addLoginResult =
                    await _userManager.AddLoginAsync(
                        user,
                        loginInfo);

                EnsureIdentitySucceeded(
                    addLoginResult,
                    "Unable to link the external account.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            return await CreateAuthenticationResultAsync(
                user,
                ipAddress,
                isNewUser,
                cancellationToken);
        }

        public async Task<AuthenticatedUserResponse>
            CompleteProfileAsync(
                Guid userId,
                CompleteProfileRequest request,
                CancellationToken cancellationToken = default)
        {
            var user =
                await _userManager.FindByIdAsync(
                    userId.ToString());

            if (user is null)
            {
                throw new KeyNotFoundException(
                    "The user was not found.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            user.FirstName =
                request.FirstName.Trim();

            user.LastName =
                request.LastName.Trim();

            user.IsProfileCompleted =
                true;

            user.UpdatedAt =
                DateTimeOffset.UtcNow;

            var updateResult =
                await _userManager.UpdateAsync(
                    user);

            EnsureIdentitySucceeded(
                updateResult,
                "Unable to complete the user profile.");

            return await MapUserAsync(
                user);
        }

        public async Task<AuthResult> RefreshAsync(
            string refreshToken,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            var rotationResult =
                await _refreshTokenService.RotateAsync(
                    refreshToken,
                    ipAddress,
                    cancellationToken);

            var user =
                await _userManager.FindByIdAsync(
                    rotationResult.UserId.ToString());

            if (user is null ||
                !user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "The user account is not available.");
            }

            var roles =
                await _userManager.GetRolesAsync(
                    user);

            var accessToken =
                _jwtService.GenerateAccessToken(
                    user,
                    roles.ToArray());

            var userResponse =
                await MapUserAsync(
                    user);

            return new AuthResult
            {
                AccessToken =
                    accessToken.Token,

                AccessTokenExpiresAt =
                    accessToken.ExpiresAt,

                RefreshToken =
                    rotationResult.Token,

                RefreshTokenExpiresAt =
                    rotationResult.ExpiresAt,

                IsNewUser =
                    false,

                NextStep =
                    ResolveNextStep(
                        userResponse),

                User =
                    userResponse
            };
        }

        public async Task LogoutAsync(
            string refreshToken,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            await _refreshTokenService.RevokeAsync(
                refreshToken,
                ipAddress,
                "User logged out.",
                cancellationToken);
        }

        public async Task LogoutFromAllDevicesAsync(
            Guid userId,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "The authenticated user identifier is invalid.");
            }

            /*
             * We only verify that the user still exists.
             *
             * An inactive user is still allowed to revoke
             * their existing sessions.
             */
            var userExists =
                await _userManager.Users
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.Id == userId,
                        cancellationToken);

            if (!userExists)
            {
                throw new KeyNotFoundException(
                    "The user was not found.");
            }

            await _refreshTokenService
                .RevokeAllForUserAsync(
                    userId,
                    ipAddress,
                    "User logged out from all devices.",
                    cancellationToken);
        }

        public async Task<AuthenticatedUserResponse>
            GetCurrentUserAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var user =
                await _userManager.Users
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == userId,
                        cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException(
                    "The user was not found.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            return await MapUserAsync(
                user);
        }

        private async Task<AuthResult>
            CreateAuthenticationResultAsync(
                ApplicationUser user,
                string? ipAddress,
                bool isNewUser,
                CancellationToken cancellationToken)
        {
            var roles =
                await _userManager.GetRolesAsync(
                    user);

            var accessToken =
                _jwtService.GenerateAccessToken(
                    user,
                    roles.ToArray());

            var refreshToken =
                await _refreshTokenService.CreateAsync(
                    user.Id,
                    ipAddress,
                    cancellationToken);

            var userResponse =
                await MapUserAsync(
                    user);

            return new AuthResult
            {
                AccessToken =
                    accessToken.Token,

                AccessTokenExpiresAt =
                    accessToken.ExpiresAt,

                RefreshToken =
                    refreshToken.Token,

                RefreshTokenExpiresAt =
                    refreshToken.ExpiresAt,

                IsNewUser =
                    isNewUser,

                NextStep =
                    ResolveNextStep(
                        userResponse),

                User =
                    userResponse
            };
        }

        private async Task<AuthenticatedUserResponse>
            MapUserAsync(
                ApplicationUser user)
        {
            var roles =
                await _userManager.GetRolesAsync(
                    user);

            return new AuthenticatedUserResponse
            {
                Id =
                    user.Id,

                Email =
                    user.Email
                    ?? string.Empty,

                FirstName =
                    user.FirstName,

                LastName =
                    user.LastName,

                ProfileImageUrl =
                    user.ProfileImageUrl,

                IsProfileCompleted =
                    user.IsProfileCompleted,

                Roles =
                    roles.ToList()
            };
        }

        private static string ResolveNextStep(
            AuthenticatedUserResponse user)
        {
            if (!user.IsProfileCompleted)
            {
                return AuthNextSteps.CompleteProfile;
            }

            if (user.Roles.Contains(
                    RoleNames.Admin,
                    StringComparer.OrdinalIgnoreCase))
            {
                return AuthNextSteps.AdminDashboard;
            }

            if (user.Roles.Contains(
                    RoleNames.Host,
                    StringComparer.OrdinalIgnoreCase))
            {
                return AuthNextSteps.HostDashboard;
            }

            return AuthNextSteps.Properties;
        }

        private static void EnsureIdentitySucceeded(
            IdentityResult result,
            string defaultMessage)
        {
            if (result.Succeeded)
            {
                return;
            }

            var errors =
                string.Join(
                    " ",
                    result.Errors.Select(
                        error =>
                            error.Description));

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(
                    errors)
                    ? defaultMessage
                    : errors);
        }
    }
}