using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MiApi.Dtos
{
    public enum  Role
    {
        User,
        Admin,
    }
    public class UserDto
    {
        public string? Id { get; set; } = null!;

        [Required]
        public string? Address { get; set; }

        [Required]
        public string? UserName { get; set; }

        [Required]
        public string? Email { get; set; }

        public bool EmailConfirmed { get; set; }

        [Required]
        public string? Password { get; set; }  

        [Required]
        public string? PhoneNumber { get; set; }

        public Role? Role { get; set; }

        public bool PhoneNumberConfirmed { get; set; }

        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }

        [Required]
        public string? FullName { get; set; }
    }
}