using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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

    // Get api/auth/confirm-email?userId=12344&token=awdwwww..
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var result = await _authService.ConfirmEmailAsync(userId, token);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Correo confirmado correctamente." });
    }
    }
}