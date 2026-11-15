using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiApi.Dtos;
using Microsoft.AspNetCore.Identity;

namespace MiApi.Services.Auth
{
    public interface IAuthService
    {
        Task<string> LoginAsync(LoginDto loginDto);
        Task<string> RegisterAsync(UserDto user);

        Task<IdentityResult> ConfirmEmailAsync(string userId, string token);
    }
}