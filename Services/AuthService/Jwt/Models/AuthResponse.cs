using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiApi.Services.AuthService.Jwt.Models
{
    public record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken);
}