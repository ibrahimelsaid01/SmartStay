using System.ComponentModel.DataAnnotations;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class UpdatePropertyPricingAndPoliciesRequest
    {
        [Required]
        [Range(
            typeof(decimal),
            "0.01",
            "9999999999999999.99",
            ErrorMessage =
                "Price per night must be greater than zero.")]
        public decimal? PricePerNight { get; set; }

        [Required]
        [StringLength(
            3,
            MinimumLength = 3,
            ErrorMessage =
                "Currency must contain exactly 3 characters.")]
        [RegularExpression(
            @"^[A-Za-z]{3}$",
            ErrorMessage =
                "Currency must contain exactly 3 English letters.")]
        public string Currency { get; set; } =
            "EGP";

        [Required]
        public TimeOnly? CheckInTime { get; set; }

        [Required]
        public TimeOnly? CheckOutTime { get; set; }

        [Required]
        [EnumDataType(
            typeof(CancellationPolicyType),
            ErrorMessage =
                "The selected cancellation policy is invalid.")]
        public CancellationPolicyType?
            CancellationPolicy
        { get; set; }
    }
}