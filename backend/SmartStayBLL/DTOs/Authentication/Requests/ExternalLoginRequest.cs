using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class ExternalLoginRequest
    {
        [Required(ErrorMessage = "Provider is required.")]
        [RegularExpression(
            "^(Google|Facebook)$",
            ErrorMessage = "Provider must be Google, Facebook")]
        public string Provider { get; set; } = string.Empty;

        [Required(ErrorMessage = "External authentication token is required.")]
        public string Token { get; set; } = string.Empty;
    }
}