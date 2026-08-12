using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class ProfileService
        : IProfileService
    {
        private readonly SmartStayDbContext _dbContext;

        private readonly UserManager<ApplicationUser>
            _userManager;

        private readonly IImageStorageService
            _imageStorageService;

        private readonly CloudinarySettings
            _cloudinarySettings;

        private readonly ILogger<ProfileService>
            _logger;

        public ProfileService(
            SmartStayDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IImageStorageService imageStorageService,
            IOptions<CloudinarySettings>
                cloudinaryOptions,
            ILogger<ProfileService> logger)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            ArgumentNullException.ThrowIfNull(
                userManager);

            ArgumentNullException.ThrowIfNull(
                imageStorageService);

            ArgumentNullException.ThrowIfNull(
                cloudinaryOptions);

            ArgumentNullException.ThrowIfNull(
                logger);

            _dbContext = dbContext;

            _userManager = userManager;

            _imageStorageService =
                imageStorageService;

            _cloudinarySettings =
                cloudinaryOptions.Value;

            _logger = logger;
        }

        public async Task<UserProfileResponse>
            GetAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var user =
                await GetActiveUserAsync(
                    userId,
                    cancellationToken);

            return await MapToResponseAsync(
                user);
        }

        public async Task<UserProfileResponse>
            UpdateAsync(
                Guid userId,
                UpdateUserProfileRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            var user =
                await GetActiveUserAsync(
                    userId,
                    cancellationToken);

            var firstName =
                request.FirstName.Trim();

            var lastName =
                request.LastName.Trim();

            if (string.IsNullOrWhiteSpace(
                    firstName))
            {
                throw new ArgumentException(
                    "First name is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    lastName))
            {
                throw new ArgumentException(
                    "Last name is required.");
            }

            if (request.Birthday.HasValue
                &&
                request.Birthday.Value >
                    DateOnly.FromDateTime(
                        DateTime.UtcNow))
            {
                throw new ArgumentException(
                    "Birthday cannot be in the future.");
            }

            if (request.Gender.HasValue
                &&
                !Enum.IsDefined(
                    typeof(UserGender),
                    request.Gender.Value))
            {
                throw new ArgumentException(
                    "The selected gender is invalid.");
            }

            var phoneNumber =
                NormalizeOptionalValue(
                    request.PhoneNumber);

            var currentTime =
                DateTimeOffset.UtcNow;

            user.FirstName =
                firstName;

            user.LastName =
                lastName;

            user.Gender =
                request.Gender;

            user.Birthday =
                request.Birthday;

            user.Country =
                NormalizeOptionalValue(
                    request.Country);

            user.Address =
                NormalizeOptionalValue(
                    request.Address);

            user.ZipCode =
                NormalizeOptionalValue(
                    request.ZipCode);

            user.IsProfileCompleted =
                true;

            user.UpdatedAt =
                currentTime;

            if (!string.Equals(
                    user.PhoneNumber,
                    phoneNumber,
                    StringComparison.Ordinal))
            {
                user.PhoneNumber =
                    phoneNumber;

                /*
                 * لا يوجد SMS verification حاليًا.
                 * عند تغيير الرقم لا يجب أن يظل Confirmed.
                 */
                user.PhoneNumberConfirmed =
                    false;
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return await MapToResponseAsync(
                user);
        }

        public async Task<UserProfileResponse>
            UploadImageAsync(
                Guid userId,
                Stream fileStream,
                string fileName,
                string contentType,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                fileStream);

            var user =
                await GetActiveUserAsync(
                    userId,
                    cancellationToken);

            var oldImagePublicId =
                user.ProfileImagePublicId;

            var baseFolder =
                _cloudinarySettings.BaseFolder
                    .Trim()
                    .Trim('/');

            var imageFolder =
                $"{baseFolder}/users/" +
                $"{user.Id}/profile";

            /*
             * نرفع الصورة الجديدة أولًا.
             * لن نحذف القديمة قبل التأكد أن الجديدة نجحت.
             */
            var uploadResult =
                await _imageStorageService
                    .UploadAsync(
                        fileStream,
                        fileName,
                        contentType,
                        imageFolder,
                        ImageAccessType.Public,
                        cancellationToken);

            user.ProfileImageUrl =
                uploadResult.SecureUrl;

            user.ProfileImagePublicId =
                uploadResult.PublicId;

            user.UpdatedAt =
                DateTimeOffset.UtcNow;

            try
            {
                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            catch
            {
                /*
                 * رفع Cloudinary نجح لكن حفظ DB فشل.
                 * نحذف الصورة الجديدة حتى لا تظل Orphan.
                 */
                await TryDeleteImageAsync(
                    uploadResult.PublicId,
                    "new user profile image cleanup");

                throw;
            }

            /*
             * بعد حفظ الصورة الجديدة في DB يمكن حذف القديمة.
             */
            if (!string.IsNullOrWhiteSpace(
                    oldImagePublicId)
                &&
                !string.Equals(
                    oldImagePublicId,
                    uploadResult.PublicId,
                    StringComparison.Ordinal))
            {
                await TryDeleteImageAsync(
                    oldImagePublicId,
                    "old user profile image replacement");
            }

            return await MapToResponseAsync(
                user);
        }

        public async Task<UserProfileResponse>
            DeleteImageAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var user =
                await GetActiveUserAsync(
                    userId,
                    cancellationToken);

            var oldImagePublicId =
                user.ProfileImagePublicId;

            user.ProfileImageUrl =
                null;

            user.ProfileImagePublicId =
                null;

            user.UpdatedAt =
                DateTimeOffset.UtcNow;

            /*
             * نمسح Reference من DB أولًا.
             */
            await _dbContext.SaveChangesAsync(
                cancellationToken);

            /*
             * بعد نجاح DB نحذف الملف من Cloudinary.
             */
            if (!string.IsNullOrWhiteSpace(
                    oldImagePublicId))
            {
                await TryDeleteImageAsync(
                    oldImagePublicId,
                    "user profile image deletion");
            }

            return await MapToResponseAsync(
                user);
        }

        private async Task<ApplicationUser>
            GetActiveUserAsync(
                Guid userId,
                CancellationToken cancellationToken)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The user identifier is required.");
            }

            var user =
                await _dbContext.Users
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

            return user;
        }

        private async Task<UserProfileResponse>
            MapToResponseAsync(
                ApplicationUser user)
        {
            var roles =
                await _userManager
                    .GetRolesAsync(user);

            return new UserProfileResponse
            {
                Id =
                    user.Id,

                FirstName =
                    user.FirstName
                    ?? string.Empty,

                LastName =
                    user.LastName
                    ?? string.Empty,

                Email =
                    user.Email
                    ?? string.Empty,

                PhoneNumber =
                    user.PhoneNumber,

                ProfileImageUrl =
                    user.ProfileImageUrl,

                Gender =
                    user.Gender?.ToString(),

                Birthday =
                    user.Birthday,

                Country =
                    user.Country,

                Address =
                    user.Address,

                ZipCode =
                    user.ZipCode,

                IsProfileCompleted =
                    user.IsProfileCompleted,

                Roles =
                    roles.ToList(),

                CreatedAt =
                    user.CreatedAt,

                UpdatedAt =
                    user.UpdatedAt
            };
        }

        private async Task TryDeleteImageAsync(
            string publicId,
            string operationDescription)
        {
            try
            {
                await _imageStorageService
                    .DeleteAsync(
                        publicId,
                        ImageAccessType.Public,
                        CancellationToken.None);
            }
            catch (Exception exception)
            {
                /*
                 * فشل حذف الصورة القديمة لا يجب أن يفشل
                 * تحديث Profile الذي تم حفظه بالفعل.
                 */
                _logger.LogWarning(
                    exception,
                    "Unable to delete Cloudinary image " +
                    "{PublicId} during " +
                    "{OperationDescription}.",
                    publicId,
                    operationDescription);
            }
        }

        private static string?
            NormalizeOptionalValue(
                string? value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return null;
            }

            return value.Trim();
        }
    }
}