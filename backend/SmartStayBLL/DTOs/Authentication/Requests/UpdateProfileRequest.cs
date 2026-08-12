using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public class UpdateProfileRequest
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "First name can only contain letters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Last name can only contain letters.")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string? PhoneNumber { get; set; }

        [RegularExpression(@"^(Male|Female)$", ErrorMessage = "Gender must be either Male, Female.")]
        public string? Gender { get; set; }

        
        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }

        [StringLength(100, ErrorMessage = "Country name cannot exceed 100 characters.")]
        public string? Country { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string? Address { get; set; }

        [StringLength(10, ErrorMessage = "Zip code cannot exceed 10 characters.")]
        [RegularExpression(@"^[0-9a-zA-Z\-]*$", ErrorMessage = "Invalid Zip code format.")]
        public string? ZipCode { get; set; }
        public IFormFile? ProfileImage { get; set; }
    }
}

