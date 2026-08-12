using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class PropertyRatingQueryService
        : IPropertyRatingQueryService
    {
        private readonly SmartStayDbContext _dbContext;

        public PropertyRatingQueryService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext = dbContext;
        }

        public async Task<PropertyRatingSnapshot>
            GetAsync(
                Guid propertyId,
                CancellationToken cancellationToken = default)
        {
            if (propertyId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The property identifier is invalid.");
            }

            var ratings =
                await GetManyAsync(
                    new[]
                    {
                        propertyId
                    },
                    cancellationToken);

            return ratings[propertyId];
        }

        public async Task<
            IReadOnlyDictionary<
                Guid,
                PropertyRatingSnapshot>>
            GetManyAsync(
                IEnumerable<Guid> propertyIds,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                propertyIds);

            var normalizedPropertyIds =
                propertyIds
                    .Where(propertyId =>
                        propertyId != Guid.Empty)
                    .Distinct()
                    .ToArray();

            if (normalizedPropertyIds.Length == 0)
            {
                return new Dictionary<
                    Guid,
                    PropertyRatingSnapshot>();
            }

            /*
             * Only reviews approved and published by the
             * administrator affect public ratings.
             */
            var ratingRows =
                await _dbContext.Reviews
                    .AsNoTracking()
                    .Where(review =>
                        normalizedPropertyIds.Contains(
                            review.PropertyId)
                        &&
                        review.Status ==
                            ReviewStatus.Posted)
                    .GroupBy(review =>
                        review.PropertyId)
                    .Select(group =>
                        new
                        {
                            PropertyId =
                                group.Key,

                            AverageRating =
                                group.Average(review =>
                                    (decimal)review.Rating),

                            ReviewsCount =
                                group.Count()
                        })
                    .ToListAsync(
                        cancellationToken);

            /*
             * Every requested property receives a result,
             * including properties that do not have reviews.
             */
            var result =
                normalizedPropertyIds
                    .ToDictionary(
                        propertyId =>
                            propertyId,

                        propertyId =>
                            new PropertyRatingSnapshot
                            {
                                PropertyId =
                                    propertyId,

                                AverageRating =
                                    0,

                                ReviewsCount =
                                    0
                            });

            foreach (var row in ratingRows)
            {
                result[row.PropertyId] =
                    new PropertyRatingSnapshot
                    {
                        PropertyId =
                            row.PropertyId,

                        AverageRating =
                            Math.Round(
                                row.AverageRating,
                                2,
                                MidpointRounding
                                    .AwayFromZero),

                        ReviewsCount =
                            row.ReviewsCount
                    };
            }

            return result;
        }
    }
}