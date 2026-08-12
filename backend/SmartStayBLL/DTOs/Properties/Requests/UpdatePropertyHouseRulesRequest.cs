using System.ComponentModel.DataAnnotations;

namespace SmartStayBLL
{
    public sealed class UpdatePropertyHouseRulesRequest
    {
        [Required(
            ErrorMessage =
                "You must specify whether smoking is allowed.")]
        public bool? AllowsSmoking { get; set; }

        [Required(
            ErrorMessage =
                "You must specify whether pets are allowed.")]
        public bool? AllowsPets { get; set; }

        [Required(
            ErrorMessage =
                "You must specify whether parties are allowed.")]
        public bool? AllowsParties { get; set; }

        [Required(
            ErrorMessage =
                "You must specify whether children are allowed.")]
        public bool? AllowsChildren { get; set; }

        [StringLength(
            1000,
            ErrorMessage =
                "Additional house rules cannot exceed 1000 characters.")]
        public string? AdditionalHouseRules { get; set; }
    }
}