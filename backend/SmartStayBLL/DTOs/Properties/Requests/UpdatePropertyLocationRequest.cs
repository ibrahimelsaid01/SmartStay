using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class UpdatePropertyLocationRequest
    {
        [Required]
        [StringLength(
            100,
            ErrorMessage =
                "Country cannot exceed 100 characters.")]
        public string Country { get; set; } =
            string.Empty;

        [Required]
        [StringLength(
            100,
            ErrorMessage =
                "City cannot exceed 100 characters.")]
        public string City { get; set; } =
            string.Empty;

        [Required]
        [StringLength(
            250,
            ErrorMessage =
                "Street address cannot exceed 250 characters.")]
        public string StreetAddress { get; set; } =
            string.Empty;

        [StringLength(
            30,
            ErrorMessage =
                "Building number cannot exceed 30 characters.")]
        public string? BuildingNumber { get; set; }

        [StringLength(
            30,
            ErrorMessage =
                "Floor cannot exceed 30 characters.")]
        public string? Floor { get; set; }

        [StringLength(
            30,
            ErrorMessage =
                "Apartment number cannot exceed 30 characters.")]
        public string? ApartmentNumber { get; set; }

        [StringLength(
            20,
            ErrorMessage =
                "Postal code cannot exceed 20 characters.")]
        public string? PostalCode { get; set; }

        [Required]
        [Range(
            typeof(decimal),
            "-90",
            "90",
            ErrorMessage =
                "Latitude must be between -90 and 90.")]
        public decimal? Latitude { get; set; }

        [Required]
        [Range(
            typeof(decimal),
            "-180",
            "180",
            ErrorMessage =
                "Longitude must be between -180 and 180.")]
        public decimal? Longitude { get; set; }
    }
}