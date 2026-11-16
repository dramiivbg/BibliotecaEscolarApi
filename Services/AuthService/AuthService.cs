using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MiApi.Dtos;
using MiApi.Repositories;
using MiApi.Services.Auth.Jwt.Models;
using MiApi.Services.Auth.Jwt.Services;
using MiApi.Services.AuthService.Jwt.Models;
using MiApi.Services.EmailService;
using MiApi.Services.SmsService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using MiApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System.Net.WebSockets;
using MiApi.Data;
using Microsoft.EntityFrameworkCore;

namespace MiApi.Services.AuthService
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;

        private  readonly AppDbContext _context;

        private readonly DbSet<RefreshToken> _dbSet;


        private readonly JwtSettings _jwt;
        private readonly IEmailService _emailService;

        private readonly ITokenService _tokenService;

        private readonly ISmsService _smsService;

        private readonly IConfiguration _configuration;

        public AuthService(UserManager<User> userManager, IEmailService emailService, ISmsService smsService, IConfiguration configuration, ITokenService tokenService, IOptions<JwtSettings> options, AppDbContext context)
        {
            _userManager = userManager;
            _emailService = emailService;
            _smsService = smsService;
            _configuration = configuration;
            _tokenService = tokenService;
            _jwt = options.Value;
            _context = context;
            _dbSet = _context.Set<RefreshToken>();
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

        public async Task<AuthResponse> LoginAsync(LoginDto loginDto, string? ip)
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

            return await IssueTokensAsync(user, ip);
        }

        private async Task<AuthResponse> IssueTokensAsync(User user, string? ip, RefreshToken? replacing = null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var (access, accessExp) = _tokenService.CreateAccessToken(user, roles);
            var (refresh, refreshHash) = _tokenService.CreateRefreshToken();

            _dbSet.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays),
                CreatedByIp = ip
            });

            if (replacing is not null)
            {
                replacing.RevokedAt = DateTime.UtcNow;
                replacing.ReplacedByTokenHash = refreshHash;
            }

            await _context.SaveChangesAsync();
            return new AuthResponse(access, accessExp, refresh);
        }

        public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ip)
        {
            var hash = _tokenService.Hash(refreshToken);
            var stored = await _dbSet.FirstOrDefaultAsync(t => t.TokenHash == hash)
                ?? throw new UnauthorizedAccessException("Sesión inválida.");

            // Reutilización de un token ya rotado: posible robo -> cerrar todas las sesiones
            if (stored.RevokedAt is not null)
            {
                await RevokeAllAsync(stored.UserId);
                throw new UnauthorizedAccessException("Sesión inválida.");
            }

            if (stored.ExpiresAt <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Sesión expirada.");

            var user = await _userManager.FindByIdAsync(stored.UserId);
            if (user is null || await _userManager.IsLockedOutAsync(user))
                throw new UnauthorizedAccessException("Sesión inválida.");

            return await IssueTokensAsync(user, ip, replacing: stored);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var hash = _tokenService.Hash(refreshToken);
            var stored = await _dbSet.FirstOrDefaultAsync(t => t.TokenHash == hash);
            if (stored is { RevokedAt: null })
            {
                stored.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeAllAsync(string userId) =>
            await _dbSet
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow));


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