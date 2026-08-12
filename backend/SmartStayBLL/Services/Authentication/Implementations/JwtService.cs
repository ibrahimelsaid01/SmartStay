using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartStayDAL;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartStayBLL
{
    public sealed class JwtService : IJwtService
    {
        private readonly JwtSettings _settings;

        public JwtService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public AccessTokenResult GenerateAccessToken(
            ApplicationUser user,
            IReadOnlyCollection<string> roles)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new InvalidOperationException(
                    "The user must have an email before generating an access token.");
            }

            var now = DateTimeOffset.UtcNow;

            var expiresAt = now.AddMinutes(
                _settings.AccessTokenExpirationMinutes);

            var claims = new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new(
                    ClaimTypes.Email,
                    user.Email),

                new(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),

                new(
                    JwtRegisteredClaimNames.Iat,
                    now.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            if (!string.IsNullOrWhiteSpace(user.FirstName))
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.GivenName,
                        user.FirstName));
            }

            if (!string.IsNullOrWhiteSpace(user.LastName))
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Surname,
                        user.LastName));
            }

            foreach (var role in roles.Distinct())
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Role,
                        role));
            }

            var keyBytes = Encoding.UTF8.GetBytes(
                _settings.Key);

            var securityKey =
                new SymmetricSecurityKey(keyBytes);

            var signingCredentials =
                new SigningCredentials(
                    securityKey,
                    SecurityAlgorithms.HmacSha256);

            var jwtToken = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expiresAt.UtcDateTime,
                signingCredentials: signingCredentials);

            var serializedToken =
                new JwtSecurityTokenHandler()
                    .WriteToken(jwtToken);

            return new AccessTokenResult
            {
                Token = serializedToken,
                ExpiresAt = expiresAt
            };
        }
    }
}