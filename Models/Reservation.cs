using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiApi.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        public int BookId { get; set; }

        public DateTime ReservationDate { get; set; }

        public DateTime DueDate { get; set; }

        public bool IsReturned { get; set; }

        public User User { get; set; } = null!;

        public Book Book { get; set; } = null!;
    }
}