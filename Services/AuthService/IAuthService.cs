using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiApi.Dtos;
using MiApi.Services.AuthService.Jwt.Models;
using Microsoft.AspNetCore.Identity;

namespace MiApi.Services.AuthService
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginDto loginDto, string? ip);
        Task<string> RegisterAsync(UserDto user);

        Task<IdentityResult> ConfirmEmailAsync(string userId, string token);

        Task<AuthResponse> RefreshAsync(string refreshToken, string? ip);

        Task LogoutAsync(string refreshToken);

        Task RevokeAllAsync(string userId);


    }
}