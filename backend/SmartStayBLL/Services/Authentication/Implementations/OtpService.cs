using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartStayDAL;
using System.Security.Cryptography;
using System.Text;

namespace SmartStayBLL
{
    public sealed class OtpService : IOtpService
    {
        private readonly SmartStayDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly OtpSettings _settings;

        public OtpService(
            SmartStayDbContext dbContext,
            IEmailService emailService,
            IOptions<OtpSettings> options)
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _settings = options.Value;
        }

        public async Task<SendOtpResult> SendAsync(
            string email,
            OtpPurpose purpose,
            Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = NormalizeEmail(email);
            var now = DateTimeOffset.UtcNow;

            var latestOtp = await _dbContext.OtpCodes
                .Where(otp =>
                    otp.NormalizedEmail == normalizedEmail &&
                    otp.Purpose == purpose &&
                    otp.UsedAt == null &&
                    otp.InvalidatedAt == null)
                .OrderByDescending(otp => otp.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestOtp is not null)
            {
                var resendAvailableAt = latestOtp.CreatedAt
                    .AddSeconds(_settings.ResendCooldownSeconds);

                if (resendAvailableAt > now)
                {
                    var remainingSeconds = (int)Math.Ceiling(
                        (resendAvailableAt - now).TotalSeconds);

                    throw new OtpCooldownException(
                                                     remainingSeconds);
                }

                latestOtp.InvalidatedAt = now;
            }

            var code = GenerateNumericCode(_settings.CodeLength);

            var otpCode = new OtpCode
            {
                Id = Guid.NewGuid(),
                NormalizedEmail = normalizedEmail,
                CodeHash = HashCode(code),
                Purpose = purpose,
                ExpiresAt = now.AddMinutes(
                    _settings.ExpirationMinutes),
                CreatedAt = now,
                FailedAttempts = 0,
                UserId = userId
            };

            _dbContext.OtpCodes.Add(otpCode);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            try
            {
                await _emailService.SendOtpAsync(
                    email.Trim(),
                    code,
                    cancellationToken);
            }
            catch
            {
                otpCode.InvalidatedAt = DateTimeOffset.UtcNow;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                throw;
            }

            return new SendOtpResult
            {
                ResendAvailableAfterSeconds =
                    _settings.ResendCooldownSeconds,

                ExpiresAt = otpCode.ExpiresAt
            };
        }

        public async Task<OtpVerificationResult> VerifyAsync(
            string email,
            string code,
            OtpPurpose purpose,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail = NormalizeEmail(email);
            var now = DateTimeOffset.UtcNow;

            var otp = await _dbContext.OtpCodes
                .Where(item =>
                    item.NormalizedEmail == normalizedEmail &&
                    item.Purpose == purpose &&
                    item.UsedAt == null &&
                    item.InvalidatedAt == null)
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (otp is null)
            {
                return Invalid(
                    "OTP_NOT_FOUND",
                    "No active verification code was found.");
            }

            if (otp.ExpiresAt <= now)
            {
                otp.InvalidatedAt = now;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                return Invalid(
                    "OTP_EXPIRED",
                    "The verification code has expired.");
            }

            if (otp.FailedAttempts >=
                _settings.MaximumFailedAttempts)
            {
                otp.InvalidatedAt = now;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                return Invalid(
                    "OTP_ATTEMPTS_EXCEEDED",
                    "Too many invalid attempts. Request a new code.");
            }

            var submittedCodeHash = HashCode(code);

            if (!FixedTimeEquals(
                    otp.CodeHash,
                    submittedCodeHash))
            {
                otp.FailedAttempts++;

                if (otp.FailedAttempts >=
                    _settings.MaximumFailedAttempts)
                {
                    otp.InvalidatedAt = now;
                }

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                return Invalid(
                    "OTP_INVALID",
                    "The verification code is invalid.");
            }

            otp.UsedAt = now;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new OtpVerificationResult
            {
                IsValid = true
            };
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToUpperInvariant();
        }

        private static string GenerateNumericCode(
            int codeLength)
        {
            if (codeLength <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(codeLength));
            }

            var maximumValue = (int)Math.Pow(10, codeLength);
            var minimumValue = maximumValue / 10;

            var number = RandomNumberGenerator.GetInt32(
                minimumValue,
                maximumValue);

            return number.ToString(
                $"D{codeLength}");
        }

        private string HashCode(string code)
        {
            var keyBytes = Encoding.UTF8.GetBytes(
                _settings.HashKey);

            var codeBytes = Encoding.UTF8.GetBytes(code);

            using var hmac = new HMACSHA256(keyBytes);

            var hashBytes = hmac.ComputeHash(codeBytes);

            return Convert.ToHexString(hashBytes);
        }

        private static bool FixedTimeEquals(
            string storedHash,
            string submittedHash)
        {
            try
            {
                var storedBytes =
                    Convert.FromHexString(storedHash);

                var submittedBytes =
                    Convert.FromHexString(submittedHash);

                return CryptographicOperations.FixedTimeEquals(
                    storedBytes,
                    submittedBytes);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static OtpVerificationResult Invalid(
            string errorCode,
            string message)
        {
            return new OtpVerificationResult
            {
                IsValid = false,
                ErrorCode = errorCode,
                ErrorMessage = message
            };
        }
    }
}