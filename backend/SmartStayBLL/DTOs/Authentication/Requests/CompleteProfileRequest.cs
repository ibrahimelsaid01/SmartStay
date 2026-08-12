using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class CompleteProfileRequest
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "First name must be between 2 and 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage = "Last name must be between 2 and 100 characters.")]
        public string LastName { get; set; } = string.Empty;
    }
}