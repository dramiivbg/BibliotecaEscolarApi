using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiApi.Models
{
    public class Book
    {
        public int Id { get; set; }

        public string? Title { get; set; }

        public string? Author { get; set; }

        public string? Description { get; set; }

        public DateTime PublishedDate { get; set; }

        public int Stock { get; set; }

        public int Pages { get; set; }

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        
    }
}