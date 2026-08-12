using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class WishListService
        : IWishListService
    {
        private const int MaximumWishListsPerUser = 50;

        private const int MaximumPageSize = 100;

        private readonly SmartStayDbContext _dbContext;

        public WishListService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);

            _dbContext = dbContext;
        }

        /*
         * =====================================================
         * Get all wish lists
         * =====================================================
         */

        public async Task<WishListsResponse> GetAllAsync(
            Guid userId,
            Guid? propertyId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);

            if (propertyId.HasValue)
            {
                ValidatePropertyId(propertyId.Value);
            }

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var wishLists =
                await _dbContext.WishLists
                    .AsNoTracking()
                    .Where(wishList =>
                        wishList.UserId == userId)
                    .OrderByDescending(wishList =>
                        wishList.UpdatedAt
                        ??
                        wishList.CreatedAt)
                    .Select(wishList =>
                        new WishListSummaryProjection
                        {
                            Id = wishList.Id,

                            Name = wishList.Name,

                            ItemsCount =
                                wishList.Items.Count,

                            ContainsProperty =
                                propertyId.HasValue
                                &&
                                wishList.Items.Any(item =>
                                    item.PropertyId ==
                                    propertyId.Value),

                            CreatedAt =
                                wishList.CreatedAt,

                            UpdatedAt =
                                wishList.UpdatedAt
                        })
                    .ToListAsync(cancellationToken);

            if (wishLists.Count == 0)
            {
                return new WishListsResponse
                {
                    Items =
                        Array.Empty<WishListSummaryResponse>(),

                    TotalCount = 0
                };
            }

            var wishListIds =
                wishLists
                    .Select(wishList =>
                        wishList.Id)
                    .ToList();

            var previewRows =
                await _dbContext.WishListItems
                    .AsNoTracking()
                    .Where(item =>
                        wishListIds.Contains(
                            item.WishListId)
                        &&
                        item.Property.Status ==
                            PropertyStatus.Published
                        &&
                        item.Property.HostProfile.Status ==
                            HostApplicationStatus.Approved
                        &&
                        item.Property.HostProfile.User
                            .IsActive)
                    .Select(item =>
                        new WishListPreviewProjection
                        {
                            WishListId =
                                item.WishListId,

                            AddedAt =
                                item.AddedAt,

                            CoverImageUrl =
                                item.Property.Images
                                    .OrderByDescending(image =>
                                        image.IsCover)
                                    .ThenBy(image =>
                                        image.DisplayOrder)
                                    .Select(image =>
                                        image.Url)
                                    .FirstOrDefault()
                        })
                    .ToListAsync(cancellationToken);

            var previewImagesByWishListId =
                previewRows
                    .Where(row =>
                        !string.IsNullOrWhiteSpace(
                            row.CoverImageUrl))
                    .GroupBy(row =>
                        row.WishListId)
                    .ToDictionary(
                        group => group.Key,
                        group =>
                            (IReadOnlyList<string>)
                            group
                                .OrderByDescending(row =>
                                    row.AddedAt)
                                .Select(row =>
                                    row.CoverImageUrl!)
                                .Distinct(
                                    StringComparer.Ordinal)
                                .Take(4)
                                .ToList());

            var responseItems =
                wishLists
                    .Select(wishList =>
                    {
                        previewImagesByWishListId
                            .TryGetValue(
                                wishList.Id,
                                out var previewImages);

                        return new WishListSummaryResponse
                        {
                            Id = wishList.Id,

                            Name = wishList.Name,

                            ItemsCount =
                                wishList.ItemsCount,

                            PreviewImageUrls =
                                previewImages
                                ??
                                Array.Empty<string>(),

                            ContainsProperty =
                                wishList.ContainsProperty,

                            CreatedAt =
                                wishList.CreatedAt,

                            UpdatedAt =
                                wishList.UpdatedAt
                        };
                    })
                    .ToList();

            return new WishListsResponse
            {
                Items = responseItems,

                TotalCount =
                    responseItems.Count
            };
        }

        /*
         * =====================================================
         * Get wish list details
         * =====================================================
         */

        public async Task<WishListDetailsResponse> GetByIdAsync(
            Guid userId,
            Guid wishListId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateWishListId(wishListId);
            ValidatePagination(page, pageSize);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var wishList =
                await _dbContext.WishLists
                    .AsNoTracking()
                    .Where(item =>
                        item.Id == wishListId
                        &&
                        item.UserId == userId)
                    .Select(item =>
                        new WishListDetailsProjection
                        {
                            Id = item.Id,

                            Name = item.Name,

                            TotalCount =
                                item.Items.Count,

                            CreatedAt =
                                item.CreatedAt,

                            UpdatedAt =
                                item.UpdatedAt
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (wishList is null)
            {
                throw new KeyNotFoundException(
                    "The wish list was not found.");
            }

            var projectedItems =
                await BuildOwnedItemsQuery(
                        userId,
                        wishListId)
                    .OrderByDescending(item =>
                        item.AddedAt)
                    .Skip(
                        (page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

            var items =
                projectedItems
                    .Select(MapItemResponse)
                    .ToList();

            var totalPages =
                wishList.TotalCount == 0
                    ? 0
                    : (int)Math.Ceiling(
                        wishList.TotalCount
                        /
                        (double)pageSize);

            return new WishListDetailsResponse
            {
                Id = wishList.Id,

                Name = wishList.Name,

                Items = items,

                Page = page,

                PageSize = pageSize,

                TotalCount =
                    wishList.TotalCount,

                TotalPages =
                    totalPages,

                CreatedAt =
                    wishList.CreatedAt,

                UpdatedAt =
                    wishList.UpdatedAt
            };
        }

        /*
         * =====================================================
         * Create wish list
         * =====================================================
         */

        public async Task<WishListSummaryResponse> CreateAsync(
            Guid userId,
            CreateWishListRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);

            ArgumentNullException.ThrowIfNull(request);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var normalizedName =
                NormalizeWishListName(
                    request.Name);

            var wishListsCount =
                await _dbContext.WishLists
                    .CountAsync(
                        wishList =>
                            wishList.UserId == userId,
                        cancellationToken);

            if (wishListsCount >=
                MaximumWishListsPerUser)
            {
                throw new InvalidOperationException(
                    $"A user cannot have more than " +
                    $"{MaximumWishListsPerUser} wish lists.");
            }

            var duplicateNameExists =
                await _dbContext.WishLists
                    .AnyAsync(
                        wishList =>
                            wishList.UserId == userId
                            &&
                            wishList.Name ==
                                normalizedName,
                        cancellationToken);

            if (duplicateNameExists)
            {
                throw new InvalidOperationException(
                    "A wish list with this name already exists.");
            }

            var wishList =
                new WishList
                {
                    Id = Guid.NewGuid(),

                    UserId = userId,

                    Name = normalizedName,

                    CreatedAt =
                        DateTimeOffset.UtcNow
                };

            _dbContext.WishLists.Add(
                wishList);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return new WishListSummaryResponse
            {
                Id = wishList.Id,

                Name = wishList.Name,

                ItemsCount = 0,

                PreviewImageUrls =
                    Array.Empty<string>(),

                ContainsProperty = false,

                CreatedAt =
                    wishList.CreatedAt,

                UpdatedAt =
                    wishList.UpdatedAt
            };
        }

        /*
         * =====================================================
         * Rename wish list
         * =====================================================
         */

        public async Task<WishListSummaryResponse> UpdateAsync(
            Guid userId,
            Guid wishListId,
            UpdateWishListRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateWishListId(wishListId);

            ArgumentNullException.ThrowIfNull(request);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var wishList =
                await GetOwnedWishListAsync(
                    userId,
                    wishListId,
                    cancellationToken);

            var normalizedName =
                NormalizeWishListName(
                    request.Name);

            var duplicateNameExists =
                await _dbContext.WishLists
                    .AnyAsync(
                        item =>
                            item.UserId == userId
                            &&
                            item.Id != wishListId
                            &&
                            item.Name ==
                                normalizedName,
                        cancellationToken);

            if (duplicateNameExists)
            {
                throw new InvalidOperationException(
                    "A wish list with this name already exists.");
            }

            wishList.Name =
                normalizedName;

            wishList.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return await GetSummaryAsync(
                userId,
                wishListId,
                cancellationToken);
        }

        /*
         * =====================================================
         * Delete wish list
         * =====================================================
         */

        public async Task DeleteAsync(
            Guid userId,
            Guid wishListId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateWishListId(wishListId);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var wishList =
                await GetOwnedWishListAsync(
                    userId,
                    wishListId,
                    cancellationToken);

            _dbContext.WishLists.Remove(
                wishList);

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        /*
         * =====================================================
         * Add property to wish list
         * =====================================================
         */

        public async Task<WishListItemResponse> AddItemAsync(
            Guid userId,
            Guid wishListId,
            AddWishListItemRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateWishListId(wishListId);

            ArgumentNullException.ThrowIfNull(request);

            ValidatePropertyId(
                request.PropertyId);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var wishList =
                await GetOwnedWishListAsync(
                    userId,
                    wishListId,
                    cancellationToken);

            var propertyExists =
                await _dbContext.Properties
                    .AsNoTracking()
                    .AnyAsync(
                        property =>
                            property.Id ==
                                request.PropertyId
                            &&
                            property.Status ==
                                PropertyStatus.Published
                            &&
                            property.HostProfile.Status ==
                                HostApplicationStatus.Approved
                            &&
                            property.HostProfile.User
                                .IsActive,
                        cancellationToken);

            if (!propertyExists)
            {
                throw new KeyNotFoundException(
                    "The published property was not found.");
            }

            var itemAlreadyExists =
                await _dbContext.WishListItems
                    .AnyAsync(
                        item =>
                            item.WishListId ==
                                wishListId
                            &&
                            item.PropertyId ==
                                request.PropertyId,
                        cancellationToken);

            if (itemAlreadyExists)
            {
                throw new InvalidOperationException(
                    "The property already exists in this wish list.");
            }

            var currentTime =
                DateTimeOffset.UtcNow;

            var item =
                new WishListItem
                {
                    WishListId =
                        wishListId,

                    PropertyId =
                        request.PropertyId,

                    AddedAt =
                        currentTime
                };

            _dbContext.WishListItems.Add(
                item);

            wishList.UpdatedAt =
                currentTime;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return await GetItemAsync(
                userId,
                wishListId,
                request.PropertyId,
                cancellationToken);
        }

        /*
         * =====================================================
         * Remove property from wish list
         * =====================================================
         */

        public async Task RemoveItemAsync(
            Guid userId,
            Guid wishListId,
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateWishListId(wishListId);
            ValidatePropertyId(propertyId);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var item =
                await _dbContext.WishListItems
                    .Include(item =>
                        item.WishList)
                    .SingleOrDefaultAsync(
                        item =>
                            item.WishListId ==
                                wishListId
                            &&
                            item.PropertyId ==
                                propertyId
                            &&
                            item.WishList.UserId ==
                                userId,
                        cancellationToken);

            if (item is null)
            {
                throw new KeyNotFoundException(
                    "The wish list item was not found.");
            }

            _dbContext.WishListItems.Remove(
                item);

            item.WishList.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        /*
         * =====================================================
         * Update property note
         * =====================================================
         */

        public async Task<WishListItemResponse>
            UpdateItemNoteAsync(
                Guid userId,
                Guid wishListId,
                Guid propertyId,
                UpdateWishListItemNoteRequest request,
                CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);
            ValidateWishListId(wishListId);
            ValidatePropertyId(propertyId);

            ArgumentNullException.ThrowIfNull(request);

            await EnsureActiveUserExistsAsync(
                userId,
                cancellationToken);

            var item =
                await _dbContext.WishListItems
                    .Include(item =>
                        item.WishList)
                    .SingleOrDefaultAsync(
                        item =>
                            item.WishListId ==
                                wishListId
                            &&
                            item.PropertyId ==
                                propertyId
                            &&
                            item.WishList.UserId ==
                                userId,
                        cancellationToken);

            if (item is null)
            {
                throw new KeyNotFoundException(
                    "The wish list item was not found.");
            }

            item.Note =
                NormalizeNote(
                    request.Note);

            item.WishList.UpdatedAt =
                DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return await GetItemAsync(
                userId,
                wishListId,
                propertyId,
                cancellationToken);
        }

        /*
         * =====================================================
         * Private database helpers
         * =====================================================
         */

        private async Task<WishList>
            GetOwnedWishListAsync(
                Guid userId,
                Guid wishListId,
                CancellationToken cancellationToken)
        {
            var wishList =
                await _dbContext.WishLists
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == wishListId
                            &&
                            item.UserId == userId,
                        cancellationToken);

            if (wishList is null)
            {
                throw new KeyNotFoundException(
                    "The wish list was not found.");
            }

            return wishList;
        }

        private async Task<WishListSummaryResponse>
            GetSummaryAsync(
                Guid userId,
                Guid wishListId,
                CancellationToken cancellationToken)
        {
            var wishList =
                await _dbContext.WishLists
                    .AsNoTracking()
                    .Where(item =>
                        item.Id == wishListId
                        &&
                        item.UserId == userId)
                    .Select(item =>
                        new WishListSummaryProjection
                        {
                            Id = item.Id,

                            Name = item.Name,

                            ItemsCount =
                                item.Items.Count,

                            ContainsProperty = false,

                            CreatedAt =
                                item.CreatedAt,

                            UpdatedAt =
                                item.UpdatedAt
                        })
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (wishList is null)
            {
                throw new KeyNotFoundException(
                    "The wish list was not found.");
            }

            var previewImages =
                await _dbContext.WishListItems
                    .AsNoTracking()
                    .Where(item =>
                        item.WishListId ==
                            wishListId
                        &&
                        item.Property.Status ==
                            PropertyStatus.Published
                        &&
                        item.Property.HostProfile.Status ==
                            HostApplicationStatus.Approved
                        &&
                        item.Property.HostProfile.User
                            .IsActive)
                    .OrderByDescending(item =>
                        item.AddedAt)
                    .Select(item =>
                        item.Property.Images
                            .OrderByDescending(image =>
                                image.IsCover)
                            .ThenBy(image =>
                                image.DisplayOrder)
                            .Select(image =>
                                image.Url)
                            .FirstOrDefault())
                    .Where(imageUrl =>
                        imageUrl != null)
                    .Take(4)
                    .ToListAsync(cancellationToken);

            return new WishListSummaryResponse
            {
                Id = wishList.Id,

                Name = wishList.Name,

                ItemsCount =
                    wishList.ItemsCount,

                PreviewImageUrls =
                    previewImages
                        .Where(imageUrl =>
                            !string.IsNullOrWhiteSpace(
                                imageUrl))
                        .Select(imageUrl =>
                            imageUrl!)
                        .Distinct(
                            StringComparer.Ordinal)
                        .ToList(),

                ContainsProperty = false,

                CreatedAt =
                    wishList.CreatedAt,

                UpdatedAt =
                    wishList.UpdatedAt
            };
        }

        private async Task<WishListItemResponse> GetItemAsync(
            Guid userId,
            Guid wishListId,
            Guid propertyId,
            CancellationToken cancellationToken)
        {
            var item =
                await BuildOwnedItemsQuery(
                        userId,
                        wishListId)
                    .SingleOrDefaultAsync(
                        item =>
                            item.PropertyId ==
                                propertyId,
                        cancellationToken);

            if (item is null)
            {
                throw new KeyNotFoundException(
                    "The wish list item was not found.");
            }

            return MapItemResponse(item);
        }

        private IQueryable<WishListItemProjection>
            BuildOwnedItemsQuery(
                Guid userId,
                Guid wishListId)
        {
            return _dbContext.WishListItems
                .AsNoTracking()
                .Where(item =>
                    item.WishListId == wishListId
                    &&
                    item.WishList.UserId == userId)
                .Select(item =>
                    new WishListItemProjection
                    {
                        PropertyId =
                            item.PropertyId,

                        Title =
                            item.Property.Title,

                        PropertyType =
                            item.Property.PropertyType,

                        SpaceType =
                            item.Property.SpaceType,

                        Country =
                            item.Property.Country,

                        City =
                            item.Property.City,

                        PricePerNight =
                            item.Property.PricePerNight,

                        Currency =
                            item.Property.Currency,

                        CoverImageUrl =
                            item.Property.Images
                                .OrderByDescending(image =>
                                    image.IsCover)
                                .ThenBy(image =>
                                    image.DisplayOrder)
                                .Select(image =>
                                    image.Url)
                                .FirstOrDefault(),

                        MaxGuests =
                            item.Property.MaxGuests,

                        IsAvailable =
                            item.Property.Status ==
                                PropertyStatus.Published
                            &&
                            item.Property.HostProfile.Status ==
                                HostApplicationStatus.Approved
                            &&
                            item.Property.HostProfile.User
                                .IsActive,

                        Note =
                            item.Note,

                        AddedAt =
                            item.AddedAt
                    });
        }

        private static WishListItemResponse MapItemResponse(
    WishListItemProjection item)
        {
            return new WishListItemResponse
            {
                PropertyId =
                    item.PropertyId,

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
                    item.PricePerNight,

                Currency =
                    item.Currency,

                CoverImageUrl =
                    item.CoverImageUrl,

                MaxGuests =
                    item.MaxGuests,

                /*
                 * The decorator replaces these default values
                 * with the real published-review aggregates.
                 */
                AverageRating =
                    0m,

                ReviewsCount =
                    0,

                IsAvailable =
                    item.IsAvailable,

                Note =
                    item.Note,

                AddedAt =
                    item.AddedAt
            };
        }

        private async Task EnsureActiveUserExistsAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var user =
                await _dbContext.Users
                    .AsNoTracking()
                    .Where(item =>
                        item.Id == userId)
                    .Select(item =>
                        new
                        {
                            item.IsActive
                        })
                    .SingleOrDefaultAsync(
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
        }

        /*
         * =====================================================
         * Validation helpers
         * =====================================================
         */

        private static string NormalizeWishListName(
            string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "The wish list name is required.");
            }

            var normalizedName =
                name.Trim();

            if (normalizedName.Length is < 2 or > 100)
            {
                throw new ArgumentException(
                    "The wish list name must be between 2 and 100 characters.");
            }

            return normalizedName;
        }

        private static string? NormalizeNote(
            string? note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return null;
            }

            var normalizedNote =
                note.Trim();

            if (normalizedNote.Length > 500)
            {
                throw new ArgumentException(
                    "The note cannot exceed 500 characters.");
            }

            return normalizedNote;
        }

        private static void ValidateUserId(
            Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The user identifier is invalid.");
            }
        }

        private static void ValidateWishListId(
            Guid wishListId)
        {
            if (wishListId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The wish list identifier is invalid.");
            }
        }

        private static void ValidatePropertyId(
            Guid propertyId)
        {
            if (propertyId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The property identifier is invalid.");
            }
        }

        private static void ValidatePagination(
            int page,
            int pageSize)
        {
            if (page < 1)
            {
                throw new ArgumentException(
                    "Page must be greater than or equal to 1.");
            }

            if (pageSize < 1 ||
                pageSize > MaximumPageSize)
            {
                throw new ArgumentException(
                    $"Page size must be between 1 and " +
                    $"{MaximumPageSize}.");
            }
        }

        /*
         * =====================================================
         * Query projections
         * =====================================================
         */

        private sealed class WishListSummaryProjection
        {
            public Guid Id { get; set; }

            public string Name { get; set; } =
                string.Empty;

            public int ItemsCount { get; set; }

            public bool ContainsProperty { get; set; }

            public DateTimeOffset CreatedAt { get; set; }

            public DateTimeOffset? UpdatedAt { get; set; }
        }

        private sealed class WishListDetailsProjection
        {
            public Guid Id { get; set; }

            public string Name { get; set; } =
                string.Empty;

            public int TotalCount { get; set; }

            public DateTimeOffset CreatedAt { get; set; }

            public DateTimeOffset? UpdatedAt { get; set; }
        }

        private sealed class WishListPreviewProjection
        {
            public Guid WishListId { get; set; }

            public string? CoverImageUrl { get; set; }

            public DateTimeOffset AddedAt { get; set; }
        }

        private sealed class WishListItemProjection
        {
            public Guid PropertyId { get; set; }

            public string Title { get; set; } =
                string.Empty;

            public PropertyType PropertyType { get; set; }

            public PropertySpaceType SpaceType { get; set; }

            public string? Country { get; set; }

            public string? City { get; set; }

            public decimal? PricePerNight { get; set; }

            public string Currency { get; set; } =
                string.Empty;

            public string? CoverImageUrl { get; set; }

            public int? MaxGuests { get; set; }

            public bool IsAvailable { get; set; }

            public string? Note { get; set; }

            public DateTimeOffset AddedAt { get; set; }
        }
    }
}