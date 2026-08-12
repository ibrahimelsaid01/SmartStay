using System.Data;
using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed class BookingLifecycleService
        : IBookingLifecycleService
    {
        private readonly SmartStayDbContext _dbContext;

        public BookingLifecycleService(
            SmartStayDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(
                dbContext);

            _dbContext =
                dbContext;
        }

        /*
         * =====================================================
         * Confirm booking after successful payment
         * =====================================================
         */

        public async Task<BookingConfirmationResponse>
            ConfirmAfterSuccessfulPaymentAsync(
                Guid bookingId,
                CancellationToken cancellationToken = default)
        {
            ValidateBookingIdentifier(
                bookingId);

            var currentTime =
                DateTimeOffset.UtcNow;

            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

            var transactionCompleted =
                false;

            try
            {
                var booking =
                    await _dbContext.Bookings
                        .SingleOrDefaultAsync(
                            item =>
                                item.Id == bookingId,
                            cancellationToken);

                if (booking is null)
                {
                    throw new KeyNotFoundException(
                        "The booking was not found.");
                }

                if (booking.Status ==
                    BookingStatus.Confirmed)
                {
                    await transaction.CommitAsync(
                        cancellationToken);

                    transactionCompleted =
                        true;

                    return new BookingConfirmationResponse
                    {
                        BookingId =
                            booking.Id,

                        Status =
                            BookingStatus
                                .Confirmed
                                .ToString(),

                        ConfirmedAt =
                            booking.ConfirmedAt,

                        WasAlreadyProcessed =
                            true,

                        Message =
                            "The booking payment was already processed and the booking is confirmed."
                    };
                }

                if (booking.Status ==
                    BookingStatus.Completed)
                {
                    await transaction.CommitAsync(
                        cancellationToken);

                    transactionCompleted =
                        true;

                    return new BookingConfirmationResponse
                    {
                        BookingId =
                            booking.Id,

                        Status =
                            BookingStatus
                                .Completed
                                .ToString(),

                        ConfirmedAt =
                            booking.ConfirmedAt,

                        WasAlreadyProcessed =
                            true,

                        Message =
                            "The booking payment was already processed and the stay has been completed."
                    };
                }

                if (booking.Status ==
                        BookingStatus.Pending
                    &&
                    (
                        !booking.ExpiresAt
                            .HasValue
                        ||
                        booking.ExpiresAt.Value <=
                            currentTime
                    ))
                {
                    booking.Status =
                        BookingStatus.Expired;

                    booking.ExpiredAt ??=
                        currentTime;

                    booking.UpdatedAt =
                        currentTime;

                    await _dbContext.SaveChangesAsync(
                        cancellationToken);

                    await transaction.CommitAsync(
                        cancellationToken);

                    transactionCompleted =
                        true;

                    throw new InvalidOperationException(
                        "The booking payment window has expired. " +
                        "The booking cannot be confirmed. " +
                        "Any successful late payment must be reviewed or refunded by the payment process.");
                }

                if (booking.Status ==
                    BookingStatus.Expired)
                {
                    throw new InvalidOperationException(
                        "The booking payment window has expired and the booking cannot be confirmed.");
                }

                if (booking.Status ==
                    BookingStatus.Cancelled)
                {
                    throw new InvalidOperationException(
                        "A cancelled booking cannot be confirmed.");
                }

                if (booking.Status !=
                    BookingStatus.Pending)
                {
                    throw new InvalidOperationException(
                        $"The booking cannot be confirmed from its current status '{booking.Status}'.");
                }

                booking.Status =
                    BookingStatus.Confirmed;

                booking.ConfirmedAt ??=
                    currentTime;

                booking.UpdatedAt =
                    currentTime;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                transactionCompleted =
                    true;

                return new BookingConfirmationResponse
                {
                    BookingId =
                        booking.Id,

                    Status =
                        BookingStatus
                            .Confirmed
                            .ToString(),

                    ConfirmedAt =
                        booking.ConfirmedAt,

                    WasAlreadyProcessed =
                        false,

                    Message =
                        "The booking was confirmed successfully after payment."
                };
            }
            catch
            {
                if (!transactionCompleted)
                {
                    await transaction.RollbackAsync(
                        CancellationToken.None);
                }

                throw;
            }
        }

        /*
         * =====================================================
         * Process automatic lifecycle transitions
         * =====================================================
         */

        public async Task<BookingLifecycleProcessResponse>
            ProcessLifecycleAsync(
                CancellationToken cancellationToken = default)
        {
            var currentTime =
                DateTimeOffset.UtcNow;

            var currentDate =
                DateOnly.FromDateTime(
                    currentTime.UtcDateTime);

            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

            try
            {
                /*
                 * These records must be tracked so the
                 * SaveChanges interceptor can detect the
                 * status transitions and create notifications.
                 */
                var expiredBookings =
                    await _dbContext.Bookings
                        .Where(booking =>
                            booking.Status ==
                                BookingStatus.Pending
                            &&
                            booking.ExpiresAt
                                .HasValue
                            &&
                            booking.ExpiresAt.Value <=
                                currentTime)
                        .ToListAsync(
                            cancellationToken);

                foreach (var booking
                         in expiredBookings)
                {
                    booking.Status =
                        BookingStatus.Expired;

                    booking.ExpiredAt ??=
                        currentTime;

                    booking.UpdatedAt =
                        currentTime;
                }

                var completedBookings =
                    await _dbContext.Bookings
                        .Where(booking =>
                            booking.Status ==
                                BookingStatus.Confirmed
                            &&
                            booking.CheckOutDate <=
                                currentDate)
                        .ToListAsync(
                            cancellationToken);

                foreach (var booking
                         in completedBookings)
                {
                    booking.Status =
                        BookingStatus.Completed;

                    booking.CompletedAt ??=
                        currentTime;

                    booking.UpdatedAt =
                        currentTime;
                }

                if (expiredBookings.Count > 0
                    ||
                    completedBookings.Count > 0)
                {
                    await _dbContext.SaveChangesAsync(
                        cancellationToken);
                }

                await transaction.CommitAsync(
                    cancellationToken);

                return new BookingLifecycleProcessResponse
                {
                    ExpiredBookingsCount =
                        expiredBookings.Count,

                    CompletedBookingsCount =
                        completedBookings.Count,

                    ProcessedAt =
                        currentTime
                };
            }
            catch
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                throw;
            }
        }

        /*
         * =====================================================
         * Validation
         * =====================================================
         */

        private static void ValidateBookingIdentifier(
            Guid bookingId)
        {
            if (bookingId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The booking identifier is invalid.");
            }
        }
    }
}