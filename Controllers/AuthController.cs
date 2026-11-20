using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MiApi.Dtos;
using MiApi.Services.AuthService;
using MiApi.Services.AuthService.Jwt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace MiApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        private const string RefreshCookie = "refreshToken";

        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        private void SetRefreshCookie(string refreshToken) =>
        Response.Cookies.Append(RefreshCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7),   // igual que RefreshTokenDays
            Path = "/api/auth"
        });

        private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();



        // POST api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] UserDto user)
        {
            try
            {
                var message = await _authService.RegisterAsync(user);
                return Ok(new { message });
            }
            catch (RegistrationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var auth = await _authService.LoginAsync(loginDto, HttpContext.Connection.RemoteIpAddress?.ToString());

                SetRefreshCookie(auth.RefreshToken);

                // El refresh token NO va en el JSON
                return Ok(new { accessToken = auth.AccessToken, expiresAt = auth.AccessTokenExpiresAt });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies[RefreshCookie];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new { error = "Sesión inválida." });

            var auth = await _authService.RefreshAsync(refreshToken, Ip);

            SetRefreshCookie(auth.RefreshToken);   // rotación: se reemplaza la cookie
            return Ok(new { accessToken = auth.AccessToken, expiresAt = auth.AccessTokenExpiresAt });
        }


        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies[RefreshCookie];
            if (!string.IsNullOrEmpty(refreshToken))
                await _authService.LogoutAsync(refreshToken);

            Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/auth" });
            return NoContent();
        }

        public record RefreshRequest([Required] string RefreshToken);

        // GET api/auth/confirm-email?userId=...&token=...
        [HttpGet("confirm-email")]
        [AllowAnonymous]
        [Produces("text/html")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            var result = await _authService.ConfirmEmailAsync(userId, token);

            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return Content(BuildPage(false, "No se pudo confirmar", error), "text/html; charset=utf-8");
            }

            return Content(
                BuildPage(true, "¡Correo confirmado!", "Tu cuenta ya está activa. Ya puedes iniciar sesión."),
                "text/html; charset=utf-8");
        }

        private string BuildPage(bool success, string title, string message)
        {
            var color = success ? "#16a34a" : "#dc2626";
            var icon = success ? "✓" : "✕";
            var loginUrl = WebUtility.HtmlEncode(_configuration["App:FrontendUrl"] + "/login");

            return $$"""
    <!DOCTYPE html>
    <html lang="es">
    <head>
      <meta charset="utf-8">
      <meta name="viewport" content="width=device-width, initial-scale=1">
      <title>{{WebUtility.HtmlEncode(title)}}</title>
      <style>
        * { box-sizing: border-box; }
        body {
          margin: 0; min-height: 100vh; display: flex;
          align-items: center; justify-content: center;
          background: #f3f4f6; font-family: system-ui, sans-serif;
        }
        .card {
          background: #fff; max-width: 420px; width: 90%;
          padding: 40px 32px; border-radius: 16px; text-align: center;
          box-shadow: 0 10px 30px rgba(0,0,0,.1);
        }
        .icon {
          width: 72px; height: 72px; margin: 0 auto 20px;
          border-radius: 50%; background: {{color}}; color: #fff;
          font-size: 40px; line-height: 72px;
        }
        h1 { margin: 0 0 12px; font-size: 24px; color: #111827; }
        p { margin: 0 0 28px; color: #6b7280; line-height: 1.5; }
        a.btn {
          display: inline-block; padding: 12px 28px; border-radius: 8px;
          background: {{color}}; color: #fff; text-decoration: none; font-weight: 600;
        }
      </style>
    </head>
    <body>
      <div class="card">
        <div class="icon">{{icon}}</div>
        <h1>{{WebUtility.HtmlEncode(title)}}</h1>
        <p>{{WebUtility.HtmlEncode(message)}}</p>
        <a class="btn" href="{{loginUrl}}">Ir a iniciar sesión</a>
      </div>
    </body>
    </html>
    """;
        }
    }
}