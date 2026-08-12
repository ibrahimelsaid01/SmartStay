using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class RejectHostApplicationRequest
    {
        [Required]
        [StringLength(
            500,
            MinimumLength = 10,
            ErrorMessage =
                "Rejection reason must contain between 10 and 500 characters.")]
        public string Reason { get; set; } =
            string.Empty;
    }
}