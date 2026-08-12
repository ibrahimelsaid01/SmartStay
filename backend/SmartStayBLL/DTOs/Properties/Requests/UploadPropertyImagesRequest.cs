using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SmartStayBLL
{
    public sealed class UploadPropertyImagesRequest
    {
        [Required(
            ErrorMessage =
                "At least one image is required.")]
        public List<IFormFile>? Files { get; set; }
    }
}