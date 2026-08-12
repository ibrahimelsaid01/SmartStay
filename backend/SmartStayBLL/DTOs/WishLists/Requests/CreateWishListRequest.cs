using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class CreateWishListRequest
    {
        [Required(
            ErrorMessage = "The wish list name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage =
                "The wish list name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}