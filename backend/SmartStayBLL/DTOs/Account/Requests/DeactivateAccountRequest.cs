using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class DeactivateAccountRequest
    {
        [Required(
            ErrorMessage =
                "Account deactivation confirmation is required.")]
        public string Confirmation { get; set; } =
            string.Empty;
    }
}