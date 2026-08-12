using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class VerifyOtpRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email format is invalid.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP code is required.")]
        [StringLength(
            6,
            MinimumLength = 6,
            ErrorMessage = "OTP code must contain exactly 6 digits.")]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "OTP code must contain digits only.")]
        public string Code { get; set; } = string.Empty;
    }
}