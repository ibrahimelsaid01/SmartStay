namespace SmartStay.Api
{
    public sealed class UploadHostNationalIdForm
    {
        public IFormFile? FrontFile { get; set; }

        public IFormFile? BackFile { get; set; }
    }
}