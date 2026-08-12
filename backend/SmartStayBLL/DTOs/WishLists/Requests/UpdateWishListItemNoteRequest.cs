using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class UpdateWishListItemNoteRequest
    {
        [StringLength(
            500,
            ErrorMessage =
                "The note cannot exceed 500 characters.")]
        public string? Note { get; set; }
    }
}