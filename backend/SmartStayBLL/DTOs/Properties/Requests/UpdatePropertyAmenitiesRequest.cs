using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class UpdatePropertyAmenitiesRequest
    {
        [Required(
            ErrorMessage =
                "The amenity IDs collection is required.")]
        public List<Guid>? AmenityIds { get; set; }
    }
}