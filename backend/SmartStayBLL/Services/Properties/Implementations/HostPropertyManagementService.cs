using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class HostPropertyManagementService
        : IHostPropertyManagementService
    {
        private const int MaximumPageSize =
            100;

        private readonly SmartStayDbContext
            _dbContext;

        public HostPropertyManagementService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext =
                dbContext;
        }

        public async Task<HostPropertiesResponse>
            GetPropertiesAsync(
                Guid userId,
                int page,
                int pageSize,
                PropertyStatus? status,
                CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(
                userId);

            ValidatePagination(
                page,
                pageSize);

            if (status.HasValue
                &&
                !Enum.IsDefined(
                    status.Value))
            {
                throw new ArgumentException(
                    "The selected property status is invalid.");
            }

            var query =
                _dbContext.Properties
                    .AsNoTracking()
                    .Where(property =>
                        property.HostProfile.UserId ==
                            userId);

            if (status.HasValue)
            {
                query =
                    query.Where(property =>
                        property.Status ==
                            status.Value);
            }

            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            /*
             * Keep enum values in their original type
             * during the SQL query, then convert them
             * to strings after loading the results.
             */
            var rawItems =
                await query
                    .OrderByDescending(property =>
                        property.UpdatedAt
                        ??
                        property.CreatedAt)
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
                            property.Status,
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

                            ImagesCount =
                                property.Images.Count(),

                            property.RejectionReason,
                            property.CreatedAt,
                            property.UpdatedAt,
                            property.SubmittedAt,
                            property.ReviewedAt,
                            property.PublishedAt
                        })
                    .ToListAsync(
                        cancellationToken);

            var items =
                rawItems
                    .Select(item =>
                        new HostPropertyListItemResponse
                        {
                            Id =
                                item.Id,

                            Title =
                                item.Title,

                            PropertyType =
                                item.PropertyType.ToString(),

                            SpaceType =
                                item.SpaceType.ToString(),

                            Status =
                                item.Status.ToString(),

                            City =
                                item.City,

                            PricePerNight =
                                item.PricePerNight,

                            Currency =
                                item.Currency,

                            CoverImageUrl =
                                item.CoverImageUrl,

                            ImagesCount =
                                item.ImagesCount,

                            CanEdit =
                                IsEditableStatus(
                                    item.Status),

                            CanUnpublish =
                                item.Status ==
                                    PropertyStatus.Published,

                            RejectionReason =
                                item.RejectionReason,

                            CreatedAt =
                                item.CreatedAt,

                            UpdatedAt =
                                item.UpdatedAt,

                            SubmittedAt =
                                item.SubmittedAt,

                            ReviewedAt =
                                item.ReviewedAt,

                            PublishedAt =
                                item.PublishedAt
                        })
                    .ToList();

            var totalPages =
                totalCount == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalCount /
                        (double)pageSize);

            return new HostPropertiesResponse
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
                    totalPages,

                AppliedStatusFilter =
                    status?.ToString()
            };
        }

        public async Task<
            HostPropertyStatusSummaryResponse>
            GetSummaryAsync(
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(
                userId);

            var statusCounts =
                await _dbContext.Properties
                    .AsNoTracking()
                    .Where(property =>
                        property.HostProfile.UserId ==
                            userId)
                    .GroupBy(property =>
                        property.Status)
                    .Select(group =>
                        new
                        {
                            Status =
                                group.Key,

                            Count =
                                group.Count()
                        })
                    .ToListAsync(
                        cancellationToken);

            var countsByStatus =
                statusCounts.ToDictionary(
                    item => item.Status,
                    item => item.Count);

            var draftCount =
                GetStatusCount(
                    countsByStatus,
                    PropertyStatus.Draft);

            var pendingCount =
                GetStatusCount(
                    countsByStatus,
                    PropertyStatus.Pending);

            var publishedCount =
                GetStatusCount(
                    countsByStatus,
                    PropertyStatus.Published);

            var rejectedCount =
                GetStatusCount(
                    countsByStatus,
                    PropertyStatus.Rejected);

            var unpublishedCount =
                GetStatusCount(
                    countsByStatus,
                    PropertyStatus.Unpublished);

            return new HostPropertyStatusSummaryResponse
            {
                TotalProperties =
                    draftCount
                    +
                    pendingCount
                    +
                    publishedCount
                    +
                    rejectedCount
                    +
                    unpublishedCount,

                DraftProperties =
                    draftCount,

                PendingProperties =
                    pendingCount,

                PublishedProperties =
                    publishedCount,

                RejectedProperties =
                    rejectedCount,

                UnpublishedProperties =
                    unpublishedCount
            };
        }

        public async Task<HostPropertyUnpublishResponse>
            UnpublishAsync(
                Guid userId,
                Guid propertyId,
                CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(
                userId);

            if (propertyId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The property identifier is invalid.");
            }

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
                /*
                 * Returning the same response for a missing
                 * property and a property owned by another
                 * host prevents ownership disclosure.
                 */
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

            if (property.Status !=
                PropertyStatus.Published)
            {
                throw new InvalidOperationException(
                    "Only published properties can be unpublished. " +
                    $"The current property status is " +
                    $"'{property.Status}'.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            property.Status =
                PropertyStatus.Unpublished;

            property.UpdatedAt =
                currentTime;

            /*
             * PublishedAt remains unchanged so that
             * the previous publication timestamp is
             * preserved for audit and display purposes.
             */

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new HostPropertyUnpublishResponse
            {
                Id =
                    property.Id,

                Status =
                    property.Status.ToString(),

                PublishedAt =
                    property.PublishedAt,

                UpdatedAt =
                    currentTime,

                Message =
                    "The property was unpublished successfully."
            };
        }

        private static void ValidateUserIdentifier(
            Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "The access token does not contain a valid user identifier.");
            }
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

            if (pageSize is < 1 or > MaximumPageSize)
            {
                throw new ArgumentException(
                    $"The page size must be between 1 and {MaximumPageSize}.");
            }
        }

        private static bool IsEditableStatus(
            PropertyStatus status)
        {
            return status ==
                       PropertyStatus.Draft
                   ||
                   status ==
                       PropertyStatus.Rejected
                   ||
                   status ==
                       PropertyStatus.Unpublished;
        }

        private static int GetStatusCount(
            IReadOnlyDictionary<PropertyStatus, int>
                statusCounts,
            PropertyStatus status)
        {
            return statusCounts.TryGetValue(
                status,
                out var count)
                    ? count
                    : 0;
        }
    }
}