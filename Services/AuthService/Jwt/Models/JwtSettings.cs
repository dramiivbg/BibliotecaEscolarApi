using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MiApi.Services.Auth.Jwt.Models
{
    public class JwtSettings
    {
        [Required, MinLength(32)] public string Key { get; set; } = string.Empty;
        [Required] public string Issuer { get; set; } = string.Empty;
        [Required] public string Audience { get; set; } = string.Empty;
        [Range(1, 120)] public int AccessTokenMinutes { get; set; } = 15;
        [Range(1, 90)] public int RefreshTokenDays { get; set; } = 7;
    }
}