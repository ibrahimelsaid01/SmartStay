using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class UpdateReviewRequest
    {
        [Range(
            1,
            5,
            ErrorMessage =
                "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [StringLength(
            2000,
            ErrorMessage =
                "Positive comment cannot exceed 2000 characters.")]
        public string? PositiveComment { get; set; }

        [StringLength(
            2000,
            ErrorMessage =
                "Negative comment cannot exceed 2000 characters.")]
        public string? NegativeComment { get; set; }
    }
}