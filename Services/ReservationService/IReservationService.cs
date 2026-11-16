using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MiApi.Dtos;


namespace MiApi.Services.ReservationService
{
    public interface IReservationService
    {
        Task<IEnumerable<ReservationDto>> GetAllAsync(string? userId = null, int? bookId = null, bool? isReturned = null);
        Task<ReservationDto?> GetByIdAsync(int id);

        Task<ReservationDto> AddAsync(ReservationDto reservation);

        Task<ReservationDto> UpdateAsync(ReservationDto reservation);
        Task<bool> DeleteAsync(int id);

        Task<ReservationDto> ReturnAsync(int id);
    }
}