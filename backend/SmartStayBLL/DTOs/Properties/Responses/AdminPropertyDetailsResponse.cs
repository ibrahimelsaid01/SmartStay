namespace SmartStayBLL
{
    public sealed class AdminPropertyDetailsResponse
    {
        public Guid Id { get; set; }

        public string Title { get; set; } =
            string.Empty;

        public string Description { get; set; } =
            string.Empty;

        public string PropertyType { get; set; } =
            string.Empty;

        public string SpaceType { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public AdminPropertyHostResponse Host { get; set; } =
            new();

        /*
         * Location
         */

        public string? Country { get; set; }

        public string? City { get; set; }

        public string? StreetAddress { get; set; }

        public string? BuildingNumber { get; set; }

        public string? Floor { get; set; }

        public string? ApartmentNumber { get; set; }

        public string? PostalCode { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        /*
         * Capacity
         */

        public int? MaxGuests { get; set; }

        public int? Bedrooms { get; set; }

        public int? Beds { get; set; }

        public decimal? Bathrooms { get; set; }

        /*
         * Pricing and policies
         */

        public decimal? PricePerNight { get; set; }

        public string Currency { get; set; } =
            string.Empty;

        public TimeOnly? CheckInTime { get; set; }

        public TimeOnly? CheckOutTime { get; set; }

        public string? CancellationPolicy { get; set; }

        /*
         * House rules
         */

        public bool? AllowsSmoking { get; set; }

        public bool? AllowsPets { get; set; }

        public bool? AllowsParties { get; set; }

        public bool? AllowsChildren { get; set; }

        public string? AdditionalHouseRules { get; set; }

        /*
         * Amenities and images
         */

        public IReadOnlyList<AdminPropertyAmenityResponse>
            Amenities
        { get; set; } =
                Array.Empty<AdminPropertyAmenityResponse>();

        public IReadOnlyList<AdminPropertyImageResponse>
            Images
        { get; set; } =
                Array.Empty<AdminPropertyImageResponse>();

        public AdminPropertyVerificationDocumentResponse?
            VerificationDocument
        { get; set; }

        /*
         * Review information
         */

        public string? RejectionReason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? SubmittedAt { get; set; }

        public DateTimeOffset? ReviewedAt { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }
    }
}