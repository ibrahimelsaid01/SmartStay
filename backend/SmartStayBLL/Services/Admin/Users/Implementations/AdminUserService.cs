using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class AdminUserService
        : IAdminUserService
    {
        private const int MaximumPageSize = 100;

        private readonly SmartStayDbContext _dbContext;

        private readonly UserManager<ApplicationUser>
            _userManager;

        public AdminUserService(
            SmartStayDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            ArgumentNullException.ThrowIfNull(
                userManager);

            _dbContext =
                dbContext;

            _userManager =
                userManager;
        }

        // =====================================================
        // List users
        // =====================================================

        public async Task<AdminUsersResponse> GetUsersAsync(
            AdminUserSearchRequest request,
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

            var query =
                _dbContext.Users
                    .AsNoTracking()
                    .AsQueryable();

            query =
                ApplySearchFilter(
                    query,
                    request.Search);

            query =
                ApplyStatusFilters(
                    query,
                    request.IsActive,
                    request.IsProfileCompleted);

            query =
                await ApplyRoleFilterAsync(
                    query,
                    request.Role,
                    cancellationToken);

            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            var users =
                await query
                    .OrderByDescending(
                        user =>
                            user.CreatedAt)
                    .ThenBy(
                        user =>
                            user.Email)
                    .Skip(
                        (page - 1) * pageSize)
                    .Take(
                        pageSize)
                    .Select(
                        user =>
                            new UserProjection
                            {
                                UserId =
                                    user.Id,

                                Email =
                                    user.Email,

                                PhoneNumber =
                                    user.PhoneNumber,

                                FirstName =
                                    user.FirstName,

                                LastName =
                                    user.LastName,

                                ProfileImageUrl =
                                    user.ProfileImageUrl,

                                IsActive =
                                    user.IsActive,

                                IsProfileCompleted =
                                    user.IsProfileCompleted,

                                CreatedAt =
                                    user.CreatedAt,

                                UpdatedAt =
                                    user.UpdatedAt
                            })
                    .ToListAsync(
                        cancellationToken);

            var userIds =
                users
                    .Select(
                        user =>
                            user.UserId)
                    .ToList();

            var rolesByUserId =
                await GetRolesByUserIdAsync(
                    userIds,
                    cancellationToken);

            var hostProfilesByUserId =
                await GetHostProfilesByUserIdAsync(
                    userIds,
                    cancellationToken);

            var propertiesCountByUserId =
                await GetPropertiesCountByUserIdAsync(
                    userIds,
                    cancellationToken);

            var bookingsCountByUserId =
                await GetGuestBookingsCountByUserIdAsync(
                    userIds,
                    cancellationToken);

            var items =
                users
                    .Select(
                        user =>
                        {
                            rolesByUserId.TryGetValue(
                                user.UserId,
                                out var roles);

                            hostProfilesByUserId.TryGetValue(
                                user.UserId,
                                out var hostProfile);

                            propertiesCountByUserId.TryGetValue(
                                user.UserId,
                                out var propertiesCount);

                            bookingsCountByUserId.TryGetValue(
                                user.UserId,
                                out var bookingsCount);

                            return new AdminUserListItemResponse
                            {
                                UserId =
                                    user.UserId,

                                Email =
                                    user.Email,

                                PhoneNumber =
                                    user.PhoneNumber,

                                FullName =
                                    BuildFullName(
                                        user.FirstName,
                                        user.LastName,
                                        user.Email),

                                FirstName =
                                    user.FirstName,

                                LastName =
                                    user.LastName,

                                ProfileImageUrl =
                                    user.ProfileImageUrl,

                                IsActive =
                                    user.IsActive,

                                IsProfileCompleted =
                                    user.IsProfileCompleted,

                                CreatedAt =
                                    user.CreatedAt,

                                UpdatedAt =
                                    user.UpdatedAt,

                                Roles =
                                    roles
                                    ??
                                    new List<string>(),

                                IsHost =
                                    hostProfile is not null,

                                HostProfileId =
                                    hostProfile?.HostProfileId,

                                HostStatus =
                                    hostProfile?.Status,

                                PropertiesCount =
                                    propertiesCount,

                                GuestBookingsCount =
                                    bookingsCount
                            };
                        })
                    .ToList();

            return new AdminUsersResponse
            {
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

                Items =
                    items
            };
        }

        // =====================================================
        // User details
        // =====================================================

        public async Task<AdminUserDetailsResponse> GetUserByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(
                userId);

            var user =
                await _dbContext.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == userId,
                        cancellationToken);

            if (user is null)
            {
                throw new KeyNotFoundException(
                    "The user was not found.");
            }

            var roles =
                await _userManager.GetRolesAsync(
                    user);

            var activeRefreshTokensCount =
                await _dbContext.RefreshTokens
                    .AsNoTracking()
                    .CountAsync(
                        token =>
                            token.UserId == userId
                            &&
                            token.RevokedAt == null
                            &&
                            token.ExpiresAt >
                                DateTimeOffset.UtcNow,
                        cancellationToken);

            var hostProfile =
                await _dbContext.HostProfiles
                    .AsNoTracking()
                    .Where(
                        profile =>
                            profile.UserId == userId)
                    .Select(
                        profile =>
                            new AdminUserHostProfileResponse
                            {
                                HostProfileId =
                                    profile.Id,

                                DisplayName =
                                    profile.DisplayName,

                                ProfileImageUrl =
                                    profile.ProfileImageUrl,

                                Status =
                                    profile.Status.ToString(),

                                RejectionReason =
                                    profile.RejectionReason,

                                CreatedAt =
                                    profile.CreatedAt,

                                SubmittedAt =
                                    profile.SubmittedAt,

                                ReviewedAt =
                                    profile.ReviewedAt
                            })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            var guestBookingStats =
                await GetGuestBookingStatsAsync(
                    userId,
                    cancellationToken);

            var hostPropertyStats =
                await GetHostPropertyStatsAsync(
                    userId,
                    cancellationToken);

            return new AdminUserDetailsResponse
            {
                UserId =
                    user.Id,

                Email =
                    user.Email,

                UserName =
                    user.UserName,

                PhoneNumber =
                    user.PhoneNumber,

                FullName =
                    BuildFullName(
                        user.FirstName,
                        user.LastName,
                        user.Email),

                FirstName =
                    user.FirstName,

                LastName =
                    user.LastName,

                ProfileImageUrl =
                    user.ProfileImageUrl,

                IsActive =
                    user.IsActive,

                IsProfileCompleted =
                    user.IsProfileCompleted,

                CreatedAt =
                    user.CreatedAt,

                UpdatedAt =
                    user.UpdatedAt,

                Roles =
                    roles
                        .OrderBy(
                            role =>
                                role,
                            StringComparer.OrdinalIgnoreCase)
                        .ToList(),

                ActiveRefreshTokensCount =
                    activeRefreshTokensCount,

                HostProfile =
                    hostProfile,

                GuestBookingStats =
                    guestBookingStats,

                HostPropertyStats =
                    hostPropertyStats
            };
        }

        // =====================================================
        // Deactivate / activate
        // =====================================================

        public async Task<AdminUserStatusResponse> DeactivateUserAsync(
            Guid currentAdminUserId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(
                currentAdminUserId);

            ValidateUserIdentifier(
                userId);

            if (currentAdminUserId == userId)
            {
                throw new InvalidOperationException(
                    "Admins cannot deactivate their own account.");
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

            var roles =
                await _userManager.GetRolesAsync(
                    user);

            if (roles.Contains(
                    RoleNames.Admin,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Admin accounts cannot be deactivated from this endpoint.");
            }

            if (!user.IsActive)
            {
                return new AdminUserStatusResponse
                {
                    UserId =
                        user.Id,

                    IsActive =
                        false,

                    RevokedRefreshTokensCount =
                        0,

                    UnpublishedPropertiesCount =
                        0,

                    Message =
                        "The user account is already inactive."
                };
            }

            await EnsureUserHasNoActiveBookingsAsync(
                user.Id,
                cancellationToken);

            var currentTime =
                DateTimeOffset.UtcNow;

            var unpublishedPropertiesCount =
                await UnpublishHostPropertiesAsync(
                    user.Id,
                    currentTime,
                    cancellationToken);

            var revokedRefreshTokensCount =
                await RevokeUserRefreshTokensAsync(
                    user.Id,
                    currentTime,
                    "Deactivated by admin.",
                    cancellationToken);

            user.IsActive =
                false;

            user.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new AdminUserStatusResponse
            {
                UserId =
                    user.Id,

                IsActive =
                    user.IsActive,

                RevokedRefreshTokensCount =
                    revokedRefreshTokensCount,

                UnpublishedPropertiesCount =
                    unpublishedPropertiesCount,

                Message =
                    "The user account was deactivated successfully."
            };
        }

        public async Task<AdminUserStatusResponse> ActivateUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserIdentifier(
                userId);

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

            if (user.IsActive)
            {
                return new AdminUserStatusResponse
                {
                    UserId =
                        user.Id,

                    IsActive =
                        true,

                    RevokedRefreshTokensCount =
                        0,

                    UnpublishedPropertiesCount =
                        0,

                    Message =
                        "The user account is already active."
                };
            }

            user.IsActive =
                true;

            user.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new AdminUserStatusResponse
            {
                UserId =
                    user.Id,

                IsActive =
                    true,

                RevokedRefreshTokensCount =
                    0,

                UnpublishedPropertiesCount =
                    0,

                Message =
                    "The user account was activated successfully. Previously unpublished properties were not republished automatically."
            };
        }

        // =====================================================
        // Query filters
        // =====================================================

        private static IQueryable<ApplicationUser>
            ApplySearchFilter(
                IQueryable<ApplicationUser> query,
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
                user =>
                    user.Email != null
                    &&
                    EF.Functions.Like(
                        user.Email,
                        likePattern)
                    ||
                    user.UserName != null
                    &&
                    EF.Functions.Like(
                        user.UserName,
                        likePattern)
                    ||
                    user.PhoneNumber != null
                    &&
                    EF.Functions.Like(
                        user.PhoneNumber,
                        likePattern)
                    ||
                    user.FirstName != null
                    &&
                    EF.Functions.Like(
                        user.FirstName,
                        likePattern)
                    ||
                    user.LastName != null
                    &&
                    EF.Functions.Like(
                        user.LastName,
                        likePattern));
        }

        private static IQueryable<ApplicationUser>
            ApplyStatusFilters(
                IQueryable<ApplicationUser> query,
                bool? isActive,
                bool? isProfileCompleted)
        {
            if (isActive.HasValue)
            {
                query =
                    query.Where(
                        user =>
                            user.IsActive ==
                            isActive.Value);
            }

            if (isProfileCompleted.HasValue)
            {
                query =
                    query.Where(
                        user =>
                            user.IsProfileCompleted ==
                            isProfileCompleted.Value);
            }

            return query;
        }

        private async Task<IQueryable<ApplicationUser>>
            ApplyRoleFilterAsync(
                IQueryable<ApplicationUser> query,
                string? role,
                CancellationToken cancellationToken)
        {
            var normalizedRole =
                NormalizeRoleFilter(
                    role);

            if (normalizedRole is null)
            {
                return query;
            }

            var roleId =
                await _dbContext.Roles
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.Name == normalizedRole)
                    .Select(
                        item =>
                            item.Id)
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (roleId == Guid.Empty)
            {
                return query.Where(
                    _ =>
                        false);
            }

            return query.Where(
                user =>
                    _dbContext.UserRoles.Any(
                        userRole =>
                            userRole.UserId == user.Id
                            &&
                            userRole.RoleId == roleId));
        }

        private static string? NormalizeRoleFilter(
            string? role)
        {
            if (string.IsNullOrWhiteSpace(
                    role))
            {
                return null;
            }

            var normalizedRole =
                role.Trim();

            if (string.Equals(
                    normalizedRole,
                    RoleNames.Admin,
                    StringComparison.OrdinalIgnoreCase))
            {
                return RoleNames.Admin;
            }

            if (string.Equals(
                    normalizedRole,
                    RoleNames.Host,
                    StringComparison.OrdinalIgnoreCase))
            {
                return RoleNames.Host;
            }

            if (string.Equals(
                    normalizedRole,
                    RoleNames.User,
                    StringComparison.OrdinalIgnoreCase))
            {
                return RoleNames.User;
            }

            throw new ArgumentException(
                "The role filter is invalid. Allowed values are Admin, Host, and User.");
        }

        // =====================================================
        // Related data for list
        // =====================================================

        private async Task<Dictionary<Guid, List<string>>>
            GetRolesByUserIdAsync(
                IReadOnlyList<Guid> userIds,
                CancellationToken cancellationToken)
        {
            if (userIds.Count == 0)
            {
                return new Dictionary<Guid, List<string>>();
            }

            var rows =
                await (
                    from userRole in _dbContext.UserRoles.AsNoTracking()
                    join role in _dbContext.Roles.AsNoTracking()
                        on userRole.RoleId equals role.Id
                    where userIds.Contains(userRole.UserId)
                    select new
                    {
                        userRole.UserId,
                        RoleName =
                            role.Name
                    })
                    .ToListAsync(
                        cancellationToken);

            return rows
                .Where(
                    row =>
                        !string.IsNullOrWhiteSpace(
                            row.RoleName))
                .GroupBy(
                    row =>
                        row.UserId)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        group
                            .Select(
                                row =>
                                    row.RoleName!)
                            .Distinct(
                                StringComparer.OrdinalIgnoreCase)
                            .OrderBy(
                                role =>
                                    role,
                                StringComparer.OrdinalIgnoreCase)
                            .ToList());
        }

        private async Task<
            Dictionary<Guid, HostProfileProjection>>
            GetHostProfilesByUserIdAsync(
                IReadOnlyList<Guid> userIds,
                CancellationToken cancellationToken)
        {
            if (userIds.Count == 0)
            {
                return new Dictionary<Guid, HostProfileProjection>();
            }

            return await _dbContext.HostProfiles
                .AsNoTracking()
                .Where(
                    host =>
                        userIds.Contains(
                            host.UserId))
                .Select(
                    host =>
                        new HostProfileProjection
                        {
                            UserId =
                                host.UserId,

                            HostProfileId =
                                host.Id,

                            Status =
                                host.Status.ToString()
                        })
                .ToDictionaryAsync(
                    item =>
                        item.UserId,
                    cancellationToken);
        }

        private async Task<Dictionary<Guid, int>>
            GetPropertiesCountByUserIdAsync(
                IReadOnlyList<Guid> userIds,
                CancellationToken cancellationToken)
        {
            if (userIds.Count == 0)
            {
                return new Dictionary<Guid, int>();
            }

            var rows =
                await _dbContext.Properties
                    .AsNoTracking()
                    .Where(
                        property =>
                            userIds.Contains(
                                property.HostProfile.UserId))
                    .GroupBy(
                        property =>
                            property.HostProfile.UserId)
                    .Select(
                        group =>
                            new
                            {
                                UserId =
                                    group.Key,

                                Count =
                                    group.Count()
                            })
                    .ToListAsync(
                        cancellationToken);

            return rows.ToDictionary(
                item =>
                    item.UserId,
                item =>
                    item.Count);
        }

        private async Task<Dictionary<Guid, int>>
            GetGuestBookingsCountByUserIdAsync(
                IReadOnlyList<Guid> userIds,
                CancellationToken cancellationToken)
        {
            if (userIds.Count == 0)
            {
                return new Dictionary<Guid, int>();
            }

            var rows =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .Where(
                        booking =>
                            userIds.Contains(
                                booking.GuestUserId))
                    .GroupBy(
                        booking =>
                            booking.GuestUserId)
                    .Select(
                        group =>
                            new
                            {
                                UserId =
                                    group.Key,

                                Count =
                                    group.Count()
                            })
                    .ToListAsync(
                        cancellationToken);

            return rows.ToDictionary(
                item =>
                    item.UserId,
                item =>
                    item.Count);
        }

        // =====================================================
        // Details stats
        // =====================================================

        private async Task<AdminUserBookingStatsResponse>
            GetGuestBookingStatsAsync(
                Guid userId,
                CancellationToken cancellationToken)
        {
            var groupedCounts =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .Where(
                        booking =>
                            booking.GuestUserId == userId)
                    .GroupBy(
                        booking =>
                            booking.Status)
                    .Select(
                        group =>
                            new
                            {
                                Status =
                                    group.Key,

                                Count =
                                    group.Count()
                            })
                    .ToListAsync(
                        cancellationToken);

            var response =
                new AdminUserBookingStatsResponse();

            foreach (var item in groupedCounts)
            {
                response.TotalBookings +=
                    item.Count;

                switch (item.Status)
                {
                    case BookingStatus.Pending:
                        response.PendingBookings =
                            item.Count;
                        break;

                    case BookingStatus.Confirmed:
                        response.ConfirmedBookings =
                            item.Count;
                        break;

                    case BookingStatus.Cancelled:
                        response.CancelledBookings =
                            item.Count;
                        break;

                    case BookingStatus.Completed:
                        response.CompletedBookings =
                            item.Count;
                        break;

                    case BookingStatus.Expired:
                        response.ExpiredBookings =
                            item.Count;
                        break;
                }
            }

            return response;
        }

        private async Task<AdminUserPropertyStatsResponse>
            GetHostPropertyStatsAsync(
                Guid userId,
                CancellationToken cancellationToken)
        {
            var groupedCounts =
                await _dbContext.Properties
                    .AsNoTracking()
                    .Where(
                        property =>
                            property.HostProfile.UserId ==
                            userId)
                    .GroupBy(
                        property =>
                            property.Status)
                    .Select(
                        group =>
                            new
                            {
                                Status =
                                    group.Key,

                                Count =
                                    group.Count()
                            })
                    .ToListAsync(
                        cancellationToken);

            var response =
                new AdminUserPropertyStatsResponse();

            foreach (var item in groupedCounts)
            {
                response.TotalProperties +=
                    item.Count;

                switch (item.Status)
                {
                    case PropertyStatus.Draft:
                        response.DraftProperties =
                            item.Count;
                        break;

                    case PropertyStatus.Pending:
                        response.PendingProperties =
                            item.Count;
                        break;

                    case PropertyStatus.Published:
                        response.PublishedProperties =
                            item.Count;
                        break;

                    case PropertyStatus.Rejected:
                        response.RejectedProperties =
                            item.Count;
                        break;

                    case PropertyStatus.Unpublished:
                        response.UnpublishedProperties =
                            item.Count;
                        break;
                }
            }

            return response;
        }

        // =====================================================
        // Deactivation helpers
        // =====================================================

        private async Task EnsureUserHasNoActiveBookingsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var currentTime =
                DateTimeOffset.UtcNow;

            var hasActiveGuestBookings =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .AnyAsync(
                        booking =>
                            booking.GuestUserId == userId
                            &&
                            (
                                booking.Status ==
                                    BookingStatus.Confirmed
                                ||
                                booking.Status ==
                                    BookingStatus.Pending
                                    &&
                                    (
                                        !booking.ExpiresAt.HasValue
                                        ||
                                        booking.ExpiresAt.Value >
                                            currentTime
                                    )
                            ),
                        cancellationToken);

            if (hasActiveGuestBookings)
            {
                throw new InvalidOperationException(
                    "This user cannot be deactivated because they have active guest bookings.");
            }

            var hasActiveHostBookings =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .AnyAsync(
                        booking =>
                            booking.Property.HostProfile.UserId == userId
                            &&
                            (
                                booking.Status ==
                                    BookingStatus.Confirmed
                                ||
                                booking.Status ==
                                    BookingStatus.Pending
                                    &&
                                    (
                                        !booking.ExpiresAt.HasValue
                                        ||
                                        booking.ExpiresAt.Value >
                                            currentTime
                                    )
                            ),
                        cancellationToken);

            if (hasActiveHostBookings)
            {
                throw new InvalidOperationException(
                    "This user cannot be deactivated because they have active host bookings.");
            }
        }

        private async Task<int> UnpublishHostPropertiesAsync(
            Guid userId,
            DateTimeOffset currentTime,
            CancellationToken cancellationToken)
        {
            var publishedProperties =
                await _dbContext.Properties
                    .Where(
                        property =>
                            property.HostProfile.UserId == userId
                            &&
                            property.Status ==
                                PropertyStatus.Published)
                    .ToListAsync(
                        cancellationToken);

            foreach (var property in publishedProperties)
            {
                property.Status =
                    PropertyStatus.Unpublished;

                property.UpdatedAt =
                    currentTime;
            }

            return publishedProperties.Count;
        }

        private async Task<int> RevokeUserRefreshTokensAsync(
            Guid userId,
            DateTimeOffset currentTime,
            string reason,
            CancellationToken cancellationToken)
        {
            var refreshTokens =
                await _dbContext.RefreshTokens
                    .Where(
                        token =>
                            token.UserId == userId
                            &&
                            token.RevokedAt == null
                            &&
                            token.ExpiresAt >
                                currentTime)
                    .ToListAsync(
                        cancellationToken);

            foreach (var refreshToken in refreshTokens)
            {
                refreshToken.RevokedAt =
                    currentTime;

                refreshToken.RevocationReason =
                    reason;
            }

            return refreshTokens.Count;
        }

        // =====================================================
        // Generic helpers
        // =====================================================

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

        private static void ValidateUserIdentifier(
            Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The user identifier is invalid.");
            }
        }

        // =====================================================
        // Internal projections
        // =====================================================

        private sealed class UserProjection
        {
            public Guid UserId { get; init; }

            public string? Email { get; init; }

            public string? PhoneNumber { get; init; }

            public string? FirstName { get; init; }

            public string? LastName { get; init; }

            public string? ProfileImageUrl { get; init; }

            public bool IsActive { get; init; }

            public bool IsProfileCompleted { get; init; }

            public DateTimeOffset CreatedAt { get; init; }

            public DateTimeOffset? UpdatedAt { get; init; }
        }

        private sealed class HostProfileProjection
        {
            public Guid UserId { get; init; }

            public Guid HostProfileId { get; init; }

            public string Status { get; init; } =
                string.Empty;
        }
    }
}