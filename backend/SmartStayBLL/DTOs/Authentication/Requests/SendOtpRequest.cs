using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class SendOtpRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email format is invalid.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;
    }
}