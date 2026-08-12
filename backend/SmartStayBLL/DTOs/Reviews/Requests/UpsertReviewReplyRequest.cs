using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class UpsertReviewReplyRequest
    {
        [Required(
            ErrorMessage =
                "The reply content is required.")]
        [StringLength(
            2000,
            MinimumLength = 2,
            ErrorMessage =
                "The reply must be between 2 and 2000 characters.")]
        public string Content { get; set; } =
            string.Empty;
    }
}