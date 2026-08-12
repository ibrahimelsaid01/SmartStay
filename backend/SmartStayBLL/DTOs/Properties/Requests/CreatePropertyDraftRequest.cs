using System.ComponentModel.DataAnnotations;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class CreatePropertyDraftRequest
    {
        [Required]
        [StringLength(
            120,
            MinimumLength = 10,
            ErrorMessage =
                "Property title must contain between 10 and 120 characters.")]
        public string Title { get; set; } =
            string.Empty;

        [Required]
        [StringLength(
            3000,
            MinimumLength = 100,
            ErrorMessage =
                "Property description must contain between 100 and 3000 characters.")]
        public string Description { get; set; } =
            string.Empty;

        [Required]
        [EnumDataType(
            typeof(PropertyType),
            ErrorMessage = "The selected property type is invalid.")]
        public PropertyType? PropertyType { get; set; }

        [Required]
        [EnumDataType(
            typeof(PropertySpaceType),
            ErrorMessage = "The selected property space type is invalid.")]
        public PropertySpaceType? SpaceType { get; set; }
    }
}