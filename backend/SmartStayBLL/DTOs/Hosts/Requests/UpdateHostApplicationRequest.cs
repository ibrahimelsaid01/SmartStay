using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class UpdateHostApplicationRequest
    {
        [Required]
        [StringLength(
            80,
            MinimumLength = 3)]
        public string DisplayName { get; set; } =
            string.Empty;

        [Required]
        [StringLength(
            1000,
            MinimumLength = 20)]
        public string Bio { get; set; } =
            string.Empty;

        [Required]
        [StringLength(100)]
        public string Country { get; set; } =
            string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } =
            string.Empty;

        [Required]
        [RegularExpression(
            @"^\+?[0-9]{8,20}$",
            ErrorMessage =
                "Phone number must contain between 8 and 20 digits and may start with +.")]
        public string PhoneNumber { get; set; } =
            string.Empty;
    }
}