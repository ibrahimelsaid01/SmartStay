using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class HostApplicationService
        : IHostApplicationService
    {
        private readonly SmartStayDbContext _dbContext;

        private readonly IImageStorageService
            _imageStorageService;

        private readonly CloudinarySettings
            _cloudinarySettings;

        private readonly ILogger<HostApplicationService>
            _logger;
        public HostApplicationService(
         SmartStayDbContext dbContext,
         IImageStorageService imageStorageService,
         IOptions<CloudinarySettings> cloudinaryOptions,
         ILogger<HostApplicationService> logger)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            ArgumentNullException.ThrowIfNull(imageStorageService);
            ArgumentNullException.ThrowIfNull(cloudinaryOptions);
            ArgumentNullException.ThrowIfNull(logger);

            _dbContext = dbContext;

            _imageStorageService =
                imageStorageService;

            _cloudinarySettings =
                cloudinaryOptions.Value;

            _logger = logger;
        }

        public async Task<HostApplicationResponse>
            CreateDraftAsync(
                Guid userId,
                CreateHostApplicationRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            // 1. Get the authenticated user.
            var user = await _dbContext.Users
                .SingleOrDefaultAsync(
                    currentUser =>
                        currentUser.Id == userId,
                    cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException(
                    "The user was not found.");
            }

            // 2. Block inactive users.
            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            // 3. Require completion of the normal user profile first.
            if (!user.IsProfileCompleted)
            {
                throw new InvalidOperationException(
                    "Complete your user profile before applying to become a host.");
            }

            // 4. Prevent multiple host applications.
            var applicationAlreadyExists =
                await _dbContext.HostProfiles
                    .AnyAsync(
                        hostProfile =>
                            hostProfile.UserId == userId,
                        cancellationToken);

            if (applicationAlreadyExists)
            {
                throw new HostApplicationAlreadyExistsException();
            }

            // 5. Normalize and validate submitted values.
            var displayName =
                request.DisplayName.Trim();

            var bio =
                request.Bio.Trim();

            var country =
                request.Country.Trim();

            var city =
                request.City.Trim();

            var phoneNumber =
                request.PhoneNumber.Trim();

            ValidateNormalizedValues(
                displayName,
                bio,
                country,
                city);

            var currentTime =
                DateTimeOffset.UtcNow;

            // PhoneNumber remains on ApplicationUser.
            user.PhoneNumber = phoneNumber;

            // There is no SMS verification currently.
            user.PhoneNumberConfirmed = false;

            user.UpdatedAt = currentTime;

            // 6. Create the application as Draft.
            var hostProfile =
                new HostProfile
                {
                    Id = Guid.NewGuid(),

                    UserId = user.Id,

                    DisplayName = displayName,

                    Bio = bio,

                    Country = country,

                    City = city,

                    Status =
                        HostApplicationStatus.Draft,

                    CreatedAt = currentTime,

                    User = user
                };

            await _dbContext.HostProfiles.AddAsync(
                hostProfile,
                cancellationToken);

            /*
             * Updating ApplicationUser and inserting HostProfile
             * are committed by the same SaveChanges call.
             * EF Core therefore executes them atomically.
             */
            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToResponse(hostProfile);
        }



        public async Task<HostApplicationResponse>
    GetCurrentAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        {
            var hostApplication =
                await _dbContext.HostProfiles
                    .AsNoTracking()
                    .Include(hostProfile =>
                        hostProfile.User)
                    .Include(hostProfile =>
                        hostProfile.IdentityDocument)
                    .SingleOrDefaultAsync(
                        hostProfile =>
                            hostProfile.UserId == userId,
                        cancellationToken);

            if (hostApplication is null)
            {
                throw new KeyNotFoundException(
                    "The host application was not found.");
            }

            return MapToResponse(hostApplication);
        }



        public async Task<HostApplicationResponse>
    UpdateCurrentAsync(
        Guid userId,
        UpdateHostApplicationRequest request,
        CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            // 1. Load the current host application.
            var hostApplication =
                await _dbContext.HostProfiles
                    .Include(hostProfile =>
                        hostProfile.User)
                    .Include(hostProfile =>
                        hostProfile.IdentityDocument)
                    .SingleOrDefaultAsync(
                        hostProfile =>
                            hostProfile.UserId == userId,
                        cancellationToken);

            if (hostApplication is null)
            {
                throw new KeyNotFoundException(
                    "The host application was not found.");
            }

            // 2. Block inactive accounts.
            if (!hostApplication.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            // 3. Only Draft and Rejected applications can be edited.
            if (hostApplication.Status is not
                    HostApplicationStatus.Draft
                and not
                    HostApplicationStatus.Rejected)
            {
                throw new HostApplicationNotEditableException(
                    hostApplication.Status.ToString());
            }

            // 4. Normalize submitted values.
            var displayName =
                request.DisplayName.Trim();

            var bio =
                request.Bio.Trim();

            var country =
                request.Country.Trim();

            var city =
                request.City.Trim();

            var phoneNumber =
                request.PhoneNumber.Trim();

            // 5. Validate again after trimming.
            ValidateNormalizedValues(
                displayName,
                bio,
                country,
                city);

            var currentTime =
                DateTimeOffset.UtcNow;

            // 6. Update host application fields.
            hostApplication.DisplayName =
                displayName;

            hostApplication.Bio =
                bio;

            hostApplication.Country =
                country;

            hostApplication.City =
                city;

            hostApplication.UpdatedAt =
                currentTime;

            // 7. Update the user's phone number only when changed.
            if (!string.Equals(
                    hostApplication.User.PhoneNumber,
                    phoneNumber,
                    StringComparison.Ordinal))
            {
                hostApplication.User.PhoneNumber =
                    phoneNumber;

                /*
                 * There is no SMS verification currently.
                 * If the phone number changes, it must not remain confirmed.
                 */
                hostApplication.User.PhoneNumberConfirmed =
                    false;
            }

            hostApplication.User.UpdatedAt =
                currentTime;

            // 8. Save all changes.
            await _dbContext.SaveChangesAsync(
                cancellationToken);

            // 9. Return the updated application.
            return MapToResponse(hostApplication);
        }


        public async Task<HostApplicationResponse>
    UploadProfileImageAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fileStream);

            // 1. Load the current host application.
            var hostApplication =
                await _dbContext.HostProfiles
                    .Include(hostProfile =>
                        hostProfile.User)
                    .Include(hostProfile =>
                        hostProfile.IdentityDocument)
                    .SingleOrDefaultAsync(
                        hostProfile =>
                            hostProfile.UserId == userId,
                        cancellationToken);

            if (hostApplication is null)
            {
                throw new KeyNotFoundException(
                    "The host application was not found.");
            }

            // 2. Block inactive accounts.
            if (!hostApplication.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            // 3. Images can only be changed while the application
            // is Draft or Rejected.
            var isEditable =
                hostApplication.Status ==
                    HostApplicationStatus.Draft
                ||
                hostApplication.Status ==
                    HostApplicationStatus.Rejected;

            if (!isEditable)
            {
                throw new HostApplicationNotEditableException(
                    hostApplication.Status.ToString());
            }

            // 4. Keep the old image ID in case this is a replacement.
            var oldImagePublicId =
                hostApplication.ProfileImagePublicId;

            // 5. Build the Cloudinary folder.
            var baseFolder =
                _cloudinarySettings.BaseFolder
                    .Trim()
                    .Trim('/');

            var imageFolder =
                $"{baseFolder}/hosts/{hostApplication.Id}/profile";

            // 6. Upload the new image first.
            var uploadResult =
                await _imageStorageService.UploadAsync(
                    fileStream,
                    fileName,
                    contentType,
                    imageFolder,
                    ImageAccessType.Public,
                    cancellationToken);

            // 7. Update the database with the new image.
            hostApplication.ProfileImageUrl =
                uploadResult.SecureUrl;

            hostApplication.ProfileImagePublicId =
                uploadResult.PublicId;

            hostApplication.UpdatedAt =
                DateTimeOffset.UtcNow;

            try
            {
                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            catch
            {
                /*
                 * Database saving failed after uploading the new image.
                 * Delete the new image so it does not remain orphaned.
                 */
                await TryDeleteImageAsync(
                    uploadResult.PublicId,
                    ImageAccessType.Public,
                    "new host profile image cleanup");

                throw;
            }

            /*
             * The database now points to the new image.
             * We can safely delete the previous image.
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
                    ImageAccessType.Public,
                    "old host profile image replacement");
            }

            return MapToResponse(hostApplication);
        }




        private async Task TryDeleteImageAsync(
                            string publicId,
                            ImageAccessType accessType,
                            string operationDescription)
        {
            try
            {
                await _imageStorageService.DeleteAsync(
                    publicId,
                    accessType,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Unable to delete Cloudinary image {PublicId} during {OperationDescription}.",
                    publicId,
                    operationDescription);
            }
        }


        public async Task<HostApplicationResponse>
    UploadNationalIdAsync(
        Guid userId,
        Stream frontFileStream,
        string frontFileName,
        string frontContentType,
        Stream backFileStream,
        string backFileName,
        string backContentType,
        CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                frontFileStream);

            ArgumentNullException.ThrowIfNull(
                backFileStream);

            // 1. Load the current host application.
            var hostApplication =
                await _dbContext.HostProfiles
                    .Include(hostProfile =>
                        hostProfile.User)
                    .Include(hostProfile =>
                        hostProfile.IdentityDocument)
                    .SingleOrDefaultAsync(
                        hostProfile =>
                            hostProfile.UserId == userId,
                        cancellationToken);

            if (hostApplication is null)
            {
                throw new KeyNotFoundException(
                    "The host application was not found.");
            }

            // 2. Block inactive accounts.
            if (!hostApplication.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            // 3. National ID images can only be changed
            // while the application is Draft or Rejected.
            var isEditable =
                hostApplication.Status ==
                    HostApplicationStatus.Draft
                ||
                hostApplication.Status ==
                    HostApplicationStatus.Rejected;

            if (!isEditable)
            {
                throw new HostApplicationNotEditableException(
                    hostApplication.Status.ToString());
            }

            // 4. Preserve old image IDs when replacing
            // an existing identity document.
            var oldFrontPublicId =
                hostApplication.IdentityDocument?
                    .FrontPublicId;

            var oldBackPublicId =
                hostApplication.IdentityDocument?
                    .BackPublicId;

            // 5. Build private Cloudinary folders.
            var baseFolder =
                _cloudinarySettings.BaseFolder
                    .Trim()
                    .Trim('/');

            var frontFolder =
                $"{baseFolder}/host-verifications/" +
                $"{hostApplication.Id}/national-id/front";

            var backFolder =
                $"{baseFolder}/host-verifications/" +
                $"{hostApplication.Id}/national-id/back";

            // 6. Upload the front image first.
            var frontUpload =
                await _imageStorageService.UploadAsync(
                    frontFileStream,
                    frontFileName,
                    frontContentType,
                    frontFolder,
                    ImageAccessType.Authenticated,
                    cancellationToken);

            ImageUploadResult backUpload;

            try
            {
                // 7. Upload the back image.
                backUpload =
                    await _imageStorageService.UploadAsync(
                        backFileStream,
                        backFileName,
                        backContentType,
                        backFolder,
                        ImageAccessType.Authenticated,
                        cancellationToken);
            }
            catch
            {
                /*
                 * The front image was uploaded but the back
                 * image failed. Remove the uploaded front image
                 * so the operation remains all-or-nothing.
                 */
                await TryDeleteImageAsync(
                    frontUpload.PublicId,
                    ImageAccessType.Authenticated,
                    "failed national ID back image upload cleanup");

                throw;
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            // 8. Create or update the database record.
            if (hostApplication.IdentityDocument is null)
            {
                var identityDocument =
                    new HostIdentityDocument
                    {
                        Id = Guid.NewGuid(),

                        HostProfileId =
                            hostApplication.Id,

                        FrontPublicId =
                            frontUpload.PublicId,

                        FrontFormat =
                            frontUpload.Format,

                        BackPublicId =
                            backUpload.PublicId,

                        BackFormat =
                            backUpload.Format,

                        CreatedAt =
                            currentTime,

                        HostProfile =
                            hostApplication
                    };

                hostApplication.IdentityDocument =
                    identityDocument;

                await _dbContext
                    .HostIdentityDocuments
                    .AddAsync(
                        identityDocument,
                        cancellationToken);
            }
            else
            {
                hostApplication
                    .IdentityDocument
                    .FrontPublicId =
                        frontUpload.PublicId;

                hostApplication
                    .IdentityDocument
                    .FrontFormat =
                        frontUpload.Format;

                hostApplication
                    .IdentityDocument
                    .BackPublicId =
                        backUpload.PublicId;

                hostApplication
                    .IdentityDocument
                    .BackFormat =
                        backUpload.Format;

                hostApplication
                    .IdentityDocument
                    .UpdatedAt =
                        currentTime;
            }

            hostApplication.UpdatedAt =
                currentTime;

            try
            {
                // 9. Save both image references together.
                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            catch
            {
                /*
                 * Cloudinary succeeded but the database failed.
                 * Remove both newly uploaded images.
                 */
                await TryDeleteImageAsync(
                    frontUpload.PublicId,
                    ImageAccessType.Authenticated,
                    "failed national ID database save cleanup");

                await TryDeleteImageAsync(
                    backUpload.PublicId,
                    ImageAccessType.Authenticated,
                    "failed national ID database save cleanup");

                throw;
            }

            /*
             * Database now points to the new images.
             * Old images can be safely removed.
             */
            if (!string.IsNullOrWhiteSpace(
                    oldFrontPublicId)
                &&
                !string.Equals(
                    oldFrontPublicId,
                    frontUpload.PublicId,
                    StringComparison.Ordinal))
            {
                await TryDeleteImageAsync(
                    oldFrontPublicId,
                    ImageAccessType.Authenticated,
                    "old national ID front image replacement");
            }

            if (!string.IsNullOrWhiteSpace(
                    oldBackPublicId)
                &&
                !string.Equals(
                    oldBackPublicId,
                    backUpload.PublicId,
                    StringComparison.Ordinal))
            {
                await TryDeleteImageAsync(
                    oldBackPublicId,
                    ImageAccessType.Authenticated,
                    "old national ID back image replacement");
            }

            return MapToResponse(
                hostApplication);
        }



        public async Task<HostApplicationResponse>
    SubmitCurrentAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        {
            // 1. Load the current host application.
            var hostApplication =
                await _dbContext.HostProfiles
                    .Include(hostProfile =>
                        hostProfile.User)
                    .Include(hostProfile =>
                        hostProfile.IdentityDocument)
                    .SingleOrDefaultAsync(
                        hostProfile =>
                            hostProfile.UserId == userId,
                        cancellationToken);

            if (hostApplication is null)
            {
                throw new KeyNotFoundException(
                    "The host application was not found.");
            }

            // 2. Block inactive accounts.
            if (!hostApplication.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            // 3. The normal user profile must remain completed.
            if (!hostApplication.User.IsProfileCompleted)
            {
                throw new InvalidOperationException(
                    "Complete your user profile before submitting the host application.");
            }

            // 4. Only Draft and Rejected applications
            // can be submitted.
            var canBeSubmitted =
                hostApplication.Status ==
                    HostApplicationStatus.Draft
                ||
                hostApplication.Status ==
                    HostApplicationStatus.Rejected;

            if (!canBeSubmitted)
            {
                throw new HostApplicationCannotBeSubmittedException(
                    hostApplication.Status.ToString());
            }

            // 5. Collect all missing application requirements.
            var missingRequirements =
                new List<string>();

            if (string.IsNullOrWhiteSpace(
                    hostApplication.DisplayName))
            {
                missingRequirements.Add(
                    "display name");
            }

            if (string.IsNullOrWhiteSpace(
                    hostApplication.Bio))
            {
                missingRequirements.Add(
                    "bio");
            }

            if (string.IsNullOrWhiteSpace(
                    hostApplication.Country))
            {
                missingRequirements.Add(
                    "country");
            }

            if (string.IsNullOrWhiteSpace(
                    hostApplication.City))
            {
                missingRequirements.Add(
                    "city");
            }

            if (string.IsNullOrWhiteSpace(
                    hostApplication.User.PhoneNumber))
            {
                missingRequirements.Add(
                    "phone number");
            }

            var hasProfileImage =
                !string.IsNullOrWhiteSpace(
                    hostApplication.ProfileImageUrl)
                &&
                !string.IsNullOrWhiteSpace(
                    hostApplication.ProfileImagePublicId);

            if (!hasProfileImage)
            {
                missingRequirements.Add(
                    "host profile image");
            }

            var identityDocument =
                hostApplication.IdentityDocument;

            var hasCompleteIdentityDocument =
                identityDocument is not null
                &&
                !string.IsNullOrWhiteSpace(
                    identityDocument.FrontPublicId)
                &&
                !string.IsNullOrWhiteSpace(
                    identityDocument.FrontFormat)
                &&
                !string.IsNullOrWhiteSpace(
                    identityDocument.BackPublicId)
                &&
                !string.IsNullOrWhiteSpace(
                    identityDocument.BackFormat);

            if (!hasCompleteIdentityDocument)
            {
                missingRequirements.Add(
                    "national ID front and back images");
            }

            // 6. Stop submission when requirements are missing.
            if (missingRequirements.Count > 0)
            {
                throw new HostApplicationIncompleteException(
                    missingRequirements);
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            // 7. Move the application to Pending.
            hostApplication.Status =
                HostApplicationStatus.Pending;

            hostApplication.SubmittedAt =
                currentTime;

            hostApplication.UpdatedAt =
                currentTime;

            /*
             * When resubmitting a rejected application,
             * remove the previous rejection decision.
             */
            hostApplication.RejectionReason =
                null;

            hostApplication.ReviewedAt =
                null;

            // 8. Save the new status.
            await _dbContext.SaveChangesAsync(
                cancellationToken);

            // 9. Return the updated application.
            return MapToResponse(
                hostApplication);
        }





        private static void ValidateNormalizedValues(
            string displayName,
            string bio,
            string country,
            string city)
        {
            /*
             * DTO validation happens before trimming.
             * We validate again after Trim so values such as
             * " a " cannot bypass MinimumLength.
             */

            if (displayName.Length is < 3 or > 80)
            {
                throw new ArgumentException(
                    "Display name must contain between 3 and 80 characters.");
            }

            if (bio.Length is < 20 or > 1000)
            {
                throw new ArgumentException(
                    "Bio must contain between 20 and 1000 characters.");
            }

            if (string.IsNullOrWhiteSpace(country) ||
                country.Length > 100)
            {
                throw new ArgumentException(
                    "Country is required and must not exceed 100 characters.");
            }

            if (string.IsNullOrWhiteSpace(city) ||
                city.Length > 100)
            {
                throw new ArgumentException(
                    "City is required and must not exceed 100 characters.");
            }
        }

        private static HostApplicationResponse MapToResponse(
            HostProfile hostProfile)
        {
            return new HostApplicationResponse
            {
                Id =
                    hostProfile.Id,

                DisplayName =
                    hostProfile.DisplayName,

                Bio =
                    hostProfile.Bio,

                Country =
                    hostProfile.Country,

                City =
                    hostProfile.City,

                PhoneNumber =
                    hostProfile.User.PhoneNumber
                    ?? string.Empty,

                ProfileImageUrl =
                    hostProfile.ProfileImageUrl,

                Status =
                    hostProfile.Status.ToString(),

                RejectionReason =
                    hostProfile.RejectionReason,

                HasProfileImage =
                    !string.IsNullOrWhiteSpace(
                        hostProfile.ProfileImagePublicId),

                HasIdentityDocument =
                    hostProfile.IdentityDocument is not null,

                CreatedAt =
                    hostProfile.CreatedAt,

                UpdatedAt =
                    hostProfile.UpdatedAt,

                SubmittedAt =
                    hostProfile.SubmittedAt,

                ReviewedAt =
                    hostProfile.ReviewedAt
            };
        }
    }
}