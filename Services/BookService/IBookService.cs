using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MiApi.Dtos;

namespace MiApi.Services.BookService
{
    public interface IBookService
    {
        Task<IEnumerable<BookDto>> GetAllAsync(string ? title = null, string ? author = null);

        Task<BookDto?> GetByIdAsync(int id);

        Task<BookDto> AddAsync(BookDto book);

        Task<BookDto> UpdateAsync(BookDto book);

        Task<bool> DeleteAsync(int id);

    }
}