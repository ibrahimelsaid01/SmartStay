namespace SmartStayDAL
{
    public sealed class Amenity
    {
        public Guid Id { get; set; }

        public string Code { get; set; } =
            string.Empty;

        public string Name { get; set; } =
            string.Empty;

        public AmenityCategory Category { get; set; }

        public string IconKey { get; set; } =
            string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } =
            true;

        public ICollection<PropertyAmenity>
            PropertyAmenities
        { get; set; } =
                new List<PropertyAmenity>();
    }
}