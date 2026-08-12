using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class
        UploadPropertyVerificationDocumentRequest
    {
        [Required]
        [EnumDataType(
            typeof(PropertyVerificationDocumentType),
            ErrorMessage =
                "The selected verification document type is invalid.")]
        public PropertyVerificationDocumentType?
            DocumentType
        { get; set; }

        [Required(
            ErrorMessage =
                "At least one document page is required.")]
        public List<IFormFile>? Files { get; set; }
    }
}