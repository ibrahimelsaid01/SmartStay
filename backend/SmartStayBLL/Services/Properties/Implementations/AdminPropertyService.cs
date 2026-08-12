using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class AdminPropertyService
        : IAdminPropertyService
    {
        private const int MinimumPropertyImages =
            3;

        private const int MaximumPropertyImages =
            10;

        private const int
            MaximumVerificationDocumentPages = 5;

        private const int MinimumRejectionReasonLength =
            10;

        private const int MaximumRejectionReasonLength =
            500;

        private const int MaximumPageSize =
            100;

        private readonly SmartStayDbContext
            _dbContext;

        private readonly IImageStorageService
            _imageStorageService;

        public AdminPropertyService(
            SmartStayDbContext dbContext,
            IImageStorageService imageStorageService)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            ArgumentNullException.ThrowIfNull(
                imageStorageService);

            _dbContext =
                dbContext;

            _imageStorageService =
                imageStorageService;
        }

        public async Task<AdminPendingPropertiesResponse>
            GetPendingAsync(
                int page,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            ValidatePagination(
                page,
                pageSize);

            var query =
                _dbContext.Properties
                    .AsNoTracking()
                    .Where(property =>
                        property.Status ==
                            PropertyStatus.Pending);

            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            /*
             * Keep enum values in their original types
             * during the SQL query. They are converted
             * to strings after the database returns them.
             */
            var rawItems =
                await query
                    .OrderByDescending(property =>
                        property.SubmittedAt)
                    .ThenByDescending(property =>
                        property.CreatedAt)
                    .Skip(
                        (page - 1) * pageSize)
                    .Take(
                        pageSize)
                    .Select(property =>
                        new
                        {
                            property.Id,
                            property.Title,
                            property.PropertyType,
                            property.SpaceType,
                            property.City,
                            property.PricePerNight,
                            property.Currency,

                            CoverImageUrl =
                                property.Images
                                    .Where(image =>
                                        image.IsCover)
                                    .OrderBy(image =>
                                        image.DisplayOrder)
                                    .Select(image =>
                                        image.Url)
                                    .FirstOrDefault(),

                            HostUserId =
                                property.HostProfile.UserId,

                            HostFirstName =
                                property.HostProfile
                                    .User
                                    .FirstName,

                            HostLastName =
                                property.HostProfile
                                    .User
                                    .LastName,

                            HostEmail =
                                property.HostProfile
                                    .User
                                    .Email,

                            property.SubmittedAt,
                            property.CreatedAt
                        })
                    .ToListAsync(
                        cancellationToken);

            var items =
                rawItems
                    .Select(item =>
                    {
                        var firstName =
                            item.HostFirstName
                            ?? string.Empty;

                        var lastName =
                            item.HostLastName
                            ?? string.Empty;

                        return
                            new AdminPendingPropertyItemResponse
                            {
                                Id =
                                    item.Id,

                                Title =
                                    item.Title,

                                PropertyType =
                                    item.PropertyType
                                        .ToString(),

                                SpaceType =
                                    item.SpaceType
                                        .ToString(),

                                City =
                                    item.City,

                                PricePerNight =
                                    item.PricePerNight,

                                Currency =
                                    item.Currency,

                                CoverImageUrl =
                                    item.CoverImageUrl,

                                HostUserId =
                                    item.HostUserId,

                                HostName =
                                    BuildFullName(
                                        firstName,
                                        lastName),

                                HostEmail =
                                    item.HostEmail
                                    ?? string.Empty,

                                SubmittedAt =
                                    item.SubmittedAt,

                                CreatedAt =
                                    item.CreatedAt
                            };
                    })
                    .ToList();

            var totalPages =
                totalCount == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalCount /
                        (double)pageSize);

            return new AdminPendingPropertiesResponse
            {
                Items =
                    items,

                Page =
                    page,

                PageSize =
                    pageSize,

                TotalCount =
                    totalCount,

                TotalPages =
                    totalPages
            };
        }

        public async Task<AdminPropertyDetailsResponse>
            GetByIdAsync(
                Guid propertyId,
                CancellationToken cancellationToken = default)
        {
            if (propertyId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The property identifier is invalid.");
            }

            var property =
                await _dbContext.Properties
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .Include(property =>
                        property.Images)
                    .Include(property =>
                        property.PropertyAmenities)
                    .ThenInclude(propertyAmenity =>
                        propertyAmenity.Amenity)
                    .Include(property =>
                        property.VerificationDocument)
                    .ThenInclude(document =>
                        document.Pages)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            return MapToDetailsResponse(
                property);
        }

        public async Task<ImageContentResult>
            GetVerificationDocumentPageContentAsync(
                Guid propertyId,
                Guid pageId,
                CancellationToken cancellationToken = default)
        {
            if (propertyId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The property identifier is invalid.");
            }

            if (pageId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The verification page identifier is invalid.");
            }

            /*
             * The query verifies both:
             *
             * 1. The page exists.
             * 2. The page belongs to the specified property.
             *
             * This prevents opening a page using a page ID
             * copied from another property.
             */
            var page =
                await _dbContext
                    .PropertyVerificationDocumentPages
                    .AsNoTracking()
                    .Where(documentPage =>
                        documentPage.Id == pageId
                        &&
                        documentPage
                            .VerificationDocument
                            .PropertyId == propertyId)
                    .Select(documentPage =>
                        new
                        {
                            documentPage.PublicId,
                            documentPage.Format
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (page is null)
            {
                throw new KeyNotFoundException(
                    "The verification document page was not found.");
            }

            return await _imageStorageService
                .DownloadAsync(
                    page.PublicId,
                    page.Format,
                    ImageAccessType.Authenticated,
                    cancellationToken);
        }

        public async Task<AdminPropertyReviewResponse>
            ApproveAsync(
                Guid propertyId,
                CancellationToken cancellationToken = default)
        {
            var property =
                await GetPendingPropertyForReviewAsync(
                    propertyId,
                    cancellationToken);

            /*
             * The property was valid during submission,
             * but the admin approval process checks it
             * again before making it publicly available.
             */
            EnsurePropertyCanBePublished(
                property);

            var currentTime =
                DateTimeOffset.UtcNow;

            property.Status =
                PropertyStatus.Published;

            property.ReviewedAt =
                currentTime;

            property.PublishedAt =
                currentTime;

            property.RejectionReason =
                null;

            property.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new AdminPropertyReviewResponse
            {
                Id =
                    property.Id,

                Status =
                    property.Status.ToString(),

                ReviewedAt =
                    currentTime,

                PublishedAt =
                    property.PublishedAt,

                RejectionReason =
                    null,

                Message =
                    "The property was approved and published successfully."
            };
        }

        public async Task<AdminPropertyReviewResponse>
            RejectAsync(
                Guid propertyId,
                string reason,
                CancellationToken cancellationToken = default)
        {
            var normalizedReason =
                NormalizeAndValidateRejectionReason(
                    reason);

            var property =
                await GetPendingPropertyForReviewAsync(
                    propertyId,
                    cancellationToken);

            var currentTime =
                DateTimeOffset.UtcNow;

            property.Status =
                PropertyStatus.Rejected;

            property.ReviewedAt =
                currentTime;

            property.PublishedAt =
                null;

            property.RejectionReason =
                normalizedReason;

            property.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new AdminPropertyReviewResponse
            {
                Id =
                    property.Id,

                Status =
                    property.Status.ToString(),

                ReviewedAt =
                    currentTime,

                PublishedAt =
                    null,

                RejectionReason =
                    normalizedReason,

                Message =
                    "The property was rejected successfully."
            };
        }

        private async Task<Property>
            GetPendingPropertyForReviewAsync(
                Guid propertyId,
                CancellationToken cancellationToken)
        {
            if (propertyId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The property identifier is invalid.");
            }

            var property =
                await _dbContext.Properties
                    .AsSplitQuery()
                    .Include(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .Include(property =>
                        property.Images)
                    .Include(property =>
                        property.VerificationDocument)
                    .ThenInclude(document =>
                        document.Pages)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            if (property.Status !=
                PropertyStatus.Pending)
            {
                throw new InvalidOperationException(
                    $"Only pending properties can be reviewed. " +
                    $"The current property status is " +
                    $"'{property.Status}'.");
            }

            return property;
        }

        private static void EnsurePropertyCanBePublished(
            Property property)
        {
            var errors =
                new List<string>();

            if (!property.HostProfile.User.IsActive)
            {
                errors.Add(
                    "The host account is inactive.");
            }

            if (property.HostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                errors.Add(
                    "The host application is no longer approved.");
            }

            ValidatePublicationBasicInformation(
                property,
                errors);

            ValidatePublicationLocation(
                property,
                errors);

            ValidatePublicationCapacity(
                property,
                errors);

            ValidatePublicationPricingAndPolicies(
                property,
                errors);

            ValidatePublicationHouseRules(
                property,
                errors);

            ValidatePublicationImages(
                property,
                errors);

            ValidatePublicationVerificationDocument(
                property,
                errors);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "The property can no longer be published. " +
                    string.Join(
                        " | ",
                        errors));
            }
        }

        private static void
            ValidatePublicationBasicInformation(
                Property property,
                ICollection<string> errors)
        {
            var title =
                property.Title?.Trim()
                ?? string.Empty;

            var description =
                property.Description?.Trim()
                ?? string.Empty;

            if (title.Length is < 10 or > 120)
            {
                errors.Add(
                    "The property title is invalid.");
            }

            if (description.Length is < 100 or > 3000)
            {
                errors.Add(
                    "The property description is invalid.");
            }

            if (!Enum.IsDefined(
                    property.PropertyType))
            {
                errors.Add(
                    "The property type is invalid.");
            }

            if (!Enum.IsDefined(
                    property.SpaceType))
            {
                errors.Add(
                    "The property space type is invalid.");
            }

            if (property.PropertyType ==
                    PropertyType.Studio
                &&
                property.SpaceType ==
                    PropertySpaceType.PrivateRoom)
            {
                errors.Add(
                    "A studio cannot be listed as a private room.");
            }
        }

        private static void ValidatePublicationLocation(
            Property property,
            ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(
                    property.Country))
            {
                errors.Add(
                    "The property country is missing.");
            }

            if (string.IsNullOrWhiteSpace(
                    property.City))
            {
                errors.Add(
                    "The property city is missing.");
            }

            if (string.IsNullOrWhiteSpace(
                    property.StreetAddress))
            {
                errors.Add(
                    "The property street address is missing.");
            }

            if (!property.Latitude.HasValue
                ||
                property.Latitude.Value is < -90 or > 90)
            {
                errors.Add(
                    "The property latitude is missing or invalid.");
            }

            if (!property.Longitude.HasValue
                ||
                property.Longitude.Value is < -180 or > 180)
            {
                errors.Add(
                    "The property longitude is missing or invalid.");
            }
        }

        private static void ValidatePublicationCapacity(
            Property property,
            ICollection<string> errors)
        {
            if (!property.MaxGuests.HasValue
                ||
                property.MaxGuests.Value is < 1 or > 20)
            {
                errors.Add(
                    "The maximum guests value is missing or invalid.");
            }

            if (!property.Bedrooms.HasValue
                ||
                property.Bedrooms.Value is < 0 or > 20)
            {
                errors.Add(
                    "The bedrooms value is missing or invalid.");
            }

            if (!property.Beds.HasValue
                ||
                property.Beds.Value is < 1 or > 30)
            {
                errors.Add(
                    "The beds value is missing or invalid.");
            }

            if (!property.Bathrooms.HasValue
                ||
                property.Bathrooms.Value is < 0.5m or > 20m)
            {
                errors.Add(
                    "The bathrooms value is missing or invalid.");
            }
        }

        private static void
            ValidatePublicationPricingAndPolicies(
                Property property,
                ICollection<string> errors)
        {
            if (!property.PricePerNight.HasValue
                ||
                property.PricePerNight.Value <= 0)
            {
                errors.Add(
                    "The price per night is missing or invalid.");
            }

            var currency =
                property.Currency?.Trim()
                ?? string.Empty;

            if (currency.Length != 3
                ||
                !currency.All(
                    char.IsAsciiLetter))
            {
                errors.Add(
                    "The property currency is invalid.");
            }

            if (!property.CheckInTime.HasValue)
            {
                errors.Add(
                    "The check-in time is missing.");
            }

            if (!property.CheckOutTime.HasValue)
            {
                errors.Add(
                    "The check-out time is missing.");
            }

            if (property.CheckInTime.HasValue
                &&
                property.CheckOutTime.HasValue
                &&
                property.CheckInTime.Value ==
                    property.CheckOutTime.Value)
            {
                errors.Add(
                    "The check-in and check-out times cannot be the same.");
            }

            if (!property.CancellationPolicy.HasValue
                ||
                !Enum.IsDefined(
                    property.CancellationPolicy.Value))
            {
                errors.Add(
                    "The cancellation policy is missing or invalid.");
            }
        }

        private static void
            ValidatePublicationHouseRules(
                Property property,
                ICollection<string> errors)
        {
            if (!property.AllowsSmoking.HasValue)
            {
                errors.Add(
                    "The smoking rule is missing.");
            }

            if (!property.AllowsPets.HasValue)
            {
                errors.Add(
                    "The pets rule is missing.");
            }

            if (!property.AllowsParties.HasValue)
            {
                errors.Add(
                    "The parties rule is missing.");
            }

            if (!property.AllowsChildren.HasValue)
            {
                errors.Add(
                    "The children rule is missing.");
            }

            if (property.AdditionalHouseRules?.Length >
                1000)
            {
                errors.Add(
                    "The additional house rules are too long.");
            }
        }

        private static void ValidatePublicationImages(
            Property property,
            ICollection<string> errors)
        {
            var images =
                property.Images.ToList();

            if (images.Count <
                MinimumPropertyImages)
            {
                errors.Add(
                    $"At least {MinimumPropertyImages} property images are required.");
            }

            if (images.Count >
                MaximumPropertyImages)
            {
                errors.Add(
                    $"No more than {MaximumPropertyImages} property images are allowed.");
            }

            if (images.Any(image =>
                    string.IsNullOrWhiteSpace(
                        image.Url)
                    ||
                    string.IsNullOrWhiteSpace(
                        image.PublicId)
                    ||
                    string.IsNullOrWhiteSpace(
                        image.Format)))
            {
                errors.Add(
                    "One or more property image records are incomplete.");
            }

            var coverImages =
                images
                    .Where(image =>
                        image.IsCover)
                    .ToList();

            if (coverImages.Count != 1)
            {
                errors.Add(
                    "Exactly one cover image is required.");
            }
            else if (coverImages[0].DisplayOrder != 1)
            {
                errors.Add(
                    "The cover image must have display order 1.");
            }

            var actualOrder =
                images
                    .Select(image =>
                        image.DisplayOrder)
                    .OrderBy(order =>
                        order)
                    .ToArray();

            var expectedOrder =
                Enumerable
                    .Range(
                        1,
                        images.Count)
                    .ToArray();

            if (!actualOrder.SequenceEqual(
                    expectedOrder))
            {
                errors.Add(
                    "The property image order is invalid.");
            }
        }

        private static void
            ValidatePublicationVerificationDocument(
                Property property,
                ICollection<string> errors)
        {
            var document =
                property.VerificationDocument;

            if (document is null)
            {
                errors.Add(
                    "The property verification document is missing.");

                return;
            }

            if (!Enum.IsDefined(
                    document.DocumentType))
            {
                errors.Add(
                    "The property verification document type is invalid.");
            }

            var pages =
                document.Pages.ToList();

            if (pages.Count == 0)
            {
                errors.Add(
                    "At least one verification document page is required.");
            }

            if (pages.Count >
                MaximumVerificationDocumentPages)
            {
                errors.Add(
                    $"No more than " +
                    $"{MaximumVerificationDocumentPages} " +
                    $"verification document pages are allowed.");
            }

            if (pages.Any(page =>
                    string.IsNullOrWhiteSpace(
                        page.PublicId)
                    ||
                    string.IsNullOrWhiteSpace(
                        page.Format)))
            {
                errors.Add(
                    "One or more verification document page records are incomplete.");
            }

            var actualPageNumbers =
                pages
                    .Select(page =>
                        page.PageNumber)
                    .OrderBy(pageNumber =>
                        pageNumber)
                    .ToArray();

            var expectedPageNumbers =
                Enumerable
                    .Range(
                        1,
                        pages.Count)
                    .ToArray();

            if (!actualPageNumbers.SequenceEqual(
                    expectedPageNumbers))
            {
                errors.Add(
                    "The verification document page order is invalid.");
            }
        }

        private static string
            NormalizeAndValidateRejectionReason(
                string reason)
        {
            if (string.IsNullOrWhiteSpace(
                    reason))
            {
                throw new ArgumentException(
                    "The rejection reason is required.");
            }

            var normalizedReason =
                reason.Trim();

            if (normalizedReason.Length <
                MinimumRejectionReasonLength)
            {
                throw new ArgumentException(
                    $"The rejection reason must contain at least " +
                    $"{MinimumRejectionReasonLength} characters.");
            }

            if (normalizedReason.Length >
                MaximumRejectionReasonLength)
            {
                throw new ArgumentException(
                    $"The rejection reason cannot exceed " +
                    $"{MaximumRejectionReasonLength} characters.");
            }

            return normalizedReason;
        }

        private static void ValidatePagination(
            int page,
            int pageSize)
        {
            if (page < 1)
            {
                throw new ArgumentException(
                    "The page number must be greater than or equal to 1.");
            }

            if (pageSize < 1
                ||
                pageSize > MaximumPageSize)
            {
                throw new ArgumentException(
                    $"The page size must be between 1 and " +
                    $"{MaximumPageSize}.");
            }
        }

        private static AdminPropertyDetailsResponse
            MapToDetailsResponse(
                Property property)
        {
            var hostUser =
                property.HostProfile.User;

            var firstName =
                hostUser.FirstName
                ?? string.Empty;

            var lastName =
                hostUser.LastName
                ?? string.Empty;

            var amenities =
                property.PropertyAmenities
                    .Where(propertyAmenity =>
                        propertyAmenity.Amenity is not null)
                    .OrderBy(propertyAmenity =>
                        propertyAmenity.Amenity.Category)
                    .ThenBy(propertyAmenity =>
                        propertyAmenity.Amenity.DisplayOrder)
                    .ThenBy(propertyAmenity =>
                        propertyAmenity.Amenity.Name)
                    .Select(propertyAmenity =>
                        new AdminPropertyAmenityResponse
                        {
                            Id =
                                propertyAmenity.Amenity.Id,

                            Code =
                                propertyAmenity.Amenity.Code,

                            Name =
                                propertyAmenity.Amenity.Name,

                            Category =
                                propertyAmenity.Amenity
                                    .Category
                                    .ToString(),

                            IconKey =
                                propertyAmenity.Amenity.IconKey,

                            DisplayOrder =
                                propertyAmenity.Amenity
                                    .DisplayOrder
                        })
                    .ToList();

            var images =
                property.Images
                    .OrderBy(image =>
                        image.DisplayOrder)
                    .ThenBy(image =>
                        image.CreatedAt)
                    .Select(image =>
                        new AdminPropertyImageResponse
                        {
                            Id =
                                image.Id,

                            Url =
                                image.Url,

                            Format =
                                image.Format,

                            IsCover =
                                image.IsCover,

                            DisplayOrder =
                                image.DisplayOrder,

                            CreatedAt =
                                image.CreatedAt
                        })
                    .ToList();

            AdminPropertyVerificationDocumentResponse?
                verificationDocumentResponse =
                    null;

            if (property.VerificationDocument
                is not null)
            {
                var document =
                    property.VerificationDocument;

                var pages =
                    document.Pages
                        .OrderBy(page =>
                            page.PageNumber)
                        .Select(page =>
                            new
                                AdminPropertyVerificationPageResponse
                            {
                                Id =
                                    page.Id,

                                PageNumber =
                                    page.PageNumber,

                                Format =
                                    page.Format,

                                CreatedAt =
                                    page.CreatedAt
                            })
                        .ToList();

                verificationDocumentResponse =
                    new
                        AdminPropertyVerificationDocumentResponse
                    {
                        Id =
                            document.Id,

                        DocumentType =
                            document.DocumentType.ToString(),

                        PagesCount =
                            pages.Count,

                        Pages =
                            pages,

                        CreatedAt =
                            document.CreatedAt,

                        UpdatedAt =
                            document.UpdatedAt
                    };
            }

            return new AdminPropertyDetailsResponse
            {
                Id =
                    property.Id,

                Title =
                    property.Title,

                Description =
                    property.Description,

                PropertyType =
                    property.PropertyType.ToString(),

                SpaceType =
                    property.SpaceType.ToString(),

                Status =
                    property.Status.ToString(),

                Host =
                    new AdminPropertyHostResponse
                    {
                        UserId =
                            property.HostProfile.UserId,

                        HostProfileId =
                            property.HostProfile.Id,

                        FirstName =
                            firstName,

                        LastName =
                            lastName,

                        FullName =
                            BuildFullName(
                                firstName,
                                lastName),

                        Email =
                            hostUser.Email
                            ?? string.Empty,

                        PhoneNumber =
                            hostUser.PhoneNumber,

                        IsActive =
                            hostUser.IsActive,

                        HostStatus =
                            property.HostProfile
                                .Status
                                .ToString()
                    },

                Country =
                    property.Country,

                City =
                    property.City,

                StreetAddress =
                    property.StreetAddress,

                BuildingNumber =
                    property.BuildingNumber,

                Floor =
                    property.Floor,

                ApartmentNumber =
                    property.ApartmentNumber,

                PostalCode =
                    property.PostalCode,

                Latitude =
                    property.Latitude,

                Longitude =
                    property.Longitude,

                MaxGuests =
                    property.MaxGuests,

                Bedrooms =
                    property.Bedrooms,

                Beds =
                    property.Beds,

                Bathrooms =
                    property.Bathrooms,

                PricePerNight =
                    property.PricePerNight,

                Currency =
                    property.Currency,

                CheckInTime =
                    property.CheckInTime,

                CheckOutTime =
                    property.CheckOutTime,

                CancellationPolicy =
                    property.CancellationPolicy?
                        .ToString(),

                AllowsSmoking =
                    property.AllowsSmoking,

                AllowsPets =
                    property.AllowsPets,

                AllowsParties =
                    property.AllowsParties,

                AllowsChildren =
                    property.AllowsChildren,

                AdditionalHouseRules =
                    property.AdditionalHouseRules,

                Amenities =
                    amenities,

                Images =
                    images,

                VerificationDocument =
                    verificationDocumentResponse,

                RejectionReason =
                    property.RejectionReason,

                CreatedAt =
                    property.CreatedAt,

                UpdatedAt =
                    property.UpdatedAt,

                SubmittedAt =
                    property.SubmittedAt,

                ReviewedAt =
                    property.ReviewedAt,

                PublishedAt =
                    property.PublishedAt
            };
        }

        private static string BuildFullName(
            string firstName,
            string lastName)
        {
            return string.Join(
                ' ',
                new[]
                {
                    firstName.Trim(),
                    lastName.Trim()
                }
                .Where(namePart =>
                    !string.IsNullOrWhiteSpace(
                        namePart)));
        }
    }
}