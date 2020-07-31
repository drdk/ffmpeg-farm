using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.WindowsService.Models
{
    public class ValidationJobRequestModel
    {
        [Required]
        public List<string> Filenames { get; set; }

        [Required]
        public DateTimeOffset Needed { get; set; }

    }
}
