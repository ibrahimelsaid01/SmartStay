using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class RejectReviewRequest
    {
        [Required(
            ErrorMessage =
                "The rejection reason is required.")]
        [StringLength(
            500,
            MinimumLength = 3,
            ErrorMessage =
                "The rejection reason must be between 3 and 500 characters.")]
        public string Reason { get; set; } =
            string.Empty;
    }
}