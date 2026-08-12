using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class PublicPropertyService
        : IPublicPropertyService
    {
        private const int MaximumPageSize =
            100;

        private readonly SmartStayDbContext
            _dbContext;

        public PublicPropertyService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext =
                dbContext;
        }

        public async Task<PublicPropertiesResponse>
            SearchAsync(
                PublicPropertySearchRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            ValidateSearchRequest(
                request);

            var normalizedSearch =
                NormalizeOptionalString(
                    request.Search);

            var normalizedCity =
                NormalizeOptionalString(
                    request.City);

            /*
             * Public search returns only:
             *
             * 1. Published properties.
             * 2. Properties owned by approved hosts.
             * 3. Properties owned by active users.
             */
            var query =
                _dbContext.Properties
                    .AsNoTracking()
                    .Where(property =>
                        property.Status ==
                            PropertyStatus.Published
                        &&
                        property.HostProfile.Status ==
                            HostApplicationStatus.Approved
                        &&
                        property.HostProfile.User.IsActive);

            /*
             * General search:
             *
             * Title
             * City
             * Country
             */
            if (normalizedSearch is not null)
            {
                query =
                    query.Where(property =>
                        property.Title.Contains(
                            normalizedSearch)
                        ||
                        (
                            property.City != null
                            &&
                            property.City.Contains(
                                normalizedSearch)
                        )
                        ||
                        (
                            property.Country != null
                            &&
                            property.Country.Contains(
                                normalizedSearch)
                        ));
            }

            /*
             * Destination filter.
             */
            if (normalizedCity is not null)
            {
                query =
                    query.Where(property =>
                        property.City != null
                        &&
                        property.City.Contains(
                            normalizedCity));
            }

            /*
             * Property type filter:
             *
             * Apartment
             * House
             * Villa
             * Studio
             * Chalet
             */
            if (request.PropertyType.HasValue)
            {
                query =
                    query.Where(property =>
                        property.PropertyType ==
                            request.PropertyType.Value);
            }

            /*
             * Space type filter:
             *
             * EntirePlace
             * PrivateRoom
             */
            if (request.SpaceType.HasValue)
            {
                query =
                    query.Where(property =>
                        property.SpaceType ==
                            request.SpaceType.Value);
            }

            /*
             * Price filters.
             */
            if (request.MinPrice.HasValue)
            {
                query =
                    query.Where(property =>
                        property.PricePerNight.HasValue
                        &&
                        property.PricePerNight.Value >=
                            request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                query =
                    query.Where(property =>
                        property.PricePerNight.HasValue
                        &&
                        property.PricePerNight.Value <=
                            request.MaxPrice.Value);
            }

            /*
             * Guest capacity filter.
             *
             * minGuests=4 means:
             *
             * Return properties that can accommodate
             * at least four guests.
             */
            if (request.MinGuests.HasValue)
            {
                query =
                    query.Where(property =>
                        property.MaxGuests.HasValue
                        &&
                        property.MaxGuests.Value >=
                            request.MinGuests.Value);
            }

            /*
             * Availability filter.
             *
             * Both dates have already been validated inside
             * ValidateSearchRequest().
             *
             * A property is excluded when it has an
             * overlapping booking that is:
             *
             * 1. Confirmed.
             *
             * OR
             *
             * 2. Pending and its payment window has not
             *    expired yet.
             *
             * A stale Pending booking does not block the
             * property, even if the background lifecycle
             * process has not changed its status to Expired yet.
             */
            if (request.CheckInDate.HasValue
                &&
                request.CheckOutDate.HasValue)
            {
                var requestedCheckInDate =
                    request.CheckInDate.Value;

                var requestedCheckOutDate =
                    request.CheckOutDate.Value;

                /*
                 * Capture the current time once so the same
                 * value is used throughout this database query.
                 */
                var currentTime =
                    DateTimeOffset.UtcNow;

                query =
                    query.Where(property =>
                        !_dbContext.Bookings.Any(booking =>
                            booking.PropertyId ==
                                property.Id
                            &&
                            (
                                booking.Status ==
                                    BookingStatus.Confirmed
                                ||
                                (
                                    booking.Status ==
                                        BookingStatus.Pending
                                    &&
                                    booking.ExpiresAt.HasValue
                                    &&
                                    booking.ExpiresAt.Value >
                                        currentTime
                                )
                            )
                            &&
                            booking.CheckInDate <
                                requestedCheckOutDate
                            &&
                            booking.CheckOutDate >
                                requestedCheckInDate));
            }

            /*
             * TotalCount must be calculated after applying
             * all filters, including date availability.
             *
             * This ensures pagination is correct.
             */
            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            query =
                request.Sort switch
                {
                    PublicPropertySortOption
                        .PriceLowToHigh =>
                        query
                            .OrderBy(property =>
                                property.PricePerNight)
                            .ThenByDescending(property =>
                                property.PublishedAt),

                    PublicPropertySortOption
                        .PriceHighToLow =>
                        query
                            .OrderByDescending(property =>
                                property.PricePerNight)
                            .ThenByDescending(property =>
                                property.PublishedAt),

                    _ =>
                        query
                            .OrderByDescending(property =>
                                property.PublishedAt)
                            .ThenByDescending(property =>
                                property.CreatedAt)
                };

            var rawItems =
                await query
                    .Skip(
                        (request.Page - 1)
                        *
                        request.PageSize)
                    .Take(
                        request.PageSize)
                    .Select(property =>
                        new
                        {
                            property.Id,
                            property.Title,
                            property.PropertyType,
                            property.SpaceType,
                            property.Country,
                            property.City,
                            property.PricePerNight,
                            property.Currency,
                            property.MaxGuests,
                            property.Bedrooms,
                            property.Beds,
                            property.Bathrooms,
                            property.PublishedAt,

                            CoverImageUrl =
                                property.Images
                                    .OrderByDescending(image =>
                                        image.IsCover)
                                    .ThenBy(image =>
                                        image.DisplayOrder)
                                    .Select(image =>
                                        image.Url)
                                    .FirstOrDefault()
                        })
                    .ToListAsync(
                        cancellationToken);

            var items =
                rawItems
                    .Select(item =>
                        new PublicPropertyListItemResponse
                        {
                            Id =
                                item.Id,

                            Title =
                                item.Title,

                            PropertyType =
                                item.PropertyType.ToString(),

                            SpaceType =
                                item.SpaceType.ToString(),

                            Country =
                                item.Country
                                ?? string.Empty,

                            City =
                                item.City
                                ?? string.Empty,

                            PricePerNight =
                                item.PricePerNight
                                ?? 0,

                            Currency =
                                item.Currency,

                            CoverImageUrl =
                                item.CoverImageUrl
                                ?? string.Empty,

                            MaxGuests =
                                item.MaxGuests
                                ?? 0,

                            Bedrooms =
                                item.Bedrooms
                                ?? 0,

                            Beds =
                                item.Beds
                                ?? 0,

                            Bathrooms =
                                item.Bathrooms
                                ?? 0,

                            PublishedAt =
                                item.PublishedAt
                        })
                    .ToList();

            var totalPages =
                totalCount == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalCount /
                        (double)request.PageSize);

            return new PublicPropertiesResponse
            {
                Items =
                    items,

                Page =
                    request.Page,

                PageSize =
                    request.PageSize,

                TotalCount =
                    totalCount,

                TotalPages =
                    totalPages
            };
        }

        public async Task<PublicPropertyDetailsResponse>
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
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.Status ==
                                PropertyStatus.Published
                            &&
                            property.HostProfile.Status ==
                                HostApplicationStatus.Approved
                            &&
                            property.HostProfile.User.IsActive,
                        cancellationToken);

            if (property is null)
            {
                /*
                 * Draft, Pending, Rejected and Unpublished
                 * properties return the same 404 response as
                 * properties that do not exist.
                 */
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            var ratingData =
                await _dbContext.Reviews
                    .AsNoTracking()
                    .Where(review =>
                        review.PropertyId == property.Id
                        &&
                        review.Status ==
                            ReviewStatus.Posted)
                    .GroupBy(review =>
                        review.PropertyId)
                    .Select(group =>
                        new
                        {
                            ReviewsCount =
                                group.Count(),

                            RatingTotal =
                                group.Sum(review =>
                                    review.Rating)
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            var reviewsCount =
                ratingData?.ReviewsCount
                ?? 0;

            var averageRating =
                reviewsCount == 0
                    ? 0
                    : Math.Round(
                        ratingData!.RatingTotal
                        /
                        (decimal)reviewsCount,
                        2,
                        MidpointRounding
                            .AwayFromZero);

            return MapToDetailsResponse(
                property,
                averageRating,
                reviewsCount);
        }

        private static PublicPropertyDetailsResponse
            MapToDetailsResponse(
                Property property,
                decimal averageRating,
                int reviewsCount)
        {
            var user =
                property.HostProfile.User;

            var firstName =
                user.FirstName
                ?? string.Empty;

            var lastName =
                user.LastName
                ?? string.Empty;

            var images =
                property.Images
                    .OrderBy(image =>
                        image.DisplayOrder)
                    .ThenBy(image =>
                        image.CreatedAt)
                    .Select(image =>
                        new PublicPropertyImageResponse
                        {
                            Id =
                                image.Id,

                            Url =
                                image.Url,

                            IsCover =
                                image.IsCover,

                            DisplayOrder =
                                image.DisplayOrder
                        })
                    .ToList();

            var amenities =
                property.PropertyAmenities
                    .Where(propertyAmenity =>
                        propertyAmenity.Amenity is not null
                        &&
                        propertyAmenity.Amenity.IsActive)
                    .OrderBy(propertyAmenity =>
                        propertyAmenity.Amenity.Category)
                    .ThenBy(propertyAmenity =>
                        propertyAmenity.Amenity.DisplayOrder)
                    .ThenBy(propertyAmenity =>
                        propertyAmenity.Amenity.Name)
                    .Select(propertyAmenity =>
                        new PublicPropertyAmenityResponse
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

            return new PublicPropertyDetailsResponse
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

                Country =
                    property.Country
                    ?? string.Empty,

                City =
                    property.City
                    ?? string.Empty,

                StreetAddress =
                    property.StreetAddress
                    ?? string.Empty,

                PostalCode =
                    property.PostalCode,

                Latitude =
                    property.Latitude,

                Longitude =
                    property.Longitude,

                FullAddress =
                    BuildPublicAddress(
                        property),

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

                PricePerNight =
                    property.PricePerNight
                    ?? 0,

                Currency =
                    property.Currency,

                AverageRating =
                    averageRating,

                ReviewsCount =
                    reviewsCount,

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

                Host =
                    new PublicPropertyHostResponse
                    {
                        UserId =
                            property.HostProfile.UserId,

                        FirstName =
                            firstName,

                        FullName =
                            BuildFullName(
                                firstName,
                                lastName),

                        DisplayName =
                            property.HostProfile.DisplayName,

                        Bio =
                            property.HostProfile.Bio,

                        Country =
                            property.HostProfile.Country,

                        City =
                            property.HostProfile.City,

                        ProfileImageUrl =
                            property.HostProfile.ProfileImageUrl
                    },

                Images =
                    images,

                Amenities =
                    amenities,

                PublishedAt =
                    property.PublishedAt
            };
        }

        private static void ValidateSearchRequest(
            PublicPropertySearchRequest request)
        {
            if (request.Page < 1)
            {
                throw new ArgumentException(
                    "The page number must be greater than or equal to 1.");
            }

            if (request.PageSize is
                < 1 or > MaximumPageSize)
            {
                throw new ArgumentException(
                    $"The page size must be between 1 and " +
                    $"{MaximumPageSize}.");
            }

            if (request.PropertyType.HasValue
                &&
                !Enum.IsDefined(
                    request.PropertyType.Value))
            {
                throw new ArgumentException(
                    "The selected property type is invalid.");
            }

            if (request.SpaceType.HasValue
                &&
                !Enum.IsDefined(
                    request.SpaceType.Value))
            {
                throw new ArgumentException(
                    "The selected property space type is invalid.");
            }

            if (!Enum.IsDefined(
                    request.Sort))
            {
                throw new ArgumentException(
                    "The selected sort option is invalid.");
            }

            if (request.MinPrice.HasValue
                &&
                request.MinPrice.Value < 0)
            {
                throw new ArgumentException(
                    "The minimum price cannot be negative.");
            }

            if (request.MaxPrice.HasValue
                &&
                request.MaxPrice.Value <= 0)
            {
                throw new ArgumentException(
                    "The maximum price must be greater than zero.");
            }

            if (request.MinPrice.HasValue
                &&
                request.MaxPrice.HasValue
                &&
                request.MinPrice.Value >
                    request.MaxPrice.Value)
            {
                throw new ArgumentException(
                    "The minimum price cannot be greater than the maximum price.");
            }

            if (request.MinGuests.HasValue
                &&
                request.MinGuests.Value is < 1 or > 20)
            {
                throw new ArgumentException(
                    "Minimum guests must be between 1 and 20.");
            }

            ValidateAvailabilityDates(
                request.CheckInDate,
                request.CheckOutDate);
        }

        private static void ValidateAvailabilityDates(
            DateOnly? checkInDate,
            DateOnly? checkOutDate)
        {
            /*
             * No dates means the user is browsing properties
             * without availability filtering.
             */
            if (!checkInDate.HasValue
                &&
                !checkOutDate.HasValue)
            {
                return;
            }

            /*
             * If one date is provided, both dates
             * must be provided.
             */
            if (!checkInDate.HasValue)
            {
                throw new ArgumentException(
                    "The check-in date is required when a check-out date is provided.");
            }

            if (!checkOutDate.HasValue)
            {
                throw new ArgumentException(
                    "The check-out date is required when a check-in date is provided.");
            }

            var today =
                DateOnly.FromDateTime(
                    DateTime.UtcNow);

            if (checkInDate.Value < today)
            {
                throw new ArgumentException(
                    "The check-in date cannot be in the past.");
            }

            if (checkOutDate.Value <=
                checkInDate.Value)
            {
                throw new ArgumentException(
                    "The check-out date must be after the check-in date.");
            }
        }

        private static string? NormalizeOptionalString(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return null;
            }

            return value.Trim();
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
                .Where(part =>
                    !string.IsNullOrWhiteSpace(
                        part)));
        }

        private static string BuildPublicAddress(
            Property property)
        {
            return string.Join(
                ", ",
                new[]
                {
                    property.StreetAddress,
                    property.City,
                    property.PostalCode,
                    property.Country
                }
                .Where(part =>
                    !string.IsNullOrWhiteSpace(
                        part))
                .Select(part =>
                    part!.Trim()));
        }
    }
}