using System.ComponentModel.DataAnnotations;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class UpdateUserProfileRequest
    {
        [Required(
            ErrorMessage =
                "First name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage =
                "First name must be between 2 and 100 characters.")]
        public string FirstName { get; set; }
            = string.Empty;

        [Required(
            ErrorMessage =
                "Last name is required.")]
        [StringLength(
            100,
            MinimumLength = 2,
            ErrorMessage =
                "Last name must be between 2 and 100 characters.")]
        public string LastName { get; set; }
            = string.Empty;

        [Phone(
            ErrorMessage =
                "Phone number format is invalid.")]
        [StringLength(
            30,
            ErrorMessage =
                "Phone number cannot exceed 30 characters.")]
        public string? PhoneNumber { get; set; }

        [EnumDataType(
            typeof(UserGender),
            ErrorMessage =
                "The selected gender is invalid.")]
        public UserGender? Gender { get; set; }

        public DateOnly? Birthday { get; set; }

        [StringLength(
            100,
            ErrorMessage =
                "Country cannot exceed 100 characters.")]
        public string? Country { get; set; }

        [StringLength(
            300,
            ErrorMessage =
                "Address cannot exceed 300 characters.")]
        public string? Address { get; set; }

        [StringLength(
            20,
            ErrorMessage =
                "Zip code cannot exceed 20 characters.")]
        public string? ZipCode { get; set; }
    }
}