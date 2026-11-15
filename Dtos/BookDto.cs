using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MiApi.Dtos
{
    public class BookDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string? Title { get; set; }

        [Required]
        public string? Author { get; set; }

        [Required]
        public string? Description { get; set; }


        [Required]
        public DateTime PublishedDate { get; set; }

        [Required]
        public int Stock { get; set; }

        [Required]
        public int Pages { get; set; }
    }
}