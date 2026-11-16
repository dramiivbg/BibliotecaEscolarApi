using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiApi.Models;

namespace MiApi.Services.Auth.Jwt.Services
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAt) CreateAccessToken(User user, IEnumerable<string> roles);
        (string Token, string Hash) CreateRefreshToken();
        string Hash(string token);
    }
}