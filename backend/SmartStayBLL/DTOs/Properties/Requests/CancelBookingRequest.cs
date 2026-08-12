using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class CancelBookingRequest
    {
        [MaxLength(
            500,
            ErrorMessage =
                "The cancellation reason cannot exceed 500 characters.")]
        public string? Reason { get; set; }
    }
}