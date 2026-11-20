using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MiApi.Dtos;
using MiApi.Exceptions;
using MiApi.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MiApi.Services.BookService
{
    public class BookService : IBookService
    {

        private readonly IRepository<Models.Book> _bookRepository;
        private readonly IRepository<Models.Reservation> _reservationRepository;

        public BookService(IRepository<Models.Book> bookRepository, IRepository<Models.Reservation> reservationRepository)
        {
            _bookRepository = bookRepository;
            _reservationRepository = reservationRepository;
        }

        public async Task<BookDto> AddAsync(BookDto book)
        {
                var bookEntity = new Models.Book
                {
                    Title = book.Title,
                    Author = book.Author,
                    PublishedDate = book.PublishedDate,
                    Description = book.Description,
                    Stock = book.Stock,
                    Pages = book.Pages
                };

                await _bookRepository.AddAsync(bookEntity);
                await _bookRepository.SaveChangesAsync();

                book.Id = bookEntity.Id; // Asignar el Id generado al DTO

                return book;

        }

        public async Task<bool> DeleteAsync(int id)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null)
                return false;

            var hasActiveReservations = (await _reservationRepository
                .GetAllAsync(r => r.BookId == id)).Any();

            if (hasActiveReservations)
                throw new ConflictException("No se puede eliminar el libro: tiene reservas activas.");


            _bookRepository.Delete(book);
            await _bookRepository.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<BookDto>> GetAllAsync(string? title = null, string? author = null)
        {

            var books = await _bookRepository.GetAllAsync(book => (string.IsNullOrEmpty(title) || book.Title.Contains(title)) &&
                                                      (string.IsNullOrEmpty(author) || book.Author.Contains(author)));
            return books.Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                PublishedDate = b.PublishedDate,
                Description = b.Description,
                Pages = b.Pages,
                Stock = b.Stock
            });
        }

        public async Task<BookDto?> GetByIdAsync(int id)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null)
                return null;

            return new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                PublishedDate = book.PublishedDate,
                Description = book.Description,
                Pages = book.Pages,
                Stock = book.Stock
            };
        }

        public async Task<BookDto> UpdateAsync(BookDto book)
        {
            var bookEntity = await _bookRepository.GetByIdAsync(book.Id);
            if (bookEntity == null)
                throw new NotFoundException("El libro no fue encontrado.");

            bookEntity.Title = book.Title;
            bookEntity.Author = book.Author;
            bookEntity.PublishedDate = book.PublishedDate;
            bookEntity.Description = book.Description;
            bookEntity.Stock = book.Stock;
            bookEntity.Pages = book.Pages;

            _bookRepository.Update(bookEntity);
            await _bookRepository.SaveChangesAsync();

            return book;
        }
    }
}