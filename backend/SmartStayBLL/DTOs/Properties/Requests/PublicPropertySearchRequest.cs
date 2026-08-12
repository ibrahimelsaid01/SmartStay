using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class PublicPropertySearchRequest
    {
        public string? Search { get; set; }

        public string? City { get; set; }

        public PropertyType? PropertyType { get; set; }

        public PropertySpaceType? SpaceType { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public int? MinGuests { get; set; }

        /*
         * Both dates are optional.
         *
         * However, if one date is provided,
         * the other date must also be provided.
         */
        public DateOnly? CheckInDate { get; set; }

        public DateOnly? CheckOutDate { get; set; }

        public PublicPropertySortOption Sort { get; set; } =
            PublicPropertySortOption.Newest;

        public int Page { get; set; } =
            1;

        public int PageSize { get; set; } =
            12;
    }
}