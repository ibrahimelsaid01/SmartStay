using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class AdminHostApplicationService
        : IAdminHostApplicationService
    {
        private readonly SmartStayDbContext
            _dbContext;
        private readonly IImageStorageService
           _imageStorageService;
        private readonly UserManager<ApplicationUser>
         _userManager;

        public AdminHostApplicationService(
            SmartStayDbContext dbContext,
            IImageStorageService imageStorageService,
            UserManager<ApplicationUser> userManager)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            ArgumentNullException.ThrowIfNull(
                imageStorageService);

            ArgumentNullException.ThrowIfNull(
                userManager);

            _dbContext =
                dbContext;

            _imageStorageService =
                imageStorageService;

            _userManager =
                userManager;
        }

        public async Task<IReadOnlyList<
            AdminHostApplicationSummaryResponse>>
            GetPendingAsync(
                CancellationToken cancellationToken = default)
        {
            var applications =
                await _dbContext.HostProfiles
                    .AsNoTracking()
                    .Where(hostProfile =>
                        hostProfile.Status ==
                        HostApplicationStatus.Pending)
                    .OrderByDescending(hostProfile =>
                        hostProfile.SubmittedAt)
                    .Select(hostProfile =>
                        new AdminHostApplicationSummaryResponse
                        {
                            Id =
                                hostProfile.Id,

                            DisplayName =
                                hostProfile.DisplayName,

                            UserFullName =
                                (
                                    (hostProfile.User.FirstName
                                        ?? string.Empty)
                                    +
                                    " "
                                    +
                                    (hostProfile.User.LastName
                                        ?? string.Empty)
                                ).Trim(),

                            Email =
                                hostProfile.User.Email
                                ?? string.Empty,

                            PhoneNumber =
                                hostProfile.User.PhoneNumber
                                ?? string.Empty,

                            Country =
                                hostProfile.Country,

                            City =
                                hostProfile.City,

                            ProfileImageUrl =
                                hostProfile.ProfileImageUrl,

                            Status =
                                hostProfile.Status.ToString(),

                            HasIdentityDocument =
                                hostProfile.IdentityDocument
                                != null,

                            CreatedAt =
                                hostProfile.CreatedAt,

                            SubmittedAt =
                                hostProfile.SubmittedAt
                        })
                    .ToListAsync(
                        cancellationToken);

            return applications;
        }




        public async Task<AdminHostApplicationDetailsResponse> GetByIdAsync( Guid applicationId,
        CancellationToken cancellationToken = default)
        {
            var application =
                await _dbContext.HostProfiles
                    .AsNoTracking()
                    .Where(hostProfile =>
                        hostProfile.Id == applicationId)
                    .Select(hostProfile =>
                        new AdminHostApplicationDetailsResponse
                        {
                            Id =
                                hostProfile.Id,

                            DisplayName =
                                hostProfile.DisplayName,

                            UserFullName =
                                (
                                    (hostProfile.User.FirstName
                                        ?? string.Empty)
                                    +
                                    " "
                                    +
                                    (hostProfile.User.LastName
                                        ?? string.Empty)
                                ).Trim(),

                            Email =
                                hostProfile.User.Email
                                ?? string.Empty,

                            PhoneNumber =
                                hostProfile.User.PhoneNumber
                                ?? string.Empty,

                            Bio =
                                hostProfile.Bio,

                            Country =
                                hostProfile.Country,

                            City =
                                hostProfile.City,

                            ProfileImageUrl =
                                hostProfile.ProfileImageUrl,

                            Status =
                                hostProfile.Status.ToString(),

                            RejectionReason =
                                hostProfile.RejectionReason,

                            HasProfileImage =
                                !string.IsNullOrWhiteSpace(
                                    hostProfile.ProfileImageUrl)
                                &&
                                !string.IsNullOrWhiteSpace(
                                    hostProfile.ProfileImagePublicId),

                            HasIdentityDocument =
                                hostProfile.IdentityDocument
                                != null,

                            CreatedAt =
                                hostProfile.CreatedAt,

                            UpdatedAt =
                                hostProfile.UpdatedAt,

                            SubmittedAt =
                                hostProfile.SubmittedAt,

                            ReviewedAt =
                                hostProfile.ReviewedAt
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (application is null)
            {
                throw new KeyNotFoundException(
                    "The host application was not found.");
            }

            return application;
        }




        public async Task<ImageContentResult> GetIdentityDocumentImageAsync(
        Guid applicationId,
        HostIdentityDocumentSide side,
        CancellationToken cancellationToken = default)
        {
            var application =
                await _dbContext.HostProfiles
                    .AsNoTracking()
                    .Where(hostProfile =>
                        hostProfile.Id == applicationId)
                    .Select(hostProfile =>
                        new
                        {
                            hostProfile.Id,

                            IdentityDocumentId =
                                hostProfile.IdentityDocument == null
                                    ? (Guid?)null
                                    : hostProfile
                                        .IdentityDocument
                                        .Id,

                            FrontPublicId =
                                hostProfile.IdentityDocument == null
                                    ? null
                                    : hostProfile
                                        .IdentityDocument
                                        .FrontPublicId,

                            FrontFormat =
                                hostProfile.IdentityDocument == null
                                    ? null
                                    : hostProfile
                                        .IdentityDocument
                                        .FrontFormat,

                            BackPublicId =
                                hostProfile.IdentityDocument == null
                                    ? null
                                    : hostProfile
                                        .IdentityDocument
                                        .BackPublicId,

                            BackFormat =
                                hostProfile.IdentityDocument == null
                                    ? null
                                    : hostProfile
                                        .IdentityDocument
                                        .BackFormat
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (application is null)
            {
                throw new KeyNotFoundException(
                    "The host application was not found.");
            }

            if (!application.IdentityDocumentId.HasValue)
            {
                throw new KeyNotFoundException(
                    "The host identity document was not found.");
            }

            var imageData =
                side switch
                {
                    HostIdentityDocumentSide.Front =>
                        new
                        {
                            PublicId =
                                application.FrontPublicId,

                            Format =
                                application.FrontFormat
                        },

                    HostIdentityDocumentSide.Back =>
                        new
                        {
                            PublicId =
                                application.BackPublicId,

                            Format =
                                application.BackFormat
                        },

                    _ =>
                        throw new ArgumentOutOfRangeException(
                            nameof(side),
                            side,
                            "The identity document side is not supported.")
                };

            if (string.IsNullOrWhiteSpace(
                    imageData.PublicId)
                ||
                string.IsNullOrWhiteSpace(
                    imageData.Format))
            {
                throw new KeyNotFoundException(
                    "The requested identity document image was not found.");
            }

            return await _imageStorageService
                .DownloadAsync(
                    imageData.PublicId,
                    imageData.Format,
                    ImageAccessType.Authenticated,
                    cancellationToken);
        }



        public async Task<AdminHostApplicationDetailsResponse> ApproveAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                // 1. Load the application with its user
                // and identity document.
                var application =
                    await _dbContext.HostProfiles
                        .Include(hostProfile =>
                            hostProfile.User)
                        .Include(hostProfile =>
                            hostProfile.IdentityDocument)
                        .SingleOrDefaultAsync(
                            hostProfile =>
                                hostProfile.Id ==
                                applicationId,
                            cancellationToken);

                if (application is null)
                {
                    throw new KeyNotFoundException(
                        "The host application was not found.");
                }

                // 2. Only Pending applications can be approved.
                if (application.Status !=
                    HostApplicationStatus.Pending)
                {
                    throw new HostApplicationNotPendingException(
                        application.Status.ToString());
                }

                // 3. The user account must still be active.
                if (!application.User.IsActive)
                {
                    throw new InvalidOperationException(
                        "The user account is inactive and cannot be approved as a host.");
                }

                // 4. Verify that the application still contains
                // all required verification information.
                EnsureApplicationIsReadyForApproval(
                    application);

                // 5. Add the Host role only when it is not
                // already assigned.
                var alreadyHasHostRole =
                    await _userManager.IsInRoleAsync(
                        application.User,
                        RoleNames.Host);

                if (!alreadyHasHostRole)
                {
                    var addRoleResult =
                        await _userManager.AddToRoleAsync(
                            application.User,
                            RoleNames.Host);

                    EnsureIdentitySucceeded(
                        addRoleResult,
                        "Unable to assign the Host role.");
                }

                var currentTime =
                    DateTimeOffset.UtcNow;

                // 6. Complete the approval.
                application.Status =
                    HostApplicationStatus.Approved;

                application.RejectionReason =
                    null;

                application.ReviewedAt =
                    currentTime;

                application.UpdatedAt =
                    currentTime;

                application.User.UpdatedAt =
                    currentTime;

                // 7. Save the application state.
                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                // 8. Commit the role and application changes together.
                await transaction.CommitAsync(
                    cancellationToken);

                return MapToDetailsResponse(
                    application);
            }
            catch
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                throw;
            }
        }





        public async Task<AdminHostApplicationDetailsResponse>
    RejectAsync(
        Guid applicationId,
        RejectHostApplicationRequest request,
        CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            // 1. Normalize the rejection reason.
            var rejectionReason =
                request.Reason.Trim();

            /*
             * Data annotations validate the original value.
             * We validate again after Trim so spaces cannot
             * bypass the minimum-length requirement.
             */
            if (rejectionReason.Length is < 10 or > 500)
            {
                throw new ArgumentException(
                    "Rejection reason must contain between 10 and 500 characters.");
            }

            // 2. Load the application and related data.
            var application =
                await _dbContext.HostProfiles
                    .Include(hostProfile =>
                        hostProfile.User)
                    .Include(hostProfile =>
                        hostProfile.IdentityDocument)
                    .SingleOrDefaultAsync(
                        hostProfile =>
                            hostProfile.Id ==
                            applicationId,
                        cancellationToken);

            if (application is null)
            {
                throw new KeyNotFoundException(
                    "The host application was not found.");
            }

            // 3. Only Pending applications can be rejected.
            if (application.Status !=
                HostApplicationStatus.Pending)
            {
                throw new HostApplicationNotPendingException(
                    application.Status.ToString());
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            // 4. Apply the rejection decision.
            application.Status =
                HostApplicationStatus.Rejected;

            application.RejectionReason =
                rejectionReason;

            application.ReviewedAt =
                currentTime;

            application.UpdatedAt =
                currentTime;

            // 5. Save the decision.
            await _dbContext.SaveChangesAsync(
                cancellationToken);

            // 6. Return the updated application.
            return MapToDetailsResponse(
                application);
        }













        private static void EnsureApplicationIsReadyForApproval(
    HostProfile application)
        {
            var missingRequirements =
                new List<string>();

            if (string.IsNullOrWhiteSpace(
                    application.DisplayName))
            {
                missingRequirements.Add(
                    "display name");
            }

            if (string.IsNullOrWhiteSpace(
                    application.Bio))
            {
                missingRequirements.Add(
                    "bio");
            }

            if (string.IsNullOrWhiteSpace(
                    application.Country))
            {
                missingRequirements.Add(
                    "country");
            }

            if (string.IsNullOrWhiteSpace(
                    application.City))
            {
                missingRequirements.Add(
                    "city");
            }

            if (string.IsNullOrWhiteSpace(
                    application.User.PhoneNumber))
            {
                missingRequirements.Add(
                    "phone number");
            }

            var hasProfileImage =
                !string.IsNullOrWhiteSpace(
                    application.ProfileImageUrl)
                &&
                !string.IsNullOrWhiteSpace(
                    application.ProfileImagePublicId);

            if (!hasProfileImage)
            {
                missingRequirements.Add(
                    "host profile image");
            }

            var document =
                application.IdentityDocument;

            var hasCompleteIdentityDocument =
                document is not null
                &&
                !string.IsNullOrWhiteSpace(
                    document.FrontPublicId)
                &&
                !string.IsNullOrWhiteSpace(
                    document.FrontFormat)
                &&
                !string.IsNullOrWhiteSpace(
                    document.BackPublicId)
                &&
                !string.IsNullOrWhiteSpace(
                    document.BackFormat);

            if (!hasCompleteIdentityDocument)
            {
                missingRequirements.Add(
                    "national ID front and back images");
            }

            if (missingRequirements.Count > 0)
            {
                throw new InvalidOperationException(
                    "The pending host application is missing required data: " +
                    string.Join(
                        ", ",
                        missingRequirements) +
                    ".");
            }
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
                string.IsNullOrWhiteSpace(errors)
                    ? defaultMessage
                    : errors);
        }



        private static AdminHostApplicationDetailsResponse
    MapToDetailsResponse(
        HostProfile application)
        {
            return new AdminHostApplicationDetailsResponse
            {
                Id =
                    application.Id,

                DisplayName =
                    application.DisplayName,

                UserFullName =
                    (
                        (application.User.FirstName
                            ?? string.Empty)
                        +
                        " "
                        +
                        (application.User.LastName
                            ?? string.Empty)
                    ).Trim(),

                Email =
                    application.User.Email
                    ?? string.Empty,

                PhoneNumber =
                    application.User.PhoneNumber
                    ?? string.Empty,

                Bio =
                    application.Bio,

                Country =
                    application.Country,

                City =
                    application.City,

                ProfileImageUrl =
                    application.ProfileImageUrl,

                Status =
                    application.Status.ToString(),

                RejectionReason =
                    application.RejectionReason,

                HasProfileImage =
                    !string.IsNullOrWhiteSpace(
                        application.ProfileImageUrl)
                    &&
                    !string.IsNullOrWhiteSpace(
                        application.ProfileImagePublicId),

                HasIdentityDocument =
                    application.IdentityDocument
                    is not null,

                CreatedAt =
                    application.CreatedAt,

                UpdatedAt =
                    application.UpdatedAt,

                SubmittedAt =
                    application.SubmittedAt,

                ReviewedAt =
                    application.ReviewedAt
            };
        }

    }
}