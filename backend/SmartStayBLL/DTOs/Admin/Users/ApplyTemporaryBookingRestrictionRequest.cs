using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class ApplyTemporaryBookingRestrictionRequest
    {
        [Range(
            1,
            90,
            ErrorMessage =
                "Temporary booking suspension duration must be between 1 and 90 days.")]
        public int DurationDays { get; set; }

        [Required(
            ErrorMessage =
                "Temporary booking suspension reason is required.")]
        [StringLength(
            1000,
            MinimumLength = 10,
            ErrorMessage =
                "Temporary booking suspension reason must contain between 10 and 1000 characters.")]
        public string Reason { get; set; } =
            string.Empty;
    }
}