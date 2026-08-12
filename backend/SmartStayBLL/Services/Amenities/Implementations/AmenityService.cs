using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class AmenityService
        : IAmenityService
    {
        private readonly SmartStayDbContext
            _dbContext;

        public AmenityService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext = dbContext;
        }

        public async Task<
            IReadOnlyList<AmenityResponse>>
            GetActiveAmenitiesAsync(
                CancellationToken cancellationToken =
                    default)
        {
            var amenities =
                await _dbContext.Amenities
                    .AsNoTracking()
                    .Where(amenity =>
                        amenity.IsActive)
                    .OrderBy(amenity =>
                        amenity.Category)
                    .ThenBy(amenity =>
                        amenity.DisplayOrder)
                    .ThenBy(amenity =>
                        amenity.Name)
                    .ToListAsync(
                        cancellationToken);

            return amenities
                .Select(MapToAmenityResponse)
                .ToList();
        }

        private static AmenityResponse
            MapToAmenityResponse(
                Amenity amenity)
        {
            return new AmenityResponse
            {
                Id =
                    amenity.Id,

                Code =
                    amenity.Code,

                Name =
                    amenity.Name,

                Category =
                    amenity.Category.ToString(),

                IconKey =
                    amenity.IconKey,

                DisplayOrder =
                    amenity.DisplayOrder
            };
        }
    }
}