using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MiApi.Dtos;
using MiApi.Services.Reservation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private bool IsAdmin => User.IsInRole("Admin");

        // GET api/reservation?userId=...&bookId=...&isReturned=...
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ReservationDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetAll(
            [FromQuery] string? userId,
            [FromQuery] int? bookId,
            [FromQuery] bool? isReturned)
        {
            // Un usuario normal solo ve sus propias reservas
            if (!IsAdmin)
                userId = CurrentUserId;

            var reservations = await _reservationService.GetAllAsync(userId, bookId, isReturned);
            return Ok(reservations);
        }

        // GET api/reservation/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReservationDto>> GetById(int id)
        {
            var reservation = await _reservationService.GetByIdAsync(id);
            if (reservation is null)
                return NotFound();

            if (!IsAdmin && reservation.UserId != CurrentUserId)
                return Forbid();

            return Ok(reservation);
        }

        // POST api/reservation
        [HttpPost]
        [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ReservationDto>> Add([FromBody] ReservationDto reservation)
        {
            // Un usuario normal solo puede reservar a su nombre
            if (!IsAdmin)
                reservation.UserId = CurrentUserId;

            var created = await _reservationService.AddAsync(reservation);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT api/reservation/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReservationDto>> Update(int id, [FromBody] ReservationDto reservation)
        {
            if (id != reservation.Id)
                return BadRequest("El id de la ruta no coincide con el de la reserva.");

            try
            {
                var updated = await _reservationService.UpdateAsync(reservation);
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // DELETE api/reservation/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _reservationService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        // PUT api/reservation/1
        [HttpPut("{id:int}/return")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReservationDto>> Return(int id)
        {
            
            try
            {
                var updated = await _reservationService.ReturnAsync(id);
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

    }
}