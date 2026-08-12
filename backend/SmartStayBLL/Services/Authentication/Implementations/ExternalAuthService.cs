using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace SmartStayBLL
{
    public sealed class ExternalAuthService
        : IExternalAuthService
    {
        private readonly HttpClient _httpClient;

        private readonly GoogleAuthSettings _googleSettings;

        private readonly FacebookAuthSettings _facebookSettings;

        public ExternalAuthService(
            HttpClient httpClient,
            IOptions<GoogleAuthSettings> googleOptions,
            IOptions<FacebookAuthSettings> facebookOptions)
        {
            _httpClient = httpClient;
            _googleSettings = googleOptions.Value;
            _facebookSettings = facebookOptions.Value;
        }

        public async Task<ExternalUserInfo> ValidateAsync(
            string provider,
            string token,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                throw new ArgumentException(
                    "External provider is required.");
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException(
                    "External authentication token is required.");
            }

            return provider.Trim().ToLowerInvariant() switch
            {
                "google" =>
                    await ValidateGoogleAsync(token),

                "facebook" =>
                    await ValidateFacebookAsync(
                        token,
                        cancellationToken),

                "apple" =>
                    throw new NotSupportedException(
                        "Apple authentication is not configured yet."),

                _ =>
                    throw new NotSupportedException(
                        "The external authentication provider is not supported.")
            };
        }

        private async Task<ExternalUserInfo> ValidateGoogleAsync(
            string token)
        {
            try
            {
                var validationSettings =
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience =
                        [
                            _googleSettings.ClientId
                        ]
                    };

                var payload =
                    await GoogleJsonWebSignature.ValidateAsync(
                        token,
                        validationSettings);

                if (string.IsNullOrWhiteSpace(payload.Subject))
                {
                    throw new UnauthorizedAccessException(
                        "Google user identifier is missing.");
                }

                if (string.IsNullOrWhiteSpace(payload.Email))
                {
                    throw new UnauthorizedAccessException(
                        "Google account email is missing.");
                }

                if (!payload.EmailVerified)
                {
                    throw new UnauthorizedAccessException(
                        "Google account email is not verified.");
                }

                return new ExternalUserInfo
                {
                    Provider = "Google",
                    ProviderKey = payload.Subject,
                    Email = payload.Email.Trim(),
                    EmailVerified = true,
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                    ProfileImageUrl = payload.Picture
                };
            }
            catch (InvalidJwtException)
            {
                throw new UnauthorizedAccessException(
                    "Invalid Google authentication token.");
            }
        }

        private async Task<ExternalUserInfo>
            ValidateFacebookAsync(
                string userAccessToken,
                CancellationToken cancellationToken)
        {
            /*
             * App access token format:
             * AppId|AppSecret
             *
             * The App Secret must stay on the backend only.
             */
            var appAccessToken =
                $"{_facebookSettings.AppId}|" +
                $"{_facebookSettings.AppSecret}";

            var encodedUserToken =
                Uri.EscapeDataString(userAccessToken);

            var encodedAppToken =
                Uri.EscapeDataString(appAccessToken);

            var graphVersion =
                _facebookSettings.GraphApiVersion;

            // 1. Validate the Facebook user access token.
            var debugTokenUrl =
                $"https://graph.facebook.com/" +
                $"{graphVersion}/debug_token" +
                $"?input_token={encodedUserToken}" +
                $"&access_token={encodedAppToken}";

            using var debugResponse =
                await _httpClient.GetAsync(
                    debugTokenUrl,
                    cancellationToken);

            if (!debugResponse.IsSuccessStatusCode)
            {
                throw new UnauthorizedAccessException(
                    "Facebook access token validation failed.");
            }

            var debugResult =
                await debugResponse.Content
                    .ReadFromJsonAsync<FacebookDebugTokenResponse>(
                        cancellationToken: cancellationToken);

            var debugData = debugResult?.Data;

            if (debugData is null ||
                !debugData.IsValid)
            {
                throw new UnauthorizedAccessException(
                    "Invalid Facebook access token.");
            }

            /*
             * Prevent accepting a token generated for
             * another Facebook application.
             */
            if (!string.Equals(
                    debugData.AppId,
                    _facebookSettings.AppId,
                    StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException(
                    "The Facebook token belongs to another application.");
            }

            if (string.IsNullOrWhiteSpace(debugData.UserId))
            {
                throw new UnauthorizedAccessException(
                    "Facebook user identifier is missing.");
            }

            if (debugData.ExpiresAt > 0)
            {
                var expiresAt =
                    DateTimeOffset.FromUnixTimeSeconds(
                        debugData.ExpiresAt);

                if (expiresAt <= DateTimeOffset.UtcNow)
                {
                    throw new UnauthorizedAccessException(
                        "Facebook access token has expired.");
                }
            }

            // 2. Read the Facebook user's profile.
            const string requestedFields =
                "id,email,first_name,last_name,picture.type(large)";

            var userInfoUrl =
                $"https://graph.facebook.com/" +
                $"{graphVersion}/me" +
                $"?fields={Uri.EscapeDataString(requestedFields)}" +
                $"&access_token={encodedUserToken}";

            using var userInfoResponse =
                await _httpClient.GetAsync(
                    userInfoUrl,
                    cancellationToken);

            if (!userInfoResponse.IsSuccessStatusCode)
            {
                throw new UnauthorizedAccessException(
                    "Unable to retrieve the Facebook user profile.");
            }

            var facebookUser =
                await userInfoResponse.Content
                    .ReadFromJsonAsync<FacebookUserInfo>(
                        cancellationToken: cancellationToken);

            if (facebookUser is null ||
                string.IsNullOrWhiteSpace(facebookUser.Id))
            {
                throw new UnauthorizedAccessException(
                    "Facebook did not return a valid user profile.");
            }

            /*
             * The ID returned from /me must match the user ID
             * returned by debug_token.
             */
            if (!string.Equals(
                    facebookUser.Id,
                    debugData.UserId,
                    StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException(
                    "Facebook user information does not match the access token.");
            }

            if (string.IsNullOrWhiteSpace(facebookUser.Email))
            {
                throw new UnauthorizedAccessException(
                    "Facebook did not provide an email address. " +
                    "Make sure the email permission was granted.");
            }

            return new ExternalUserInfo
            {
                Provider = "Facebook",

                ProviderKey = facebookUser.Id,

                Email = facebookUser.Email.Trim(),

                /*
                 * Facebook does not return a separate
                 * email_verified field in this response.
                 * For this project, we trust the email returned
                 * after server-side token validation.
                 */
                EmailVerified = true,

                FirstName = facebookUser.FirstName,

                LastName = facebookUser.LastName,

                ProfileImageUrl =
                    facebookUser.Picture?.Data?.Url
            };
        }
    }
}