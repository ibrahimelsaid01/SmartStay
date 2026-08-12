using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class UpdatePropertyImageOrderRequest
    {
        [Required(
            ErrorMessage =
                "The image IDs collection is required.")]
        public List<Guid>? ImageIds { get; set; }
    }
}