using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartStayDAL;
using System.Security.Cryptography;
using System.Text;

namespace SmartStayBLL
{
    public sealed class RefreshTokenService
        : IRefreshTokenService
    {
        private readonly SmartStayDbContext _dbContext;
        private readonly JwtSettings _jwtSettings;

        public RefreshTokenService(
            SmartStayDbContext dbContext,
            IOptions<JwtSettings> jwtOptions)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            ArgumentNullException.ThrowIfNull(
                jwtOptions);

            _dbContext =
                dbContext;

            _jwtSettings =
                jwtOptions.Value;
        }

        public async Task<RefreshTokenIssueResult>
            CreateAsync(
                Guid userId,
                string? ipAddress,
                CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The user identifier is invalid.");
            }

            var now =
                DateTimeOffset.UtcNow;

            var rawToken =
                GenerateRawToken();

            var refreshToken =
                new RefreshToken
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        userId,

                    TokenHash =
                        HashToken(
                            rawToken),

                    CreatedAt =
                        now,

                    CreatedByIp =
                        ipAddress,

                    ExpiresAt =
                        now.AddDays(
                            _jwtSettings
                                .RefreshTokenExpirationDays)
                };

            _dbContext.RefreshTokens.Add(
                refreshToken);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new RefreshTokenIssueResult
            {
                Token =
                    rawToken,

                ExpiresAt =
                    refreshToken.ExpiresAt
            };
        }

        public async Task<RefreshTokenRotationResult>
            RotateAsync(
                string rawToken,
                string? ipAddress,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(
                    rawToken))
            {
                throw new UnauthorizedAccessException(
                    "Refresh token is missing.");
            }

            var tokenHash =
                HashToken(
                    rawToken);

            var now =
                DateTimeOffset.UtcNow;

            var storedToken =
                await _dbContext.RefreshTokens
                    .Include(token =>
                        token.User)
                    .SingleOrDefaultAsync(
                        token =>
                            token.TokenHash ==
                                tokenHash,
                        cancellationToken);

            if (storedToken is null)
            {
                throw new UnauthorizedAccessException(
                    "Invalid refresh token.");
            }

            if (storedToken.RevokedAt is not null)
            {
                /*
                 * Reusing a refresh token that was replaced
                 * during rotation may indicate token theft.
                 */
                if (storedToken.ReplacedByTokenId.HasValue)
                {
                    await RevokeAllForUserAsync(
                        storedToken.UserId,
                        ipAddress,
                        "Refresh token reuse detected.",
                        cancellationToken);

                    throw new UnauthorizedAccessException(
                        "Refresh token reuse detected.");
                }

                /*
                 * Tokens revoked by logout, logout-all-devices,
                 * account deactivation, or another explicit
                 * security action are not rotation-reuse events.
                 */
                throw new UnauthorizedAccessException(
                    "Refresh token has been revoked.");
            }

            if (storedToken.ExpiresAt <= now)
            {
                throw new UnauthorizedAccessException(
                    "Refresh token has expired.");
            }

            if (!storedToken.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "The user account is inactive.");
            }

            var newRawToken =
                GenerateRawToken();

            var newRefreshToken =
                new RefreshToken
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        storedToken.UserId,

                    TokenHash =
                        HashToken(
                            newRawToken),

                    CreatedAt =
                        now,

                    CreatedByIp =
                        ipAddress,

                    ExpiresAt =
                        now.AddDays(
                            _jwtSettings
                                .RefreshTokenExpirationDays)
                };

            storedToken.RevokedAt =
                now;

            storedToken.RevokedByIp =
                ipAddress;

            storedToken.RevocationReason =
                "Replaced by a new refresh token.";

            storedToken.ReplacedByTokenId =
                newRefreshToken.Id;

            _dbContext.RefreshTokens.Add(
                newRefreshToken);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new RefreshTokenRotationResult
            {
                UserId =
                    storedToken.UserId,

                Token =
                    newRawToken,

                ExpiresAt =
                    newRefreshToken.ExpiresAt
            };
        }

        public async Task<bool> RevokeAsync(
            string rawToken,
            string? ipAddress,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(
                    rawToken))
            {
                return false;
            }

            var tokenHash =
                HashToken(
                    rawToken);

            var storedToken =
                await _dbContext.RefreshTokens
                    .SingleOrDefaultAsync(
                        token =>
                            token.TokenHash ==
                                tokenHash,
                        cancellationToken);

            if (storedToken is null ||
                storedToken.RevokedAt is not null)
            {
                return false;
            }

            storedToken.RevokedAt =
                DateTimeOffset.UtcNow;

            storedToken.RevokedByIp =
                ipAddress;

            storedToken.RevocationReason =
                NormalizeRevocationReason(
                    reason);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return true;
        }

        public async Task RevokeAllForUserAsync(
            Guid userId,
            string? ipAddress,
            string reason,
            CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The user identifier is invalid.");
            }

            var now =
                DateTimeOffset.UtcNow;

            var normalizedReason =
                NormalizeRevocationReason(
                    reason);

            var activeTokens =
                await _dbContext.RefreshTokens
                    .Where(token =>
                        token.UserId == userId
                        &&
                        token.RevokedAt == null
                        &&
                        token.ExpiresAt > now)
                    .ToListAsync(
                        cancellationToken);

            foreach (var token in activeTokens)
            {
                token.RevokedAt =
                    now;

                token.RevokedByIp =
                    ipAddress;

                token.RevocationReason =
                    normalizedReason;
            }

            if (activeTokens.Count == 0)
            {
                return;
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        private static string GenerateRawToken()
        {
            var randomBytes =
                RandomNumberGenerator.GetBytes(
                    64);

            return Convert.ToHexString(
                randomBytes);
        }

        private static string HashToken(
            string rawToken)
        {
            var tokenBytes =
                Encoding.UTF8.GetBytes(
                    rawToken);

            var hashBytes =
                SHA256.HashData(
                    tokenBytes);

            return Convert.ToHexString(
                hashBytes);
        }

        private static string NormalizeRevocationReason(
            string? reason)
        {
            if (string.IsNullOrWhiteSpace(
                    reason))
            {
                return "Refresh token revoked.";
            }

            return reason.Trim();
        }
    }
}