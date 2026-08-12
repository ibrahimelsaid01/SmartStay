using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SmartStayBLL
{
    public class RequestEmailChangeDto
    {
        [Required]
        [EmailAddress]
        public string NewEmail { get; set; } = string.Empty;
    }
}
