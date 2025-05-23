using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UrlHealthCheckFunction.Models
{
    public class InputModel
    {
        [Required]
        public List<string> Urls { get; set; }
    }
}
