using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiApi.Dtos;
namespace MiApi.Services.UserService
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync(string? role = null, string? email = null);
        Task<UserDto?> GetByIdAsync(string id);

        Task<UserDto> AddAsync(UserDto user);

        Task<UserDto> UpdateAsync(UserDto user);

        Task<bool> ActivateAsync(string id, bool activate);
    }
}