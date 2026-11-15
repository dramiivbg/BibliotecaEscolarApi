using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MiApi.Dtos;
using MiApi.Services.Email;
using MiApi.Services.Sms;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;

namespace MiApi.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<Models.User> _userManager;


        private readonly IEmailService _emailService;

        private readonly ISmsService _smsService;

        private readonly IConfiguration _configuration;

        public AuthService(UserManager<Models.User> userManager, IEmailService emailService, ISmsService smsService, IConfiguration configuration)
        {
            _userManager = userManager;
            _emailService = emailService;
            _smsService = smsService;
            _configuration = configuration;
        }

        public async Task<IdentityResult> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidUser",
                    Description = "Usuario inválido."
                });

            // Si ya estaba confirmado, no hay nada que hacer
            if (user.EmailConfirmed)
                return IdentityResult.Success;

            string tokenDecoded;
            try
            {
                tokenDecoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch (FormatException)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidToken",
                    Description = "El enlace es inválido."
                });
            }

            return await _userManager.ConfirmEmailAsync(user, tokenDecoded);
        }

        public async Task<string> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email!);

            // Mismo mensaje para "no existe" y "contraseña incorrecta" (evita enumeración de usuarios)
            if (user is null)
                throw new UnauthorizedAccessException("Credenciales inválidas.");

            if (await _userManager.IsLockedOutAsync(user))
                throw new UnauthorizedAccessException("Cuenta bloqueada temporalmente. Intenta más tarde.");

            bool checkPassword = await _userManager.CheckPasswordAsync(user, loginDto.Password!);
            Console.WriteLine(user.Email);

            if (!checkPassword)
            {
                await _userManager.AccessFailedAsync(user); // cuenta intentos fallidos para el lockout
                throw new UnauthorizedAccessException("Credenciales inválidas.");
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
                throw new UnauthorizedAccessException("Debes confirmar tu correo antes de iniciar sesión.");

            await _userManager.ResetAccessFailedCountAsync(user);

            return await GenerateJwtAsync(user);
        }

        private async Task<string> GenerateJwtAsync(Models.User user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email!),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.FullName ?? user.Email!)
    };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "60")),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public async Task<string> RegisterAsync(UserDto user)
        {
            var newUser = new Models.User
            {
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = false,
                LockoutEnabled = true,
            };

            var result = await _userManager.CreateAsync(newUser, user.Password!);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            var roleResult = await _userManager.AddToRoleAsync(newUser, "User");
            if (!roleResult.Succeeded)
            {
                throw new Exception(string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
            var tokenEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var link = $"{_configuration["App:FrontendUrl"]}/confirm-email" +
                       $"?userId={Uri.EscapeDataString(newUser.Id)}&token={tokenEncoded}";

            await _emailService.SendAsync(
                newUser.Email!,
                "Confirma tu correo",
                $"<p>Haz clic <a href='{link}'>aquí</a> para confirmar tu correo.</p>");

            return "Registro exitoso. Revisa tu correo para confirmar la cuenta.";
        }
    }
}