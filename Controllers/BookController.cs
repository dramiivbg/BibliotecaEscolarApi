using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiApi.Dtos;
using MiApi.Exceptions;
using MiApi.Services.Book;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookController : ControllerBase
    {
        private readonly IBookService _bookService;

        public BookController(IBookService bookService)
        {
            _bookService = bookService;
        }

        // GET api/book?title=...&author=...
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<BookDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetAll(
            [FromQuery] string? title,
            [FromQuery] string? author)
        {
            var books = await _bookService.GetAllAsync(title, author);
            return Ok(books);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // quita o cambia a [Authorize(Roles = "Admin")] según tu necesidad
        [ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BookDto>> Add([FromBody] BookDto book)
        {
            var created = await _bookService.AddAsync(book);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // GET api/book/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookDto>> GetById(int id)
        {
            var book = await _bookService.GetByIdAsync(id);
            return book is null ? NotFound() : Ok(book);
        }

        // PUT api/book/5
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookDto>> Update(int id, [FromBody] BookDto book)
        {
            if (id != book.Id)
                return BadRequest("El id de la ruta no coincide con el del libro.");

            try
            {
                var updated = await _bookService.UpdateAsync(book);
                return Ok(updated);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }


        // DELETE api/book/5
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {

        try
            {
                var deleted = await _bookService.DeleteAsync(id);
                return deleted ? NoContent() : NotFound();
            }
            catch (ConflictException ex)
            {
                
                return Conflict(new { error = ex.Message });
            }
        }
    }
}