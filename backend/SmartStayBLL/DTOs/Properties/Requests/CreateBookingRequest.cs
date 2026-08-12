using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class CreateBookingRequest
    {
        public Guid PropertyId { get; set; }

        [Required(
            ErrorMessage =
                "The check-in date is required.")]
        public DateOnly? CheckInDate { get; set; }

        [Required(
            ErrorMessage =
                "The check-out date is required.")]
        public DateOnly? CheckOutDate { get; set; }

        [Range(
            1,
            20,
            ErrorMessage =
                "The guests count must be between 1 and 20.")]
        public int GuestsCount { get; set; }

        public bool AcceptedBookingTerms { get; set; }

        public bool AcceptedCancellationPolicy { get; set; }

        public bool AcceptedPropertyRules { get; set; }

        public bool AcceptedComplaintPolicy { get; set; }
    }
}