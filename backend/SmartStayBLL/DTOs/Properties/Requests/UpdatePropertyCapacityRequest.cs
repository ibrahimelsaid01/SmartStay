using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class UpdatePropertyCapacityRequest
    {
        [Required]
        [Range(
            1,
            20,
            ErrorMessage =
                "Maximum guests must be between 1 and 20.")]
        public int? MaxGuests { get; set; }

        [Required]
        [Range(
            0,
            20,
            ErrorMessage =
                "Bedrooms must be between 0 and 20.")]
        public int? Bedrooms { get; set; }

        [Required]
        [Range(
            1,
            30,
            ErrorMessage =
                "Beds must be between 1 and 30.")]
        public int? Beds { get; set; }

        [Required]
        [Range(
            typeof(decimal),
            "0.5",
            "20",
            ErrorMessage =
                "Bathrooms must be between 0.5 and 20.")]
        public decimal? Bathrooms { get; set; }
    }
}