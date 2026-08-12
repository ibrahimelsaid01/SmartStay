namespace SmartStayDAL
{
    public sealed class Property
    {
        public Guid Id { get; set; }

        public Guid HostProfileId { get; set; }

        /*
         * Basic information.
         * These fields are required when the Draft
         * is created for the first time.
         */
        public string Title { get; set; } =
            string.Empty;

        public string Description { get; set; } =
            string.Empty;

        public PropertyType PropertyType { get; set; }

        public PropertySpaceType SpaceType { get; set; }

        /*
         * Capacity information.
         * Nullable because a Draft can exist before
         * the host completes this step.
         */
        public int? MaxGuests { get; set; }

        public int? Bedrooms { get; set; }

        public int? Beds { get; set; }

        public decimal? Bathrooms { get; set; }

        /*
         * Pricing.
         */
        public decimal? PricePerNight { get; set; }

        public string Currency { get; set; } =
            "EGP";

        /*
         * Location.
         * The exact address remains private and is not
         * exposed directly in the public listing.
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

        public TimeOnly? CheckInTime { get; set; }

        public TimeOnly? CheckOutTime { get; set; }

        public CancellationPolicyType? CancellationPolicy { get; set; } =
                CancellationPolicyType.Moderate;


        public bool? AllowsSmoking { get; set; }

        public bool? AllowsPets { get; set; }

        public bool? AllowsParties { get; set; }

        public bool? AllowsChildren { get; set; }

        public string? AdditionalHouseRules { get; set; }



        public PropertyStatus Status { get; set; } =
            PropertyStatus.Draft;

        public string? RejectionReason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? SubmittedAt { get; set; }

        public DateTimeOffset? ReviewedAt { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }

        /*
         * Navigation properties.
         */
        public HostProfile HostProfile { get; set; } =
            null!;

        public ICollection<PropertyImage> Images
        {
            get;
            set;
        } = new List<PropertyImage>();

        public ICollection<PropertyAmenity>
            PropertyAmenities
        { get; set; } =
                new List<PropertyAmenity>();

        public PropertyVerificationDocument?
            VerificationDocument
        { get; set; }
    }
}