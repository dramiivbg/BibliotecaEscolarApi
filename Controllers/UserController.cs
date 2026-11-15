using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MiApi.Dtos;
using MiApi.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET api/user?role=Admin&email=...
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll(
            [FromQuery] string? role,
            [FromQuery] string? email)
        {
            var users = await _userService.GetAllAsync(role, email);
            return Ok(users);
        }

        // GET api/user/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetById(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            return user is null ? NotFound() : Ok(user);
        }

        // POST api/user
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> Add([FromBody] UserDto user)
        {
            try
            {
                var created = await _userService.AddAsync(user);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (RegistrationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT api/user/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> Update(string id, [FromBody] UserDto user)
        {
            if (id != user.Id)
                return BadRequest(new { error = "El id de la ruta no coincide con el del usuario." });

            try
            {
                var updated = await _userService.UpdateAsync(user);
                return Ok(updated);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (RegistrationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET api/user/{id}/enable
        [HttpGet("{id}/enable")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Enable(string id)
        {

            var sucess = await _userService.ActivateAsync(id, true);
            return sucess ? NoContent() : NotFound();
        }

        // GET api/user/{id}/disable
        [HttpGet("{id}/disable")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Disable(string id)
        {
            if (id == CurrentUserId)
                return BadRequest(new { error = "No puedes desabilitar tu propia cuenta." });

            var sucess = await _userService.ActivateAsync(id, false);
            return sucess ? NoContent() : NotFound();
        }


    }
}