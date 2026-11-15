using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MiApi.Models
{
    public class User : IdentityUser
    {

        public string? Address { get; set; }

        public string? FullName { get; set; }

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    }
}