namespace SmartStayBLL
{
    public sealed class WishListRatingDecorator
        : IWishListService
    {
        private readonly WishListService
            _wishListService;

        private readonly IPropertyRatingQueryService
            _propertyRatingQueryService;

        public WishListRatingDecorator(
            WishListService wishListService,
            IPropertyRatingQueryService
                propertyRatingQueryService)
        {
            ArgumentNullException.ThrowIfNull(
                wishListService);

            ArgumentNullException.ThrowIfNull(
                propertyRatingQueryService);

            _wishListService =
                wishListService;

            _propertyRatingQueryService =
                propertyRatingQueryService;
        }

        public Task<WishListsResponse> GetAllAsync(
            Guid userId,
            Guid? propertyId,
            CancellationToken cancellationToken = default)
        {
            return _wishListService.GetAllAsync(
                userId,
                propertyId,
                cancellationToken);
        }

        public async Task<WishListDetailsResponse>
            GetByIdAsync(
                Guid userId,
                Guid wishListId,
                int page,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _wishListService
                    .GetByIdAsync(
                        userId,
                        wishListId,
                        page,
                        pageSize,
                        cancellationToken);

            await ApplyRatingsAsync(
                response.Items,
                cancellationToken);

            return response;
        }

        public Task<WishListSummaryResponse> CreateAsync(
            Guid userId,
            CreateWishListRequest request,
            CancellationToken cancellationToken = default)
        {
            return _wishListService.CreateAsync(
                userId,
                request,
                cancellationToken);
        }

        public Task<WishListSummaryResponse> UpdateAsync(
            Guid userId,
            Guid wishListId,
            UpdateWishListRequest request,
            CancellationToken cancellationToken = default)
        {
            return _wishListService.UpdateAsync(
                userId,
                wishListId,
                request,
                cancellationToken);
        }

        public Task DeleteAsync(
            Guid userId,
            Guid wishListId,
            CancellationToken cancellationToken = default)
        {
            return _wishListService.DeleteAsync(
                userId,
                wishListId,
                cancellationToken);
        }

        public async Task<WishListItemResponse>
            AddItemAsync(
                Guid userId,
                Guid wishListId,
                AddWishListItemRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _wishListService
                    .AddItemAsync(
                        userId,
                        wishListId,
                        request,
                        cancellationToken);

            await ApplyRatingAsync(
                response,
                cancellationToken);

            return response;
        }

        public Task RemoveItemAsync(
            Guid userId,
            Guid wishListId,
            Guid propertyId,
            CancellationToken cancellationToken = default)
        {
            return _wishListService.RemoveItemAsync(
                userId,
                wishListId,
                propertyId,
                cancellationToken);
        }

        public async Task<WishListItemResponse>
            UpdateItemNoteAsync(
                Guid userId,
                Guid wishListId,
                Guid propertyId,
                UpdateWishListItemNoteRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _wishListService
                    .UpdateItemNoteAsync(
                        userId,
                        wishListId,
                        propertyId,
                        request,
                        cancellationToken);

            await ApplyRatingAsync(
                response,
                cancellationToken);

            return response;
        }

        private async Task ApplyRatingsAsync(
            IReadOnlyList<WishListItemResponse> items,
            CancellationToken cancellationToken)
        {
            if (items.Count == 0)
            {
                return;
            }

            var ratings =
                await _propertyRatingQueryService
                    .GetManyAsync(
                        items.Select(item =>
                            item.PropertyId),
                        cancellationToken);

            foreach (var item in items)
            {
                if (!ratings.TryGetValue(
                        item.PropertyId,
                        out var rating))
                {
                    continue;
                }

                item.AverageRating =
                    rating.AverageRating;

                item.ReviewsCount =
                    rating.ReviewsCount;
            }
        }

        private async Task ApplyRatingAsync(
            WishListItemResponse item,
            CancellationToken cancellationToken)
        {
            var rating =
                await _propertyRatingQueryService
                    .GetAsync(
                        item.PropertyId,
                        cancellationToken);

            item.AverageRating =
                rating.AverageRating;

            item.ReviewsCount =
                rating.ReviewsCount;
        }
    }
}