using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiApi.Dtos;
using MiApi.Repositories;
using MiApi.Services.EmailService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace MiApi.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly UserManager<Models.User> _userManager;

        private readonly IConfiguration _configuration;


        private readonly IEmailService _emailService;
        public UserService(UserManager<Models.User> userManager, IConfiguration configuration, IEmailService emailService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
        }
        public async Task<UserDto> AddAsync(UserDto user)
        {
            var newUser = new Models.User
            {
                UserName = user.Email,
                Email = user.Email,
                FullName = user.FullName,
                Address = user.Address,
            };

            var result = await _userManager.CreateAsync(newUser, user.Password!);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            string role = user.Role > 0 ? "Admin" : "User";
            var roleResult = await _userManager.AddToRoleAsync(newUser, role);
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

            return new UserDto
            {
                Id = newUser.Id,
                Email = newUser.Email,
                FullName = newUser.FullName,
                Address = newUser.Address,
                Role = user.Role,
                EmailConfirmed = newUser.EmailConfirmed,
                LockoutEnabled = newUser.LockoutEnabled,
                AccessFailedCount = newUser.AccessFailedCount,
                LockoutEnd = newUser.LockoutEnd,
                UserName = newUser.UserName,
                PhoneNumber = newUser.PhoneNumber
            };
        }

        public async Task<bool> ActivateAsync(string id, bool activate)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user is null)
                return false;

            // Requisito: el lockout debe estar habilitado para este usuario
            await _userManager.SetLockoutEnabledAsync(user, true);

            var result = activate
                ? await _userManager.SetLockoutEndDateAsync(user, null)                       // habilitar
                : await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);   // deshabilitar

            if (activate)
                await _userManager.ResetAccessFailedCountAsync(user);

            if (!result.Succeeded)
                throw new RegistrationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            // Invalida tokens/cookies antiguos ligados al security stamp
            await _userManager.UpdateSecurityStampAsync(user);

            return true;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync(string? role = null, string? email = null)
        {
            IEnumerable<Models.User> users;

            if (!string.IsNullOrWhiteSpace(role))
                users = await _userManager.GetUsersInRoleAsync(role);
            else
                users = await _userManager.Users.AsNoTracking().ToListAsync();

            if (!string.IsNullOrWhiteSpace(email))
                users = users.Where(u => u.Email!.Contains(email, StringComparison.OrdinalIgnoreCase));

            var result = new List<UserDto>();
            foreach (var u in users) // secuencial, no paralelo
            {
                var roles = await _userManager.GetRolesAsync(u);
                result.Add(new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Address = u.Address,
                    Role = roles.Contains("Admin") ? Role.Admin : Role.User,
                    EmailConfirmed = u.EmailConfirmed,
                    LockoutEnabled = u.LockoutEnabled,
                    AccessFailedCount = u.AccessFailedCount,
                    LockoutEnd = u.LockoutEnd,
                    UserName = u.UserName,
                    PhoneNumber = u.PhoneNumber
                });
            }

            return result;
        }

        public async Task<UserDto?> GetByIdAsync(string id)
        {
            var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Address = user.Address,
                Role = roles.Contains("Admin") ? Role.Admin : Role.User,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                AccessFailedCount = user.AccessFailedCount,
                LockoutEnd = user.LockoutEnd,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber
            };
        }

        public async Task<UserDto> UpdateAsync(UserDto user)
        {
            var existingUser = await _userManager.FindByIdAsync(user.Id);
            if (existingUser is null)
                throw new ApplicationException("Usuario no encontrado.");

            existingUser.FullName = user.FullName;
            existingUser.Address = user.Address;
            existingUser.PhoneNumber  = user.PhoneNumber;

            var result = await _userManager.UpdateAsync(existingUser);
            if (!result.Succeeded)
                throw new ApplicationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            // Actualizar rol si es necesario
            var currentRoles = await _userManager.GetRolesAsync(existingUser);
            string newRole = user.Role > 0 ? "Admin" : "User";

            if (!currentRoles.Contains(newRole))
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(existingUser, currentRoles);
                if (!removeResult.Succeeded)
                    throw new ApplicationException(string.Join(", ", removeResult.Errors.Select(e => e.Description)));

                var addResult = await _userManager.AddToRoleAsync(existingUser, newRole);
                if (!addResult.Succeeded)
                    throw new ApplicationException(string.Join(", ", addResult.Errors.Select(e => e.Description)));
            }

            return new UserDto
            {
                Id = existingUser.Id,
                Email = existingUser.Email,
                FullName = existingUser.FullName,
                Address = existingUser.Address,
                Role = user.Role,
                EmailConfirmed = existingUser.EmailConfirmed,
                LockoutEnabled = existingUser.LockoutEnabled,
                AccessFailedCount = existingUser.AccessFailedCount,
                LockoutEnd = existingUser.LockoutEnd,
                UserName = existingUser.UserName,
                PhoneNumber = existingUser.PhoneNumber
            };
        }
    }
}