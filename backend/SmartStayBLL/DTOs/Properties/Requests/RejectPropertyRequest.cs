using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class RejectPropertyRequest
    {
        [Required(
            ErrorMessage =
                "The rejection reason is required.")]
        [MinLength(
            10,
            ErrorMessage =
                "The rejection reason must contain at least 10 characters.")]
        [MaxLength(
            500,
            ErrorMessage =
                "The rejection reason cannot exceed 500 characters.")]
        public string? Reason { get; set; }
    }
}