namespace SmartStayBLL
{
    public sealed class PublicPropertyRatingDecorator
        : IPublicPropertyService
    {
        private readonly PublicPropertyService
            _publicPropertyService;

        private readonly IPropertyRatingQueryService
            _propertyRatingQueryService;

        public PublicPropertyRatingDecorator(
            PublicPropertyService publicPropertyService,
            IPropertyRatingQueryService
                propertyRatingQueryService)
        {
            ArgumentNullException.ThrowIfNull(
                publicPropertyService);

            ArgumentNullException.ThrowIfNull(
                propertyRatingQueryService);

            _publicPropertyService =
                publicPropertyService;

            _propertyRatingQueryService =
                propertyRatingQueryService;
        }

        public async Task<PublicPropertiesResponse>
            SearchAsync(
                PublicPropertySearchRequest request,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _publicPropertyService
                    .SearchAsync(
                        request,
                        cancellationToken);

            if (response.Items.Count == 0)
            {
                return response;
            }

            var ratings =
                await _propertyRatingQueryService
                    .GetManyAsync(
                        response.Items.Select(item =>
                            item.Id),
                        cancellationToken);

            foreach (var item in response.Items)
            {
                if (!ratings.TryGetValue(
                        item.Id,
                        out var rating))
                {
                    continue;
                }

                item.AverageRating =
                    rating.AverageRating;

                item.ReviewsCount =
                    rating.ReviewsCount;
            }

            return response;
        }

        public async Task<PublicPropertyDetailsResponse>
            GetByIdAsync(
                Guid propertyId,
                CancellationToken cancellationToken = default)
        {
            var response =
                await _publicPropertyService
                    .GetByIdAsync(
                        propertyId,
                        cancellationToken);

            var rating =
                await _propertyRatingQueryService
                    .GetAsync(
                        propertyId,
                        cancellationToken);

            response.AverageRating =
                rating.AverageRating;

            response.ReviewsCount =
                rating.ReviewsCount;

            return response;
        }
    }
}