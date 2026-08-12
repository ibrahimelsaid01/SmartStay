using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed partial class HostPropertyService
        : IHostPropertyService
    {
        private const int MaximumPropertyImages =
            10;

        private const long MaximumImageSizeBytes =
            5 * 1024 * 1024;

        private const int
            MaximumVerificationDocumentPages = 5;

        private static readonly HashSet<string>
            AllowedImageContentTypes =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "image/jpeg",
                    "image/jpg",
                    "image/png",
                    "image/webp"
                };

        private static readonly HashSet<string>
            AllowedImageExtensions =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

        private readonly SmartStayDbContext
            _dbContext;

        private readonly IImageStorageService
            _imageStorageService;

        private readonly ILogger<HostPropertyService>
            _logger;

        public HostPropertyService(
            SmartStayDbContext dbContext,
            IImageStorageService imageStorageService,
            ILogger<HostPropertyService> logger)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            ArgumentNullException.ThrowIfNull(
                imageStorageService);

            ArgumentNullException.ThrowIfNull(
                logger);

            _dbContext =
                dbContext;

            _imageStorageService =
                imageStorageService;

            _logger =
                logger;
        }      

        public async Task<PropertyDraftResponse>
        CreateDraftAsync(
            Guid userId,
            CreatePropertyDraftRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            var hostProfile =
                await _dbContext.HostProfiles
                    .Include(host =>
                        host.User)
                    .SingleOrDefaultAsync(
                        host =>
                            host.UserId == userId,
                        cancellationToken);

            if (hostProfile is null)
            {
                throw new KeyNotFoundException(
                    "The host profile was not found.");
            }

            if (!hostProfile.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            if (!hostProfile.User.IsProfileCompleted)
            {
                throw new InvalidOperationException(
                    "Complete your user profile before creating a property.");
            }

            if (hostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only approved hosts can create properties.");
            }

            /*
             * Normalize values before validation
             * and database storage.
             */
            var title =
                request.Title.Trim();

            var description =
                request.Description.Trim();

            ValidateNormalizedBasicInformation(
                title,
                description);

            if (!request.PropertyType.HasValue)
            {
                throw new ArgumentException(
                    "The property type is required.");
            }

            if (!request.SpaceType.HasValue)
            {
                throw new ArgumentException(
                    "The property space type is required.");
            }

            var propertyType =
                request.PropertyType.Value;

            var spaceType =
                request.SpaceType.Value;

            if (!Enum.IsDefined(propertyType))
            {
                throw new ArgumentException(
                    "The selected property type is invalid.");
            }

            if (!Enum.IsDefined(spaceType))
            {
                throw new ArgumentException(
                    "The selected property space type is invalid.");
            }

            ValidatePropertyTypeCombination(
                propertyType,
                spaceType);

            var currentTime =
                DateTimeOffset.UtcNow;

            var property =
                new Property
                {
                    Id =
                        Guid.NewGuid(),

                    HostProfileId =
                        hostProfile.Id,

                    Title =
                        title,

                    Description =
                        description,

                    PropertyType =
                        propertyType,

                    SpaceType =
                        spaceType,

                    Currency =
                        "EGP",

                    Status =
                        PropertyStatus.Draft,

                    CreatedAt =
                        currentTime
                };

            await _dbContext.Properties
                .AddAsync(
                    property,
                    cancellationToken);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToDraftResponse(
                property);
        }

        public async Task<PropertyDraftResponse>
        GetByIdAsync(
            Guid userId,
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            var property =
                await _dbContext.Properties
                    .AsNoTracking()
                    .Where(property =>
                        property.Id == propertyId
                        &&
                        property.HostProfile.UserId == userId)
                    .Select(property =>
                        new PropertyDraftResponse
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

                            Currency =
                                property.Currency,

                            Status =
                                property.Status.ToString(),

                            CreatedAt =
                                property.CreatedAt,

                            UpdatedAt =
                                property.UpdatedAt
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            return property;
        }

        public async Task<PropertyDraftResponse>
        UpdateBasicInformationAsync(
            Guid userId,
            Guid propertyId,
            UpdatePropertyBasicInformationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            /*
             * Load only a property owned by the authenticated
             * host, together with the account information
             * required for authorization checks.
             */
            var property =
                await _dbContext.Properties
                    .Include(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId ==
                                userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            /*
             * The account and HostProfile must still be valid.
             */
            if (!property.HostProfile.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            if (property.HostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only approved hosts can manage properties.");
            }

            EnsurePropertyIsEditable(
                property);

            /*
             * Normalize string values.
             */
            var title =
                request.Title.Trim();

            var description =
                request.Description.Trim();

            ValidateNormalizedBasicInformation(
                title,
                description);

            if (!request.PropertyType.HasValue)
            {
                throw new ArgumentException(
                    "The property type is required.");
            }

            if (!request.SpaceType.HasValue)
            {
                throw new ArgumentException(
                    "The property space type is required.");
            }

            var propertyType =
                request.PropertyType.Value;

            var spaceType =
                request.SpaceType.Value;

            if (!Enum.IsDefined(propertyType))
            {
                throw new ArgumentException(
                    "The selected property type is invalid.");
            }

            if (!Enum.IsDefined(spaceType))
            {
                throw new ArgumentException(
                    "The selected property space type is invalid.");
            }

            ValidatePropertyTypeCombination(
                propertyType,
                spaceType);

            /*
             * Update only the basic-information fields.
             */
            property.Title =
                title;

            property.Description =
                description;

            property.PropertyType =
                propertyType;

            property.SpaceType =
                spaceType;

            property.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToDraftResponse(
                property);
        }

        public async Task<PropertyLocationResponse>
        UpdateLocationAsync(
            Guid userId,
            Guid propertyId,
            UpdatePropertyLocationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            /*
             * Load only a property owned by the
             * authenticated host.
             */
            var property =
                await _dbContext.Properties
                    .Include(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId ==
                                userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            /*
             * The owner account must still be active,
             * and its host application must remain approved.
             */
            if (!property.HostProfile.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            if (property.HostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only approved hosts can manage properties.");
            }

            /*
             * Reuses the helper created during the
             * basic-information implementation.
             */
            EnsurePropertyIsEditable(
                property);

            /*
             * Normalize required strings.
             */
            var country =
                request.Country.Trim();

            var city =
                request.City.Trim();

            var streetAddress =
                request.StreetAddress.Trim();

            /*
             * Normalize optional strings.
             * Empty values are stored as null rather than "".
             */
            var buildingNumber =
                NormalizeOptionalString(
                    request.BuildingNumber);

            var floor =
                NormalizeOptionalString(
                    request.Floor);

            var apartmentNumber =
                NormalizeOptionalString(
                    request.ApartmentNumber);

            var postalCode =
                NormalizeOptionalString(
                    request.PostalCode);

            ValidateNormalizedLocation(
                country,
                city,
                streetAddress,
                buildingNumber,
                floor,
                apartmentNumber,
                postalCode);

            if (!request.Latitude.HasValue)
            {
                throw new ArgumentException(
                    "Latitude is required.");
            }

            if (!request.Longitude.HasValue)
            {
                throw new ArgumentException(
                    "Longitude is required.");
            }

            var latitude =
                request.Latitude.Value;

            var longitude =
                request.Longitude.Value;

            ValidateCoordinates(
                latitude,
                longitude);

            /*
             * Update only location-related columns.
             */
            property.Country =
                country;

            property.City =
                city;

            property.StreetAddress =
                streetAddress;

            property.BuildingNumber =
                buildingNumber;

            property.Floor =
                floor;

            property.ApartmentNumber =
                apartmentNumber;

            property.PostalCode =
                postalCode;

            property.Latitude =
                latitude;

            property.Longitude =
                longitude;

            property.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToLocationResponse(
                property);
        }

        private static PropertyLocationResponse
        MapToLocationResponse(
            Property property)
        {
            return new PropertyLocationResponse
            {
                Id =
                    property.Id,

                Country =
                    property.Country
                    ?? string.Empty,

                City =
                    property.City
                    ?? string.Empty,

                StreetAddress =
                    property.StreetAddress
                    ?? string.Empty,

                BuildingNumber =
                    property.BuildingNumber,

                Floor =
                    property.Floor,

                ApartmentNumber =
                    property.ApartmentNumber,

                PostalCode =
                    property.PostalCode,

                Latitude =
                    property.Latitude
                    ?? 0,

                Longitude =
                    property.Longitude
                    ?? 0,

                Status =
                    property.Status.ToString(),

                UpdatedAt =
                    property.UpdatedAt
            };
        }

        public async Task<PropertyCapacityResponse>
        UpdateCapacityAsync(
            Guid userId,
            Guid propertyId,
            UpdatePropertyCapacityRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            var property =
                await _dbContext.Properties
                    .Include(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId ==
                                userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            if (!property.HostProfile.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            if (property.HostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only approved hosts can manage properties.");
            }

            EnsurePropertyIsEditable(
                property);

            if (!request.MaxGuests.HasValue)
            {
                throw new ArgumentException(
                    "Maximum guests is required.");
            }

            if (!request.Bedrooms.HasValue)
            {
                throw new ArgumentException(
                    "Bedrooms is required.");
            }

            if (!request.Beds.HasValue)
            {
                throw new ArgumentException(
                    "Beds is required.");
            }

            if (!request.Bathrooms.HasValue)
            {
                throw new ArgumentException(
                    "Bathrooms is required.");
            }

            var maxGuests =
                request.MaxGuests.Value;

            var bedrooms =
                request.Bedrooms.Value;

            var beds =
                request.Beds.Value;

            var bathrooms =
                request.Bathrooms.Value;

            ValidateCapacity(
                maxGuests,
                bedrooms,
                beds,
                bathrooms);

            property.MaxGuests =
                maxGuests;

            property.Bedrooms =
                bedrooms;

            property.Beds =
                beds;

            property.Bathrooms =
                bathrooms;

            property.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToCapacityResponse(
                property);
        }

        public async Task<PropertyPricingAndPoliciesResponse>
        UpdatePricingAndPoliciesAsync(
            Guid userId,
            Guid propertyId,
            UpdatePropertyPricingAndPoliciesRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            var property =
                await _dbContext.Properties
                    .Include(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId ==
                                userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            if (!property.HostProfile.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            if (property.HostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only approved hosts can manage properties.");
            }

            EnsurePropertyIsEditable(
                property);

            if (!request.PricePerNight.HasValue)
            {
                throw new ArgumentException(
                    "Price per night is required.");
            }

            if (!request.CheckInTime.HasValue)
            {
                throw new ArgumentException(
                    "Check-in time is required.");
            }

            if (!request.CheckOutTime.HasValue)
            {
                throw new ArgumentException(
                    "Check-out time is required.");
            }

            if (!request.CancellationPolicy.HasValue)
            {
                throw new ArgumentException(
                    "Cancellation policy is required.");
            }

            var pricePerNight =
                request.PricePerNight.Value;

            var checkInTime =
                request.CheckInTime.Value;

            var checkOutTime =
                request.CheckOutTime.Value;

            var cancellationPolicy =
                request.CancellationPolicy.Value;

            var currency =
                CurrencyCodeNormalizer.NormalizeForStorage(
                    request.Currency);

            ValidatePricingAndPolicies(
                pricePerNight,
                currency,
                checkInTime,
                checkOutTime,
                cancellationPolicy);

            property.PricePerNight =
                pricePerNight;

            property.Currency =
                currency;

            property.CheckInTime =
                checkInTime;

            property.CheckOutTime =
                checkOutTime;

            property.CancellationPolicy =
                cancellationPolicy;

            property.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToPricingAndPoliciesResponse(
                property);
        }

        public async Task<PropertyHouseRulesResponse>
        UpdateHouseRulesAsync(
            Guid userId,
            Guid propertyId,
            UpdatePropertyHouseRulesRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            var property =
                await _dbContext.Properties
                    .Include(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId ==
                                userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            if (!property.HostProfile.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            if (property.HostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only approved hosts can manage properties.");
            }

            EnsurePropertyIsEditable(
                property);

            if (!request.AllowsSmoking.HasValue)
            {
                throw new ArgumentException(
                    "You must specify whether smoking is allowed.");
            }

            if (!request.AllowsPets.HasValue)
            {
                throw new ArgumentException(
                    "You must specify whether pets are allowed.");
            }

            if (!request.AllowsParties.HasValue)
            {
                throw new ArgumentException(
                    "You must specify whether parties are allowed.");
            }

            if (!request.AllowsChildren.HasValue)
            {
                throw new ArgumentException(
                    "You must specify whether children are allowed.");
            }

            var additionalHouseRules =
                NormalizeOptionalString(
                    request.AdditionalHouseRules);

            ValidateAdditionalHouseRules(
                additionalHouseRules);

            property.AllowsSmoking =
                request.AllowsSmoking.Value;

            property.AllowsPets =
                request.AllowsPets.Value;

            property.AllowsParties =
                request.AllowsParties.Value;

            property.AllowsChildren =
                request.AllowsChildren.Value;

            property.AdditionalHouseRules =
                additionalHouseRules;

            property.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToHouseRulesResponse(
                property);
        }

        public async Task<PropertyAmenitiesResponse>
        GetAmenitiesAsync(
            Guid userId,
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            var property =
                await _dbContext.Properties
                    .AsNoTracking()
                    .Include(property =>
                        property.PropertyAmenities)
                    .ThenInclude(propertyAmenity =>
                        propertyAmenity.Amenity)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId ==
                                userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            return MapToAmenitiesResponse(
                property);
        }

        public async Task<PropertyAmenitiesResponse>
        UpdateAmenitiesAsync(
            Guid userId,
            Guid propertyId,
            UpdatePropertyAmenitiesRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            if (request.AmenityIds is null)
            {
                throw new ArgumentException(
                    "The amenity IDs collection is required.");
            }

            var property =
                await _dbContext.Properties
                    .Include(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .Include(property =>
                        property.PropertyAmenities)
                    .ThenInclude(propertyAmenity =>
                        propertyAmenity.Amenity)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId ==
                                userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            if (!property.HostProfile.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            if (property.HostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only approved hosts can manage properties.");
            }

            EnsurePropertyIsEditable(
                property);

            /*
             * Remove duplicated IDs without changing
             * the actual meaning of the request.
             */
            var requestedAmenityIds =
                request.AmenityIds
                    .Distinct()
                    .ToArray();

            var selectedAmenities =
                await _dbContext.Amenities
                    .Where(amenity =>
                        amenity.IsActive
                        &&
                        requestedAmenityIds.Contains(
                            amenity.Id))
                    .OrderBy(amenity =>
                        amenity.Category)
                    .ThenBy(amenity =>
                        amenity.DisplayOrder)
                    .ThenBy(amenity =>
                        amenity.Name)
                    .ToListAsync(
                        cancellationToken);

            if (selectedAmenities.Count !=
                requestedAmenityIds.Length)
            {
                throw new ArgumentException(
                    "One or more selected amenities do not exist or are inactive.");
            }

            /*
             * PUT means full replacement:
             * remove old selections, then add the new list.
             */
            _dbContext.PropertyAmenities
                .RemoveRange(
                    property.PropertyAmenities);

            var currentTime =
                DateTimeOffset.UtcNow;

            var newPropertyAmenities =
                selectedAmenities
                    .Select(amenity =>
                        new PropertyAmenity
                        {
                            PropertyId =
                                property.Id,

                            AmenityId =
                                amenity.Id,

                            CreatedAt =
                                currentTime,

                            Property =
                                property,

                            Amenity =
                                amenity
                        })
                    .ToList();

            if (newPropertyAmenities.Count > 0)
            {
                await _dbContext.PropertyAmenities
                    .AddRangeAsync(
                        newPropertyAmenities,
                        cancellationToken);
            }

            property.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            /*
             * Replace the in-memory navigation so the
             * response represents the new selections.
             */
            property.PropertyAmenities =
                newPropertyAmenities;

            return MapToAmenitiesResponse(
                property);
        }

        public async Task<PropertyImagesResponse>
        GetImagesAsync(
            Guid userId,
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            var property =
                await _dbContext.Properties
                    .AsNoTracking()
                    .Include(property =>
                        property.Images)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId == userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            return MapToImagesResponse(
                property);
        }

        public async Task<PropertyImagesResponse> UploadImagesAsync(
            Guid userId,
            Guid propertyId,
            IReadOnlyCollection<IFormFile> files,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                files);

            var property =
                await GetOwnedEditablePropertyWithImagesAsync(
                    userId,
                    propertyId,
                    cancellationToken);

            ValidatePropertyImageFiles(
                files,
                property.Images.Count);

            /*
             * Repair an inconsistent old order before appending
             * any new images.
             */
            NormalizePropertyImageOrder(
                property.Images);

            var uploadedImages =
                new List<ImageUploadResult>();

            var newPropertyImages =
                new List<PropertyImage>();

            var folder =
                $"smartstay/properties/{property.Id}/gallery";

            var databaseCommitted =
                false;

            try
            {
                /*
                 * Every file is validated before this loop.
                 * If any upload fails, the catch block removes
                 * all files uploaded during this request.
                 */
                foreach (var file in files)
                {
                    await using var fileStream =
                        file.OpenReadStream();

                    var uploadResult =
                        await _imageStorageService.UploadAsync(
                            fileStream,
                            file.FileName,
                            file.ContentType,
                            folder,
                            ImageAccessType.Public,
                            cancellationToken);

                    uploadedImages.Add(
                        uploadResult);
                }

                var currentTime =
                    DateTimeOffset.UtcNow;

                var existingImagesCount =
                    property.Images.Count;

                for (var index = 0;
                     index < uploadedImages.Count;
                     index++)
                {
                    var uploadedImage =
                        uploadedImages[index];

                    var isFirstPropertyImage =
                        existingImagesCount == 0
                        &&
                        index == 0;

                    var propertyImage =
                        new PropertyImage
                        {
                            Id =
                                Guid.NewGuid(),

                            PropertyId =
                                property.Id,

                            Url =
                                uploadedImage.SecureUrl,

                            PublicId =
                                uploadedImage.PublicId,

                            Format =
                                uploadedImage.Format,

                            IsCover =
                                isFirstPropertyImage,

                            DisplayOrder =
                                existingImagesCount
                                + index
                                + 1,

                            CreatedAt =
                                currentTime,

                            Property =
                                property
                        };

                    newPropertyImages.Add(
                        propertyImage);

                    property.Images.Add(
                        propertyImage);
                }

                /*
                 * Guarantees exactly one cover and a contiguous
                 * display order before saving.
                 */
                NormalizePropertyImageOrder(
                    property.Images);

                await using var transaction =
                    await _dbContext.Database
                        .BeginTransactionAsync(
                            cancellationToken);

                await _dbContext.PropertyImages
                    .AddRangeAsync(
                        newPropertyImages,
                        cancellationToken);

                property.UpdatedAt =
                    currentTime;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                databaseCommitted =
                    true;

                return MapToImagesResponse(
                    property);
            }
            catch
            {
                if (!databaseCommitted)
                {
                    await DeleteUploadedImagesQuietlyAsync(
                        uploadedImages,
                        ImageAccessType.Public,
                        "unsuccessful property gallery upload");
                }

                throw;
            }
        }

        public async Task<PropertyImagesResponse> DeleteImageAsync(
            Guid userId,
            Guid propertyId,
            Guid imageId,
            CancellationToken cancellationToken = default)
        {
            var property =
                await GetOwnedEditablePropertyWithImagesAsync(
                    userId,
                    propertyId,
                    cancellationToken);

            var image =
                property.Images
                    .SingleOrDefault(candidate =>
                        candidate.Id == imageId);

            if (image is null)
            {
                throw new KeyNotFoundException(
                    "The property image was not found.");
            }

            var publicId =
                image.PublicId;

            property.Images.Remove(
                image);

            _dbContext.PropertyImages.Remove(
                image);

            /*
             * If the deleted image was the cover,
             * normalization promotes the first remaining image.
             */
            NormalizePropertyImageOrder(
                property.Images);

            property.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            /*
             * Commit the database state first. An orphaned
             * Cloudinary asset can be cleaned later, while a
             * database record pointing to a missing asset would
             * break the property gallery.
             */
            try
            {
                var deletionResult =
                    await _imageStorageService.DeleteAsync(
                        publicId,
                        ImageAccessType.Public,
                        CancellationToken.None);

                if (!deletionResult.IsDeleted)
                {
                    _logger.LogWarning(
                        "Cloudinary did not confirm deletion of " +
                        "property image {PublicId}. Provider result: {ProviderResult}",
                        publicId,
                        deletionResult.ProviderResult);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Property image {PublicId} was removed from " +
                    "the database, but Cloudinary cleanup failed.",
                    publicId);
            }

            return MapToImagesResponse(
                property);
        }

        public async Task<PropertyImagesResponse> SetCoverImageAsync(
            Guid userId,
            Guid propertyId,
            Guid imageId,
            CancellationToken cancellationToken = default)
        {
            var property =
                await GetOwnedEditablePropertyWithImagesAsync(
                    userId,
                    propertyId,
                    cancellationToken);

            var selectedImage =
                property.Images
                    .SingleOrDefault(image =>
                        image.Id == imageId);

            if (selectedImage is null)
            {
                throw new KeyNotFoundException(
                    "The property image was not found.");
            }

            foreach (var image in property.Images)
            {
                image.IsCover =
                    image.Id == selectedImage.Id;
            }

            NormalizePropertyImageOrder(
                property.Images);

            property.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToImagesResponse(
                property);
        }

        public async Task<PropertyImagesResponse> UpdateImageOrderAsync(
            Guid userId,
            Guid propertyId,
            UpdatePropertyImageOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            if (request.ImageIds is null)
            {
                throw new ArgumentException(
                    "The image IDs collection is required.");
            }

            var property =
                await GetOwnedEditablePropertyWithImagesAsync(
                    userId,
                    propertyId,
                    cancellationToken);

            if (request.ImageIds.Any(imageId =>
                    imageId == Guid.Empty))
            {
                throw new ArgumentException(
                    "The image IDs collection contains an invalid identifier.");
            }

            var distinctImageIds =
                request.ImageIds
                    .Distinct()
                    .ToArray();

            if (distinctImageIds.Length !=
                request.ImageIds.Count)
            {
                throw new ArgumentException(
                    "The image IDs collection cannot contain duplicate values.");
            }

            if (request.ImageIds.Count !=
                property.Images.Count)
            {
                throw new ArgumentException(
                    "The image order must contain every property image exactly once.");
            }

            var currentImageIds =
                property.Images
                    .Select(image =>
                        image.Id)
                    .ToHashSet();

            if (!currentImageIds.SetEquals(
                    request.ImageIds))
            {
                throw new ArgumentException(
                    "One or more image IDs do not belong to this property.");
            }

            if (property.Images.Count == 0)
            {
                return MapToImagesResponse(
                    property);
            }

            NormalizePropertyImageOrder(
                property.Images);

            var coverImage =
                property.Images
                    .Single(image =>
                        image.IsCover);

            if (request.ImageIds[0] !=
                coverImage.Id)
            {
                throw new ArgumentException(
                    "The cover image must remain the first image in the order.");
            }

            var imagesById =
                property.Images
                    .ToDictionary(
                        image => image.Id);

            for (var index = 0;
                 index < request.ImageIds.Count;
                 index++)
            {
                imagesById[
                    request.ImageIds[index]]
                    .DisplayOrder =
                        index + 1;
            }

            property.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToImagesResponse(
                property);
        }

        public async Task<PropertyVerificationDocumentResponse>
        GetVerificationDocumentAsync(
            Guid userId,
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            var property =
                await _dbContext.Properties
                    .AsNoTracking()
                    .Include(property =>
                        property.VerificationDocument)
                    .ThenInclude(document =>
                        document.Pages)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId ==
                                userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            if (property.VerificationDocument is null)
            {
                throw new KeyNotFoundException(
                    "The property verification document was not found.");
            }

            return MapToVerificationDocumentResponse(
                property);
        }

        public async Task<PropertyVerificationDocumentResponse>
            UploadVerificationDocumentAsync(
                Guid userId,
                Guid propertyId,
                PropertyVerificationDocumentType documentType,
                IReadOnlyCollection<IFormFile> files,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                files);

            if (!Enum.IsDefined(documentType))
            {
                throw new ArgumentException(
                    "The selected verification document type is invalid.");
            }

            var property =
                await _dbContext.Properties
                    .Include(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .Include(property =>
                        property.VerificationDocument)
                    .ThenInclude(document =>
                        document.Pages)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId ==
                                userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            if (!property.HostProfile.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            if (property.HostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only approved hosts can manage properties.");
            }

            EnsurePropertyIsEditable(
                property);

            ValidateVerificationDocumentFiles(
                files);

            var oldDocumentPublicIds =
                property.VerificationDocument?
                    .Pages
                    .Select(page =>
                        page.PublicId)
                    .ToArray()
                ??
                Array.Empty<string>();

            var uploadedPages =
                new List<ImageUploadResult>();

            var folder =
                $"smartstay/property-verifications/" +
                $"{property.Id}/documents";

            var databaseCommitted =
                false;

            try
            {
                foreach (var file in files)
                {
                    await using var fileStream =
                        file.OpenReadStream();

                    var uploadResult =
                        await _imageStorageService.UploadAsync(
                            fileStream,
                            file.FileName,
                            file.ContentType,
                            folder,
                            ImageAccessType.Authenticated,
                            cancellationToken);

                    uploadedPages.Add(
                        uploadResult);
                }

                var currentTime =
                    DateTimeOffset.UtcNow;

                var verificationDocument =
                    new PropertyVerificationDocument
                    {
                        Id =
                            Guid.NewGuid(),

                        PropertyId =
                            property.Id,

                        DocumentType =
                            documentType,

                        CreatedAt =
                            currentTime,

                        Property =
                            property
                    };

                for (var index = 0;
                     index < uploadedPages.Count;
                     index++)
                {
                    var uploadedPage =
                        uploadedPages[index];

                    var documentPage =
                        new PropertyVerificationDocumentPage
                        {
                            Id =
                                Guid.NewGuid(),

                            VerificationDocumentId =
                                verificationDocument.Id,

                            PublicId =
                                uploadedPage.PublicId,

                            Format =
                                uploadedPage.Format,

                            PageNumber =
                                index + 1,

                            CreatedAt =
                                currentTime,

                            VerificationDocument =
                                verificationDocument
                        };

                    verificationDocument.Pages.Add(
                        documentPage);
                }

                await using var transaction =
                    await _dbContext.Database
                        .BeginTransactionAsync(
                            cancellationToken);

                if (property.VerificationDocument is not null)
                {
                    _dbContext
                        .PropertyVerificationDocuments
                        .Remove(
                            property.VerificationDocument);

                    property.VerificationDocument =
                        null;

                    await _dbContext.SaveChangesAsync(
                        cancellationToken);
                }

                await _dbContext
                    .PropertyVerificationDocuments
                    .AddAsync(
                        verificationDocument,
                        cancellationToken);

                property.VerificationDocument =
                    verificationDocument;

                property.UpdatedAt =
                    currentTime;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                databaseCommitted =
                    true;

                await DeleteStoredImagesQuietlyAsync(
                    oldDocumentPublicIds,
                    ImageAccessType.Authenticated,
                    "old property verification document replacement");

                return MapToVerificationDocumentResponse(
                    property);
            }
            catch
            {
                if (!databaseCommitted)
                {
                    await DeleteUploadedImagesQuietlyAsync(
                        uploadedPages,
                        ImageAccessType.Authenticated,
                        "unsuccessful property verification upload");
                }

                throw;
            }
        }

        public async Task
        DeleteVerificationDocumentAsync(
            Guid userId,
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            var property =
                await _dbContext.Properties
                    .Include(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .Include(property =>
                        property.VerificationDocument)
                    .ThenInclude(document =>
                        document.Pages)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId ==
                                userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            if (!property.HostProfile.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            if (property.HostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only approved hosts can manage properties.");
            }

            EnsurePropertyIsEditable(
                property);

            var verificationDocument =
                property.VerificationDocument;

            if (verificationDocument is null)
            {
                throw new KeyNotFoundException(
                    "The property verification document was not found.");
            }

            var publicIds =
                verificationDocument.Pages
                    .Select(page =>
                        page.PublicId)
                    .ToArray();

            _dbContext.PropertyVerificationDocuments
                .Remove(
                    verificationDocument);

            property.VerificationDocument =
                null;

            property.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await DeleteStoredImagesQuietlyAsync(
                publicIds,
                ImageAccessType.Authenticated,
                "deleted property verification document");
        }

        public async Task<PropertySubmissionResponse> SubmitAsync(
            Guid userId,
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            var property =
                await _dbContext.Properties
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
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId ==
                                userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            if (!property.HostProfile.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            if (!property.HostProfile.User.IsProfileCompleted)
            {
                throw new InvalidOperationException(
                    "Complete your user profile before submitting a property.");
            }

            if (property.HostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only approved hosts can submit properties.");
            }

            EnsurePropertyCanBeSubmitted(
                property);

            var validationErrors =
                GetPropertySubmissionValidationErrors(
                    property);

            if (validationErrors.Count > 0)
            {
                throw new ArgumentException(
                    "The property is not ready for submission. " +
                    string.Join(" | ", validationErrors));
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            property.Status =
                PropertyStatus.Pending;

            property.SubmittedAt =
                currentTime;

            property.UpdatedAt =
                currentTime;

            property.ReviewedAt =
                null;

            property.RejectionReason =
                null;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return MapToSubmissionResponse(
                property);
        }

        private static
        PropertyVerificationDocumentResponse
        MapToVerificationDocumentResponse(
            Property property)
        {
            var document =
                property.VerificationDocument
                ??
                throw new InvalidOperationException(
                    "The verification document is missing.");

            var pages =
                document.Pages
                    .OrderBy(page =>
                        page.PageNumber)
                    .Select(page =>
                        new
                            PropertyVerificationDocumentPageResponse
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

            return new PropertyVerificationDocumentResponse
            {
                PropertyId =
                    property.Id,

                DocumentId =
                    document.Id,

                DocumentType =
                    document.DocumentType.ToString(),

                PagesCount =
                    pages.Count,

                Pages =
                    pages,

                Status =
                    property.Status.ToString(),

                CreatedAt =
                    document.CreatedAt,

                UpdatedAt =
                    document.UpdatedAt
            };
        }

        private async Task DeleteStoredImagesQuietlyAsync(
            IEnumerable<string> publicIds,
            ImageAccessType accessType,
            string operationContext)
        {
            foreach (var publicId in publicIds)
            {
                try
                {
                    var deletionResult =
                        await _imageStorageService.DeleteAsync(
                            publicId,
                            accessType,
                            CancellationToken.None);

                    if (!deletionResult.IsDeleted)
                    {
                        _logger.LogWarning(
                            "Image storage did not confirm deletion of " +
                            "{PublicId} during {OperationContext}. " +
                            "Provider result: {ProviderResult}",
                            publicId,
                            operationContext,
                            deletionResult.ProviderResult);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Failed to delete stored image " +
                        "{PublicId} during {OperationContext}.",
                        publicId,
                        operationContext);
                }
            }
        }

        private async Task DeleteUploadedImagesQuietlyAsync(
            IEnumerable<ImageUploadResult> uploadedImages,
            ImageAccessType accessType,
            string operationContext)
        {
            foreach (var uploadedImage in uploadedImages)
            {
                try
                {
                    var deletionResult =
                        await _imageStorageService.DeleteAsync(
                            uploadedImage.PublicId,
                            accessType,
                            CancellationToken.None);

                    if (!deletionResult.IsDeleted)
                    {
                        _logger.LogWarning(
                            "Image storage did not confirm deletion of " +
                            "{PublicId} during {OperationContext}. " +
                            "Provider result: {ProviderResult}",
                            uploadedImage.PublicId,
                            operationContext,
                            deletionResult.ProviderResult);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Failed to delete uploaded image " +
                        "{PublicId} during {OperationContext}.",
                        uploadedImage.PublicId,
                        operationContext);
                }
            }
        }

        public async Task<ImageContentResult>
        GetVerificationDocumentPageContentAsync(
            Guid userId,
            Guid propertyId,
            Guid pageId,
            CancellationToken cancellationToken = default)
        {
            var page =
                await _dbContext
                    .PropertyVerificationDocumentPages
                    .AsNoTracking()
                    .Where(page =>
                        page.Id == pageId
                        &&
                        page.VerificationDocument
                            .PropertyId == propertyId
                        &&
                        page.VerificationDocument
                            .Property
                            .HostProfile
                            .UserId == userId)
                    .Select(page =>
                        new
                        {
                            page.PublicId,
                            page.Format
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

        private static void
        ValidateVerificationDocumentFiles(
            IReadOnlyCollection<IFormFile> files)
        {
            if (files.Count == 0)
            {
                throw new ArgumentException(
                    "At least one document page is required.");
            }

            if (files.Count >
                MaximumVerificationDocumentPages)
            {
                throw new ArgumentException(
                    "A property verification document " +
                    "cannot contain more than 5 pages.");
            }

            foreach (var file in files)
            {
                if (file is null
                    ||
                    file.Length == 0)
                {
                    throw new ArgumentException(
                        "Empty document pages are not allowed.");
                }

                if (file.Length >
                    MaximumImageSizeBytes)
                {
                    throw new ArgumentException(
                        $"The document page '{file.FileName}' " +
                        "exceeds the maximum allowed size of 5 MB.");
                }

                if (!AllowedImageContentTypes.Contains(
                        file.ContentType))
                {
                    throw new ArgumentException(
                        $"The document page '{file.FileName}' " +
                        "has an unsupported content type.");
                }

                var extension =
                    Path.GetExtension(
                        file.FileName);

                if (string.IsNullOrWhiteSpace(extension)
                    ||
                    !AllowedImageExtensions.Contains(
                        extension))
                {
                    throw new ArgumentException(
                        $"The document page '{file.FileName}' " +
                        "must be JPG, JPEG, PNG, or WebP.");
                }
            }
        }

        private async Task<Property>
            GetOwnedEditablePropertyWithImagesAsync(
                Guid userId,
                Guid propertyId,
                CancellationToken cancellationToken)
        {
            var property =
                await _dbContext.Properties
                    .Include(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .Include(property =>
                        property.Images)
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId == userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            if (!property.HostProfile.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            if (property.HostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only approved hosts can manage properties.");
            }

            EnsurePropertyIsEditable(
                property);

            return property;
        }

        private static void ValidatePropertyImageFiles(
            IReadOnlyCollection<IFormFile> files,
            int existingImagesCount)
        {
            if (files.Count == 0)
            {
                throw new ArgumentException(
                    "At least one property image is required.");
            }

            if (existingImagesCount + files.Count >
                MaximumPropertyImages)
            {
                throw new ArgumentException(
                    $"A property cannot contain more than " +
                    $"{MaximumPropertyImages} images.");
            }

            foreach (var file in files)
            {
                if (file is null
                    ||
                    file.Length == 0)
                {
                    throw new ArgumentException(
                        "Empty image files are not allowed.");
                }

                if (file.Length >
                    MaximumImageSizeBytes)
                {
                    throw new ArgumentException(
                        $"The image '{file.FileName}' exceeds " +
                        "the maximum allowed size of 5 MB.");
                }

                if (!AllowedImageContentTypes.Contains(
                        file.ContentType))
                {
                    throw new ArgumentException(
                        $"The image '{file.FileName}' has an " +
                        "unsupported content type.");
                }

                var extension =
                    Path.GetExtension(
                        file.FileName);

                if (string.IsNullOrWhiteSpace(
                        extension)
                    ||
                    !AllowedImageExtensions.Contains(
                        extension))
                {
                    throw new ArgumentException(
                        $"The image '{file.FileName}' must be " +
                        "JPG, JPEG, PNG, or WebP.");
                }
            }
        }

        private static void NormalizePropertyImageOrder(
            ICollection<PropertyImage> images)
        {
            if (images.Count == 0)
            {
                return;
            }

            var initiallyOrderedImages =
                images
                    .OrderBy(image =>
                        image.DisplayOrder <= 0
                            ? int.MaxValue
                            : image.DisplayOrder)
                    .ThenBy(image =>
                        image.CreatedAt)
                    .ThenBy(image =>
                        image.Id)
                    .ToList();

            /*
             * Keep the first existing cover. If none exists,
             * promote the first image.
             */
            var coverImage =
                initiallyOrderedImages
                    .FirstOrDefault(image =>
                        image.IsCover)
                ??
                initiallyOrderedImages[0];

            foreach (var image in initiallyOrderedImages)
            {
                image.IsCover =
                    image.Id == coverImage.Id;
            }

            var finalOrder =
                initiallyOrderedImages
                    .OrderByDescending(image =>
                        image.IsCover)
                    .ThenBy(image =>
                        image.DisplayOrder <= 0
                            ? int.MaxValue
                            : image.DisplayOrder)
                    .ThenBy(image =>
                        image.CreatedAt)
                    .ThenBy(image =>
                        image.Id)
                    .ToList();

            for (var index = 0;
                 index < finalOrder.Count;
                 index++)
            {
                finalOrder[index].DisplayOrder =
                    index + 1;
            }
        }

        private static PropertyImagesResponse MapToImagesResponse(
            Property property)
        {
            var images =
                property.Images
                    .OrderBy(image =>
                        image.DisplayOrder)
                    .ThenBy(image =>
                        image.CreatedAt)
                    .Select(image =>
                        new PropertyImageResponse
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

            return new PropertyImagesResponse
            {
                PropertyId =
                    property.Id,

                ImagesCount =
                    images.Count,

                CoverImageId =
                    images
                        .FirstOrDefault(image =>
                            image.IsCover)?
                        .Id,

                Images =
                    images,

                Status =
                    property.Status.ToString(),

                UpdatedAt =
                    property.UpdatedAt
            };
        }

        private static PropertyAmenitiesResponse
        MapToAmenitiesResponse(
            Property property)
        {
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
                        new AmenityResponse
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
                                propertyAmenity.Amenity
                                    .IconKey,

                            DisplayOrder =
                                propertyAmenity.Amenity
                                    .DisplayOrder
                        })
                    .ToList();

            return new PropertyAmenitiesResponse
            {
                PropertyId =
                    property.Id,

                SelectedAmenitiesCount =
                    amenities.Count,

                Amenities =
                    amenities,

                Status =
                    property.Status.ToString(),

                UpdatedAt =
                    property.UpdatedAt
            };
        }

        private static void ValidateAdditionalHouseRules(
            string? additionalHouseRules)
        {
            if (additionalHouseRules?.Length > 1000)
            {
                throw new ArgumentException(
                    "Additional house rules cannot exceed 1000 characters.");
            }
        }

        private static PropertyHouseRulesResponse
        MapToHouseRulesResponse(
            Property property)
        {
            return new PropertyHouseRulesResponse
            {
                Id =
                    property.Id,

                AllowsSmoking =
                    property.AllowsSmoking
                    ?? false,

                AllowsPets =
                    property.AllowsPets
                    ?? false,

                AllowsParties =
                    property.AllowsParties
                    ?? false,

                AllowsChildren =
                    property.AllowsChildren
                    ?? false,

                AdditionalHouseRules =
                    property.AdditionalHouseRules,

                Status =
                    property.Status.ToString(),

                UpdatedAt =
                    property.UpdatedAt
            };
        }

        private static PropertyPricingAndPoliciesResponse
        MapToPricingAndPoliciesResponse(
            Property property)
        {
            return new PropertyPricingAndPoliciesResponse
            {
                Id =
                    property.Id,

                PricePerNight =
                    property.PricePerNight
                    ?? 0,

                Currency =
                    property.Currency,

                CheckInTime =
                    property.CheckInTime
                    ?? default,

                CheckOutTime =
                    property.CheckOutTime
                    ?? default,

                CancellationPolicy =
                    property.CancellationPolicy?
                        .ToString()
                    ?? string.Empty,

                Status =
                    property.Status.ToString(),

                UpdatedAt =
                    property.UpdatedAt
            };
        }

        private static void ValidatePricingAndPolicies(
            decimal pricePerNight,
            string currency,
            TimeOnly checkInTime,
            TimeOnly checkOutTime,
            CancellationPolicyType cancellationPolicy)
        {
            const decimal maximumDatabasePrice =
                9999999999999999.99m;

            if (pricePerNight <= 0)
            {
                throw new ArgumentException(
                    "Price per night must be greater than zero.");
            }

            if (pricePerNight > maximumDatabasePrice)
            {
                throw new ArgumentException(
                    "Price per night exceeds the supported maximum value.");
            }

            _ = CurrencyCodeNormalizer.NormalizeForStorage(
                currency);

            if (!Enum.IsDefined(cancellationPolicy))
            {
                throw new ArgumentException(
                    "The selected cancellation policy is invalid.");
            }

            if (checkInTime == checkOutTime)
            {
                throw new ArgumentException(
                    "Check-in time and check-out time cannot be the same.");
            }
        }

        private static PropertyCapacityResponse
        MapToCapacityResponse(
            Property property)
        {
            return new PropertyCapacityResponse
            {
                Id =
                    property.Id,

                MaxGuests =
                    property.MaxGuests
                    ?? 0,

                Bedrooms =
                    property.Bedrooms
                    ?? 0,

                Beds =
                    property.Beds
                    ?? 0,

                Bathrooms =
                    property.Bathrooms
                    ?? 0,

                Status =
                    property.Status.ToString(),

                UpdatedAt =
                    property.UpdatedAt
            };
        }

        private static void ValidateCapacity(
            int maxGuests,
            int bedrooms,
            int beds,
            decimal bathrooms)
        {
            if (maxGuests is < 1 or > 20)
            {
                throw new ArgumentException(
                    "Maximum guests must be between 1 and 20.");
            }

            if (bedrooms is < 0 or > 20)
            {
                throw new ArgumentException(
                    "Bedrooms must be between 0 and 20.");
            }

            if (beds is < 1 or > 30)
            {
                throw new ArgumentException(
                    "Beds must be between 1 and 30.");
            }

            if (bathrooms is < 0.5m or > 20m)
            {
                throw new ArgumentException(
                    "Bathrooms must be between 0.5 and 20.");
            }
        }

        private static void ValidateCoordinates(
            decimal latitude,
            decimal longitude)
        {
            if (latitude is < -90 or > 90)
            {
                throw new ArgumentException(
                    "Latitude must be between -90 and 90.");
            }

            if (longitude is < -180 or > 180)
            {
                throw new ArgumentException(
                    "Longitude must be between -180 and 180.");
            }
        }

        private static void ValidateNormalizedLocation(
            string country,
            string city,
            string streetAddress,
            string? buildingNumber,
            string? floor,
            string? apartmentNumber,
            string? postalCode)
        {
            if (country.Length is 0 or > 100)
            {
                throw new ArgumentException(
                    "Country is required and cannot exceed 100 characters.");
            }

            if (city.Length is 0 or > 100)
            {
                throw new ArgumentException(
                    "City is required and cannot exceed 100 characters.");
            }

            if (streetAddress.Length is 0 or > 250)
            {
                throw new ArgumentException(
                    "Street address is required and cannot exceed 250 characters.");
            }

            if (buildingNumber?.Length > 30)
            {
                throw new ArgumentException(
                    "Building number cannot exceed 30 characters.");
            }

            if (floor?.Length > 30)
            {
                throw new ArgumentException(
                    "Floor cannot exceed 30 characters.");
            }

            if (apartmentNumber?.Length > 30)
            {
                throw new ArgumentException(
                    "Apartment number cannot exceed 30 characters.");
            }

            if (postalCode?.Length > 20)
            {
                throw new ArgumentException(
                    "Postal code cannot exceed 20 characters.");
            }
        }

        private static string? NormalizeOptionalString(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static void EnsurePropertyCanBeSubmitted(
            Property property)
        {
            var canBeSubmitted =
                property.Status ==
                    PropertyStatus.Draft
                ||
                property.Status ==
                    PropertyStatus.Rejected
                ||
                property.Status ==
                    PropertyStatus.Unpublished;

            if (!canBeSubmitted)
            {
                throw new PropertyNotEditableException(
                    property.Status.ToString());
            }
        }

        private static IReadOnlyList<string>
            GetPropertySubmissionValidationErrors(
                Property property)
        {
            var errors =
                new List<string>();

            ValidateSubmissionBasicInformation(
                property,
                errors);

            ValidateSubmissionLocation(
                property,
                errors);

            ValidateSubmissionCapacity(
                property,
                errors);

            ValidateSubmissionPricingAndPolicies(
                property,
                errors);

            ValidateSubmissionHouseRules(
                property,
                errors);

            ValidateSubmissionImages(
                property,
                errors);

            ValidateSubmissionVerificationDocument(
                property,
                errors);

            return errors;
        }

        private static void ValidateSubmissionBasicInformation(
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
                    "Basic information: title must contain between 10 and 120 characters.");
            }

            if (description.Length is < 100 or > 3000)
            {
                errors.Add(
                    "Basic information: description must contain between 100 and 3000 characters.");
            }

            if (!Enum.IsDefined(property.PropertyType))
            {
                errors.Add(
                    "Basic information: property type is invalid.");
            }

            if (!Enum.IsDefined(property.SpaceType))
            {
                errors.Add(
                    "Basic information: property space type is invalid.");
            }

            if (property.PropertyType ==
                    PropertyType.Studio
                &&
                property.SpaceType ==
                    PropertySpaceType.PrivateRoom)
            {
                errors.Add(
                    "Basic information: a studio cannot be listed as a private room.");
            }
        }

        private static void ValidateSubmissionLocation(
            Property property,
            ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(
                    property.Country))
            {
                errors.Add(
                    "Location: country is required.");
            }
            else if (property.Country.Trim().Length > 100)
            {
                errors.Add(
                    "Location: country cannot exceed 100 characters.");
            }

            if (string.IsNullOrWhiteSpace(
                    property.City))
            {
                errors.Add(
                    "Location: city is required.");
            }
            else if (property.City.Trim().Length > 100)
            {
                errors.Add(
                    "Location: city cannot exceed 100 characters.");
            }

            if (string.IsNullOrWhiteSpace(
                    property.StreetAddress))
            {
                errors.Add(
                    "Location: street address is required.");
            }
            else if (property.StreetAddress.Trim().Length > 250)
            {
                errors.Add(
                    "Location: street address cannot exceed 250 characters.");
            }

            if (!property.Latitude.HasValue)
            {
                errors.Add(
                    "Location: latitude is required.");
            }
            else if (property.Latitude.Value is < -90 or > 90)
            {
                errors.Add(
                    "Location: latitude must be between -90 and 90.");
            }

            if (!property.Longitude.HasValue)
            {
                errors.Add(
                    "Location: longitude is required.");
            }
            else if (property.Longitude.Value is < -180 or > 180)
            {
                errors.Add(
                    "Location: longitude must be between -180 and 180.");
            }
        }

        private static void ValidateSubmissionCapacity(
            Property property,
            ICollection<string> errors)
        {
            if (!property.MaxGuests.HasValue)
            {
                errors.Add(
                    "Capacity: maximum guests is required.");
            }
            else if (property.MaxGuests.Value is < 1 or > 20)
            {
                errors.Add(
                    "Capacity: maximum guests must be between 1 and 20.");
            }

            if (!property.Bedrooms.HasValue)
            {
                errors.Add(
                    "Capacity: bedrooms is required.");
            }
            else if (property.Bedrooms.Value is < 0 or > 20)
            {
                errors.Add(
                    "Capacity: bedrooms must be between 0 and 20.");
            }

            if (!property.Beds.HasValue)
            {
                errors.Add(
                    "Capacity: beds is required.");
            }
            else if (property.Beds.Value is < 1 or > 30)
            {
                errors.Add(
                    "Capacity: beds must be between 1 and 30.");
            }

            if (!property.Bathrooms.HasValue)
            {
                errors.Add(
                    "Capacity: bathrooms is required.");
            }
            else if (property.Bathrooms.Value is < 0.5m or > 20m)
            {
                errors.Add(
                    "Capacity: bathrooms must be between 0.5 and 20.");
            }
        }

        private static void ValidateSubmissionPricingAndPolicies(
            Property property,
            ICollection<string> errors)
        {
            if (!property.PricePerNight.HasValue)
            {
                errors.Add(
                    "Pricing and policies: price per night is required.");
            }
            else if (property.PricePerNight.Value <= 0)
            {
                errors.Add(
                    "Pricing and policies: price per night must be greater than zero.");
            }

            try
            {
                _ = CurrencyCodeNormalizer.NormalizeForStorage(
                    property.Currency);
            }
            catch (ArgumentException)
            {
                errors.Add(
                    "Pricing and policies: currency must be one of the supported currencies: EGP, USD, EUR.");
            }

            if (!property.CheckInTime.HasValue)
            {
                errors.Add(
                    "Pricing and policies: check-in time is required.");
            }

            if (!property.CheckOutTime.HasValue)
            {
                errors.Add(
                    "Pricing and policies: check-out time is required.");
            }

            if (property.CheckInTime.HasValue
                &&
                property.CheckOutTime.HasValue
                &&
                property.CheckInTime.Value ==
                    property.CheckOutTime.Value)
            {
                errors.Add(
                    "Pricing and policies: check-in and check-out times cannot be the same.");
            }

            if (!property.CancellationPolicy.HasValue)
            {
                errors.Add(
                    "Pricing and policies: cancellation policy is required.");
            }
            else if (!Enum.IsDefined(
                         property.CancellationPolicy.Value))
            {
                errors.Add(
                    "Pricing and policies: cancellation policy is invalid.");
            }
        }

        private static void ValidateSubmissionHouseRules(
            Property property,
            ICollection<string> errors)
        {
            if (!property.AllowsSmoking.HasValue)
            {
                errors.Add(
                    "House rules: smoking preference is required.");
            }

            if (!property.AllowsPets.HasValue)
            {
                errors.Add(
                    "House rules: pets preference is required.");
            }

            if (!property.AllowsParties.HasValue)
            {
                errors.Add(
                    "House rules: parties preference is required.");
            }

            if (!property.AllowsChildren.HasValue)
            {
                errors.Add(
                    "House rules: children preference is required.");
            }

            if (property.AdditionalHouseRules?.Length > 1000)
            {
                errors.Add(
                    "House rules: additional rules cannot exceed 1000 characters.");
            }
        }

        private static void ValidateSubmissionImages(
            Property property,
            ICollection<string> errors)
        {
            var images =
                property.Images.ToList();

            if (images.Count < 3)
            {
                errors.Add(
                    "Images: at least 3 property images are required.");
            }

            if (images.Count > MaximumPropertyImages)
            {
                errors.Add(
                    $"Images: a property cannot contain more than {MaximumPropertyImages} images.");
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
                    "Images: one or more image records are incomplete.");
            }

            var coverImages =
                images
                    .Where(image =>
                        image.IsCover)
                    .ToList();

            if (coverImages.Count != 1)
            {
                errors.Add(
                    "Images: exactly one cover image is required.");
            }
            else if (coverImages[0].DisplayOrder != 1)
            {
                errors.Add(
                    "Images: the cover image must have display order 1.");
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
                    .Range(1, images.Count)
                    .ToArray();

            if (!actualOrder.SequenceEqual(
                    expectedOrder))
            {
                errors.Add(
                    "Images: display order must be unique and contiguous starting from 1.");
            }
        }

        private static void
            ValidateSubmissionVerificationDocument(
                Property property,
                ICollection<string> errors)
        {
            var document =
                property.VerificationDocument;

            if (document is null)
            {
                errors.Add(
                    "Verification document: a property verification document is required.");

                return;
            }

            if (!Enum.IsDefined(
                    document.DocumentType))
            {
                errors.Add(
                    "Verification document: document type is invalid.");
            }

            var pages =
                document.Pages.ToList();

            if (pages.Count == 0)
            {
                errors.Add(
                    "Verification document: at least one page is required.");
            }

            if (pages.Count >
                MaximumVerificationDocumentPages)
            {
                errors.Add(
                    $"Verification document: no more than {MaximumVerificationDocumentPages} pages are allowed.");
            }

            if (pages.Any(page =>
                    string.IsNullOrWhiteSpace(
                        page.PublicId)
                    ||
                    string.IsNullOrWhiteSpace(
                        page.Format)))
            {
                errors.Add(
                    "Verification document: one or more page records are incomplete.");
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
                    .Range(1, pages.Count)
                    .ToArray();

            if (!actualPageNumbers.SequenceEqual(
                    expectedPageNumbers))
            {
                errors.Add(
                    "Verification document: page numbers must be unique and contiguous starting from 1.");
            }
        }

        private static PropertySubmissionResponse
            MapToSubmissionResponse(
                Property property)
        {
            return new PropertySubmissionResponse
            {
                Id =
                    property.Id,

                Status =
                    property.Status.ToString(),

                SubmittedAt =
                    property.SubmittedAt
                    ?? DateTimeOffset.UtcNow,

                UpdatedAt =
                    property.UpdatedAt,

                Message =
                    "The property was submitted successfully and is awaiting admin review."
            };
        }

        private static void EnsurePropertyIsEditable(
            Property property)
        {
            var isEditable =
                property.Status ==
                    PropertyStatus.Draft
                ||
                property.Status ==
                    PropertyStatus.Rejected
                ||
                property.Status ==
                    PropertyStatus.Unpublished;

            if (!isEditable)
            {
                throw new PropertyNotEditableException(
                    property.Status.ToString());
            }
        }

        private static void
            ValidateNormalizedBasicInformation(
                string title,
                string description)
        {
            if (title.Length is < 10 or > 120)
            {
                throw new ArgumentException(
                    "Property title must contain between 10 and 120 characters.");
            }

            if (description.Length is < 100 or > 3000)
            {
                throw new ArgumentException(
                    "Property description must contain between 100 and 3000 characters.");
            }
        }

        private static void
            ValidatePropertyTypeCombination(
                PropertyType propertyType,
                PropertySpaceType spaceType)
        {
            if (propertyType ==
                    PropertyType.Studio
                &&
                spaceType ==
                    PropertySpaceType.PrivateRoom)
            {
                throw new ArgumentException(
                    "A studio cannot be listed as a private room.");
            }
        }

        private static PropertyDraftResponse
            MapToDraftResponse(
                Property property)
        {
            return new PropertyDraftResponse
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

                Currency =
                    property.Currency,

                Status =
                    property.Status.ToString(),

                CreatedAt =
                    property.CreatedAt,

                UpdatedAt =
                    property.UpdatedAt
            };
        }
    }
}