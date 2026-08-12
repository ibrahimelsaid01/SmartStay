using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class AdminVerificationQueueService
        : IAdminVerificationQueueService
    {
        private const int MaximumPageSize =
            100;

        private const int HighPriorityAfterHours =
            48;

        private readonly SmartStayDbContext _dbContext;

        public AdminVerificationQueueService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext =
                dbContext;
        }

        // =====================================================
        // Queue
        // =====================================================

        public async Task<AdminVerificationQueueResponse> GetQueueAsync(
            AdminVerificationQueueRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            var page =
                NormalizePage(
                    request.Page);

            var pageSize =
                NormalizePageSize(
                    request.PageSize);

            var normalizedType =
                NormalizeQueueType(
                    request.Type);

            var currentTime =
                DateTimeOffset.UtcNow;

            var highPriorityThreshold =
                currentTime.AddHours(
                    -HighPriorityAfterHours);

            var summary =
                await GetSummaryAsync(
                    highPriorityThreshold,
                    currentTime,
                    cancellationToken);

            var items =
                new List<AdminVerificationQueueItemResponse>();

            if (normalizedType is "all" or "host")
            {
                var hostItems =
                    await GetPendingHostApplicationItemsAsync(
                        request.Search,
                        highPriorityThreshold,
                        cancellationToken);

                items.AddRange(
                    hostItems);
            }

            if (normalizedType is "all" or "property")
            {
                var propertyItems =
                    await GetPendingPropertyVerificationItemsAsync(
                        request.Search,
                        highPriorityThreshold,
                        cancellationToken);

                items.AddRange(
                    propertyItems);
            }

            var orderedItems =
                items
                    .OrderByDescending(
                        item =>
                            item.IsHighPriority)
                    .ThenByDescending(
                        item =>
                            item.SubmittedAt
                            ??
                            item.CreatedAt)
                    .ThenBy(
                        item =>
                            item.Title,
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            var totalCount =
                orderedItems.Count;

            var pagedItems =
                orderedItems
                    .Skip(
                        (page - 1) * pageSize)
                    .Take(
                        pageSize)
                    .ToList();

            return new AdminVerificationQueueResponse
            {
                GeneratedAt =
                    currentTime,

                Type =
                    normalizedType,

                Page =
                    page,

                PageSize =
                    pageSize,

                TotalCount =
                    totalCount,

                TotalPages =
                    CalculateTotalPages(
                        totalCount,
                        pageSize),

                Summary =
                    summary,

                Items =
                    pagedItems
            };
        }

        private async Task<AdminVerificationQueueSummaryResponse>
            GetSummaryAsync(
                DateTimeOffset highPriorityThreshold,
                DateTimeOffset currentTime,
                CancellationToken cancellationToken)
        {
            var startOfToday =
                new DateTimeOffset(
                    currentTime.UtcDateTime.Date,
                    TimeSpan.Zero);

            var startOfTomorrow =
                startOfToday.AddDays(
                    1);

            var pendingHostApplications =
                await _dbContext.HostProfiles
                    .AsNoTracking()
                    .CountAsync(
                        host =>
                            host.Status ==
                            HostApplicationStatus.Pending,
                        cancellationToken);

            var pendingPropertyVerifications =
                await _dbContext.Properties
                    .AsNoTracking()
                    .CountAsync(
                        property =>
                            property.Status ==
                            PropertyStatus.Pending,
                        cancellationToken);

            var highPriorityHostApplications =
                await _dbContext.HostProfiles
                    .AsNoTracking()
                    .CountAsync(
                        host =>
                            host.Status ==
                                HostApplicationStatus.Pending
                            &&
                            (
                                host.SubmittedAt.HasValue
                                    ? host.SubmittedAt.Value
                                    : host.CreatedAt
                            ) <= highPriorityThreshold,
                        cancellationToken);

            var highPriorityPropertyVerifications =
                await _dbContext.Properties
                    .AsNoTracking()
                    .CountAsync(
                        property =>
                            property.Status ==
                                PropertyStatus.Pending
                            &&
                            (
                                property.SubmittedAt.HasValue
                                    ? property.SubmittedAt.Value
                                    : property.CreatedAt
                            ) <= highPriorityThreshold,
                        cancellationToken);

            var reviewedHostApplicationsToday =
                await _dbContext.HostProfiles
                    .AsNoTracking()
                    .CountAsync(
                        host =>
                            host.ReviewedAt.HasValue
                            &&
                            host.ReviewedAt.Value >= startOfToday
                            &&
                            host.ReviewedAt.Value < startOfTomorrow,
                        cancellationToken);

            var reviewedPropertiesToday =
                await _dbContext.Properties
                    .AsNoTracking()
                    .CountAsync(
                        property =>
                            property.ReviewedAt.HasValue
                            &&
                            property.ReviewedAt.Value >= startOfToday
                            &&
                            property.ReviewedAt.Value < startOfTomorrow,
                        cancellationToken);

            return new AdminVerificationQueueSummaryResponse
            {
                PendingHostApplications =
                    pendingHostApplications,

                PendingPropertyVerifications =
                    pendingPropertyVerifications,

                TotalPending =
                    pendingHostApplications
                    +
                    pendingPropertyVerifications,

                HighPriority =
                    highPriorityHostApplications
                    +
                    highPriorityPropertyVerifications,

                ReviewedToday =
                    reviewedHostApplicationsToday
                    +
                    reviewedPropertiesToday
            };
        }

        private async Task<IReadOnlyList<AdminVerificationQueueItemResponse>>
            GetPendingHostApplicationItemsAsync(
                string? search,
                DateTimeOffset highPriorityThreshold,
                CancellationToken cancellationToken)
        {
            var query =
                _dbContext.HostProfiles
                    .AsNoTracking()
                    .Where(
                        host =>
                            host.Status ==
                            HostApplicationStatus.Pending);

            query =
                ApplyHostSearchFilter(
                    query,
                    search);

            var rawItems =
                await query
                    .Select(
                        host =>
                            new
                            {
                                host.Id,
                                host.DisplayName,
                                host.Country,
                                host.City,
                                host.ProfileImageUrl,
                                host.CreatedAt,
                                host.SubmittedAt,
                                host.Status,

                                UserFirstName =
                                    host.User.FirstName,

                                UserLastName =
                                    host.User.LastName,

                                UserEmail =
                                    host.User.Email,

                                UserPhoneNumber =
                                    host.User.PhoneNumber,

                                HasIdentityDocument =
                                    host.IdentityDocument != null
                            })
                    .ToListAsync(
                        cancellationToken);

            return rawItems
                .Select(
                    host =>
                    {
                        var submittedOrCreated =
                            host.SubmittedAt
                            ??
                            host.CreatedAt;

                        return new AdminVerificationQueueItemResponse
                        {
                            VerificationId =
                                host.Id,

                            VerificationType =
                                "HostApplication",

                            ReferenceCode =
                                BuildReferenceCode(
                                    "HA",
                                    host.Id),

                            Title =
                                host.DisplayName,

                            Subtitle =
                                "Host application verification",

                            ApplicantName =
                                BuildFullName(
                                    host.UserFirstName,
                                    host.UserLastName,
                                    host.UserEmail),

                            ApplicantEmail =
                                host.UserEmail
                                ??
                                string.Empty,

                            ApplicantPhoneNumber =
                                host.UserPhoneNumber,

                            ApplicantImageUrl =
                                host.ProfileImageUrl,

                            Location =
                                BuildLocation(
                                    host.City,
                                    host.Country),

                            Status =
                                host.Status.ToString(),

                            IsHighPriority =
                                submittedOrCreated <=
                                highPriorityThreshold,

                            DocumentsCount =
                                host.HasIdentityDocument
                                    ? 1
                                    : 0,

                            MissingDocumentsCount =
                                host.HasIdentityDocument
                                    ? 0
                                    : 1,

                            HasRequiredDocuments =
                                host.HasIdentityDocument,

                            CreatedAt =
                                host.CreatedAt,

                            SubmittedAt =
                                host.SubmittedAt,

                            DetailsEndpoint =
                                $"/api/admin/host-applications/{host.Id}",

                            ApproveEndpoint =
                                $"/api/admin/host-applications/{host.Id}/approve",

                            RejectEndpoint =
                                $"/api/admin/host-applications/{host.Id}/reject",

                            HistoryEndpoint =
                                $"/api/admin/verifications/host/{host.Id}/history"
                        };
                    })
                .ToList();
        }

        private async Task<IReadOnlyList<AdminVerificationQueueItemResponse>>
            GetPendingPropertyVerificationItemsAsync(
                string? search,
                DateTimeOffset highPriorityThreshold,
                CancellationToken cancellationToken)
        {
            var query =
                _dbContext.Properties
                    .AsNoTracking()
                    .Where(
                        property =>
                            property.Status ==
                            PropertyStatus.Pending);

            query =
                ApplyPropertySearchFilter(
                    query,
                    search);

            var rawItems =
                await query
                    .Select(
                        property =>
                            new
                            {
                                property.Id,
                                property.Title,
                                property.PropertyType,
                                property.SpaceType,
                                property.City,
                                property.Country,
                                property.CreatedAt,
                                property.SubmittedAt,
                                property.Status,

                                CoverImageUrl =
                                    property.Images
                                        .Where(
                                            image =>
                                                image.IsCover)
                                        .OrderBy(
                                            image =>
                                                image.DisplayOrder)
                                        .Select(
                                            image =>
                                                image.Url)
                                        .FirstOrDefault(),

                                HostFirstName =
                                    property.HostProfile.User.FirstName,

                                HostLastName =
                                    property.HostProfile.User.LastName,

                                HostEmail =
                                    property.HostProfile.User.Email,

                                HostPhoneNumber =
                                    property.HostProfile.User.PhoneNumber,

                                HostProfileImageUrl =
                                    property.HostProfile.ProfileImageUrl,

                                HasVerificationDocument =
                                    property.VerificationDocument != null,

                                VerificationDocumentPagesCount =
                                    property.VerificationDocument == null
                                        ? 0
                                        : property.VerificationDocument.Pages.Count
                            })
                    .ToListAsync(
                        cancellationToken);

            return rawItems
                .Select(
                    property =>
                    {
                        var submittedOrCreated =
                            property.SubmittedAt
                            ??
                            property.CreatedAt;

                        var hasRequiredDocument =
                            property.HasVerificationDocument
                            &&
                            property.VerificationDocumentPagesCount > 0;

                        return new AdminVerificationQueueItemResponse
                        {
                            VerificationId =
                                property.Id,

                            VerificationType =
                                "Property",

                            ReferenceCode =
                                BuildReferenceCode(
                                    "PV",
                                    property.Id),

                            Title =
                                property.Title,

                            Subtitle =
                                $"{property.PropertyType} / {property.SpaceType}",

                            ApplicantName =
                                BuildFullName(
                                    property.HostFirstName,
                                    property.HostLastName,
                                    property.HostEmail),

                            ApplicantEmail =
                                property.HostEmail
                                ??
                                string.Empty,

                            ApplicantPhoneNumber =
                                property.HostPhoneNumber,

                            ApplicantImageUrl =
                                property.HostProfileImageUrl
                                ??
                                property.CoverImageUrl,

                            Location =
                                BuildLocation(
                                    property.City,
                                    property.Country),

                            Status =
                                property.Status.ToString(),

                            IsHighPriority =
                                submittedOrCreated <=
                                highPriorityThreshold,

                            DocumentsCount =
                                property.VerificationDocumentPagesCount,

                            MissingDocumentsCount =
                                hasRequiredDocument
                                    ? 0
                                    : 1,

                            HasRequiredDocuments =
                                hasRequiredDocument,

                            CreatedAt =
                                property.CreatedAt,

                            SubmittedAt =
                                property.SubmittedAt,

                            DetailsEndpoint =
                                $"/api/admin/properties/{property.Id}",

                            ApproveEndpoint =
                                $"/api/admin/properties/{property.Id}/approve",

                            RejectEndpoint =
                                $"/api/admin/properties/{property.Id}/reject",

                            HistoryEndpoint =
                                $"/api/admin/verifications/property/{property.Id}/history"
                        };
                    })
                .ToList();
        }

        // =====================================================
        // History
        // =====================================================

        public async Task<AdminVerificationHistoryResponse> GetHistoryAsync(
            string verificationType,
            Guid verificationId,
            CancellationToken cancellationToken = default)
        {
            ValidateVerificationIdentifier(
                verificationId);

            var normalizedType =
                NormalizeHistoryType(
                    verificationType);

            return normalizedType switch
            {
                "host" =>
                    await GetHostApplicationHistoryAsync(
                        verificationId,
                        cancellationToken),

                "property" =>
                    await GetPropertyVerificationHistoryAsync(
                        verificationId,
                        cancellationToken),

                _ =>
                    throw new ArgumentException(
                        "The verification type is invalid.")
            };
        }

        private async Task<AdminVerificationHistoryResponse>
            GetHostApplicationHistoryAsync(
                Guid hostProfileId,
                CancellationToken cancellationToken)
        {
            var host =
                await _dbContext.HostProfiles
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == hostProfileId,
                        cancellationToken);

            if (host is null)
            {
                throw new KeyNotFoundException(
                    "The host application was not found.");
            }

            var items =
                new List<AdminVerificationHistoryItemResponse>
                {
                    new()
                    {
                        OccurredAt =
                            host.CreatedAt,

                        Title =
                            "Host application created",

                        Description =
                            "The host started a new application draft.",

                        ActorType =
                            "Host",

                        EventType =
                            "Created",

                        IsImportant =
                            false
                    }
                };

            if (host.SubmittedAt.HasValue)
            {
                items.Add(
                    new AdminVerificationHistoryItemResponse
                    {
                        OccurredAt =
                            host.SubmittedAt.Value,

                        Title =
                            "Host application submitted",

                        Description =
                            "The host submitted the application for admin review.",

                        ActorType =
                            "Host",

                        EventType =
                            "Submitted",

                        IsImportant =
                            true
                    });
            }

            AddWaitingFlagIfNeeded(
                items,
                host.Status == HostApplicationStatus.Pending,
                host.SubmittedAt ?? host.CreatedAt,
                "Host application is waiting for review");

            if (host.ReviewedAt.HasValue)
            {
                if (host.Status == HostApplicationStatus.Approved)
                {
                    items.Add(
                        new AdminVerificationHistoryItemResponse
                        {
                            OccurredAt =
                                host.ReviewedAt.Value,

                            Title =
                                "Host application approved",

                            Description =
                                "The admin approved the host application.",

                            ActorType =
                                "Admin",

                            EventType =
                                "Approved",

                            IsImportant =
                                true
                        });
                }
                else if (host.Status == HostApplicationStatus.Rejected)
                {
                    items.Add(
                        new AdminVerificationHistoryItemResponse
                        {
                            OccurredAt =
                                host.ReviewedAt.Value,

                            Title =
                                "Host application rejected",

                            Description =
                                string.IsNullOrWhiteSpace(
                                    host.RejectionReason)
                                    ? "The admin rejected the host application."
                                    : $"The admin rejected the host application. Reason: {host.RejectionReason}",

                            ActorType =
                                "Admin",

                            EventType =
                                "Rejected",

                            IsImportant =
                                true
                        });
                }
            }

            return new AdminVerificationHistoryResponse
            {
                VerificationId =
                    host.Id,

                VerificationType =
                    "HostApplication",

                ReferenceCode =
                    BuildReferenceCode(
                        "HA",
                        host.Id),

                Items =
                    items
                        .OrderByDescending(
                            item =>
                                item.OccurredAt)
                        .ToList()
            };
        }

        private async Task<AdminVerificationHistoryResponse>
            GetPropertyVerificationHistoryAsync(
                Guid propertyId,
                CancellationToken cancellationToken)
        {
            var property =
                await _dbContext.Properties
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == propertyId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property verification request was not found.");
            }

            var items =
                new List<AdminVerificationHistoryItemResponse>
                {
                    new()
                    {
                        OccurredAt =
                            property.CreatedAt,

                        Title =
                            "Property draft created",

                        Description =
                            "The host created a new property listing draft.",

                        ActorType =
                            "Host",

                        EventType =
                            "Created",

                        IsImportant =
                            false
                    }
                };

            if (property.SubmittedAt.HasValue)
            {
                items.Add(
                    new AdminVerificationHistoryItemResponse
                    {
                        OccurredAt =
                            property.SubmittedAt.Value,

                        Title =
                            "Property submitted for verification",

                        Description =
                            "The host submitted the property listing for admin verification.",

                        ActorType =
                            "Host",

                        EventType =
                            "Submitted",

                        IsImportant =
                            true
                    });
            }

            AddWaitingFlagIfNeeded(
                items,
                property.Status == PropertyStatus.Pending,
                property.SubmittedAt ?? property.CreatedAt,
                "Property verification is waiting for review");

            if (property.ReviewedAt.HasValue)
            {
                if (property.Status == PropertyStatus.Published)
                {
                    items.Add(
                        new AdminVerificationHistoryItemResponse
                        {
                            OccurredAt =
                                property.ReviewedAt.Value,

                            Title =
                                "Property verification approved",

                            Description =
                                "The admin approved and published the property listing.",

                            ActorType =
                                "Admin",

                            EventType =
                                "Approved",

                            IsImportant =
                                true
                        });
                }
                else if (property.Status == PropertyStatus.Rejected)
                {
                    items.Add(
                        new AdminVerificationHistoryItemResponse
                        {
                            OccurredAt =
                                property.ReviewedAt.Value,

                            Title =
                                "Property verification rejected",

                            Description =
                                string.IsNullOrWhiteSpace(
                                    property.RejectionReason)
                                    ? "The admin rejected the property verification request."
                                    : $"The admin rejected the property verification request. Reason: {property.RejectionReason}",

                            ActorType =
                                "Admin",

                            EventType =
                                "Rejected",

                            IsImportant =
                                true
                        });
                }
            }

            return new AdminVerificationHistoryResponse
            {
                VerificationId =
                    property.Id,

                VerificationType =
                    "Property",

                ReferenceCode =
                    BuildReferenceCode(
                        "PV",
                        property.Id),

                Items =
                    items
                        .OrderByDescending(
                            item =>
                                item.OccurredAt)
                        .ToList()
            };
        }

        private static void AddWaitingFlagIfNeeded(
            List<AdminVerificationHistoryItemResponse> items,
            bool isStillPending,
            DateTimeOffset submittedOrCreatedAt,
            string title)
        {
            if (!isStillPending)
            {
                return;
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            if (submittedOrCreatedAt >
                currentTime.AddHours(
                    -HighPriorityAfterHours))
            {
                return;
            }

            items.Add(
                new AdminVerificationHistoryItemResponse
                {
                    OccurredAt =
                        submittedOrCreatedAt.AddHours(
                            HighPriorityAfterHours),

                    Title =
                        title,

                    Description =
                        "The request has been waiting for more than 48 hours and should be reviewed with higher priority.",

                    ActorType =
                        "System",

                    EventType =
                        "Flagged",

                    IsImportant =
                        true
                });
        }

        // =====================================================
        // Search filters
        // =====================================================

        private static IQueryable<HostProfile>
            ApplyHostSearchFilter(
                IQueryable<HostProfile> query,
                string? search)
        {
            if (string.IsNullOrWhiteSpace(
                    search))
            {
                return query;
            }

            var normalizedSearch =
                search.Trim();

            var likePattern =
                $"%{normalizedSearch}%";

            return query.Where(
                host =>
                    EF.Functions.Like(
                        host.DisplayName,
                        likePattern)
                    ||
                    EF.Functions.Like(
                        host.City,
                        likePattern)
                    ||
                    EF.Functions.Like(
                        host.Country,
                        likePattern)
                    ||
                    host.User.Email != null
                    &&
                    EF.Functions.Like(
                        host.User.Email,
                        likePattern)
                    ||
                    host.User.FirstName != null
                    &&
                    EF.Functions.Like(
                        host.User.FirstName,
                        likePattern)
                    ||
                    host.User.LastName != null
                    &&
                    EF.Functions.Like(
                        host.User.LastName,
                        likePattern));
        }

        private static IQueryable<Property>
            ApplyPropertySearchFilter(
                IQueryable<Property> query,
                string? search)
        {
            if (string.IsNullOrWhiteSpace(
                    search))
            {
                return query;
            }

            var normalizedSearch =
                search.Trim();

            var likePattern =
                $"%{normalizedSearch}%";

            return query.Where(
                property =>
                    EF.Functions.Like(
                        property.Title,
                        likePattern)
                    ||
                    property.City != null
                    &&
                    EF.Functions.Like(
                        property.City,
                        likePattern)
                    ||
                    property.Country != null
                    &&
                    EF.Functions.Like(
                        property.Country,
                        likePattern)
                    ||
                    property.HostProfile.User.Email != null
                    &&
                    EF.Functions.Like(
                        property.HostProfile.User.Email,
                        likePattern)
                    ||
                    property.HostProfile.User.FirstName != null
                    &&
                    EF.Functions.Like(
                        property.HostProfile.User.FirstName,
                        likePattern)
                    ||
                    property.HostProfile.User.LastName != null
                    &&
                    EF.Functions.Like(
                        property.HostProfile.User.LastName,
                        likePattern));
        }

        // =====================================================
        // Helpers
        // =====================================================

        private static string NormalizeQueueType(
            string? type)
        {
            if (string.IsNullOrWhiteSpace(
                    type))
            {
                return "all";
            }

            var normalizedType =
                type.Trim()
                    .ToLowerInvariant();

            return normalizedType switch
            {
                "all" =>
                    "all",

                "host" or "hosts" or "hostapplication" or "hostapplications" =>
                    "host",

                "property" or "properties" =>
                    "property",

                _ =>
                    throw new ArgumentException(
                        "The verification queue type is invalid. Allowed values are all, host, and property.")
            };
        }

        private static string NormalizeHistoryType(
            string? type)
        {
            if (string.IsNullOrWhiteSpace(
                    type))
            {
                throw new ArgumentException(
                    "The verification type is required.");
            }

            var normalizedType =
                type.Trim()
                    .ToLowerInvariant();

            return normalizedType switch
            {
                "host" or "hosts" or "hostapplication" or "hostapplications" =>
                    "host",

                "property" or "properties" =>
                    "property",

                _ =>
                    throw new ArgumentException(
                        "The verification type is invalid. Allowed values are host and property.")
            };
        }

        private static int NormalizePage(
            int page)
        {
            return page <= 0
                ? 1
                : page;
        }

        private static int NormalizePageSize(
            int pageSize)
        {
            if (pageSize <= 0)
            {
                return 20;
            }

            return pageSize > MaximumPageSize
                ? MaximumPageSize
                : pageSize;
        }

        private static int CalculateTotalPages(
            int totalCount,
            int pageSize)
        {
            if (totalCount <= 0)
            {
                return 0;
            }

            return (int)Math.Ceiling(
                totalCount / (double)pageSize);
        }

        private static string BuildReferenceCode(
            string prefix,
            Guid id)
        {
            var shortCode =
                id.ToString("N")[..4]
                    .ToUpperInvariant();

            return $"{prefix}-{shortCode}";
        }

        private static string BuildFullName(
            string? firstName,
            string? lastName,
            string? fallback)
        {
            var fullName =
                string.Join(
                    " ",
                    new[]
                    {
                        firstName,
                        lastName
                    }
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Select(
                        value =>
                            value!.Trim()));

            if (!string.IsNullOrWhiteSpace(
                    fullName))
            {
                return fullName;
            }

            return fallback
                ??
                "Unknown User";
        }

        private static string? BuildLocation(
            string? city,
            string? country)
        {
            var location =
                string.Join(
                    ", ",
                    new[]
                    {
                        city,
                        country
                    }
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value))
                    .Select(
                        value =>
                            value!.Trim()));

            return string.IsNullOrWhiteSpace(
                    location)
                ? null
                : location;
        }

        private static void ValidateVerificationIdentifier(
            Guid verificationId)
        {
            if (verificationId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The verification identifier is invalid.");
            }
        }
    }
}