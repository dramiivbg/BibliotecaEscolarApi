using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MiApi.Dtos;
using MiApi.Repositories;
using MiApi.Services.Book;

namespace MiApi.Services.Reservation
{
    public class ReservationService : IReservationService
    {

         private readonly IRepository<Models.Reservation> _reservationRepository;

         private readonly IRepository<Models.Book> _bookRepository;

        public ReservationService(IRepository<Models.Reservation> reservationRepository, IRepository<Models.Book> bookRepository)
        {
            _reservationRepository = reservationRepository;
            _bookRepository = bookRepository;
        }

        public async Task<ReservationDto> AddAsync(ReservationDto reservation)
        {
            var book = await _bookRepository.GetByIdAsync(reservation.BookId)
                ?? throw new NotFoundException("El libro no existe.");

            if (book.Stock <= 0)
                throw new InvalidOperationException("No hay ejemplares disponibles.");

            book.Stock--;
            var reservationEntity = new Models.Reservation
            {
                UserId = reservation.UserId,
                BookId = reservation.BookId,
                ReservationDate = reservation.ReservationDate,
                DueDate = reservation.DueDate,
                IsReturned = false,
            };

            await _reservationRepository.AddAsync(reservationEntity);
            await _reservationRepository.SaveChangesAsync();


            reservation.Id = reservationEntity.Id;

            return reservation;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null)
                return false;

            if (!reservation.IsReturned)
            {
                var book = await _bookRepository.GetByIdAsync(reservation.BookId)
                    ?? throw new NotFoundException("El libro no existe.");

                book.Stock++;
            }

            _reservationRepository.Delete(reservation);
            await _reservationRepository.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ReservationDto>> GetAllAsync(string? userId = null, int? bookId = null, bool? isReturned = null)
        {
           
            var reservations = await _reservationRepository.GetAllAsync(r => (userId == null || r.UserId == userId) &&
                                                (bookId == null || r.BookId == bookId) &&
                                                (isReturned == null || r.IsReturned == isReturned));
            return reservations.Select(r => new ReservationDto
            {
                Id = r.Id,
                UserId = r.UserId,
                BookId = r.BookId,
                ReservationDate = r.ReservationDate,
                DueDate = r.DueDate,
                IsReturned = r.IsReturned
            });
        }


        public async Task<ReservationDto?> GetByIdAsync(int id)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null)
                return null;

            return new ReservationDto
            {
                Id = reservation.Id,
                UserId = reservation.UserId,
                BookId = reservation.BookId,
                ReservationDate = reservation.ReservationDate,
                DueDate = reservation.DueDate,
                IsReturned = reservation.IsReturned
            };
        }

        public async Task<ReservationDto> ReturnAsync(int id)
        {
            var reservationEntity = await _reservationRepository.GetByIdAsync(id);
            if (reservationEntity == null)
                throw new ArgumentException("Reservation not found");
            
            reservationEntity.IsReturned = true;

            _reservationRepository.Update(reservationEntity);
            await _reservationRepository.SaveChangesAsync();

            return new ReservationDto
            {
                Id = reservationEntity.Id,
                UserId = reservationEntity.UserId,
                BookId = reservationEntity.BookId,
                ReservationDate = reservationEntity.ReservationDate,
                DueDate = reservationEntity.DueDate,
                IsReturned = reservationEntity.IsReturned
            };
        }

        public async Task<ReservationDto> UpdateAsync(ReservationDto reservation)
        {
            var reservationEntity = await _reservationRepository.GetByIdAsync(reservation.Id);
            if (reservationEntity == null)
                throw new ArgumentException("Reservation not found");

            reservationEntity.ReservationDate = reservation.ReservationDate;
            reservationEntity.DueDate = reservation.DueDate;

            _reservationRepository.Update(reservationEntity);
            await _reservationRepository.SaveChangesAsync();

            return new ReservationDto
            {
                Id = reservationEntity.Id,
                UserId = reservationEntity.UserId,
                BookId = reservationEntity.BookId,
                ReservationDate = reservationEntity.ReservationDate,
                DueDate = reservationEntity.DueDate,
                IsReturned = reservationEntity.IsReturned
            };
        }
    }
}