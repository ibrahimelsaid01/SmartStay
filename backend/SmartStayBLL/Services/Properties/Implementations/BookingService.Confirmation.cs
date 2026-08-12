using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed partial class BookingService
    {
        /*
         * =====================================================
         * Guest booking confirmation page
         * =====================================================
         */

        public async Task<GuestBookingConfirmationResponse>
            GetGuestBookingConfirmationAsync(
                Guid guestUserId,
                Guid bookingId,
                CancellationToken cancellationToken = default)
        {
            ValidateGuestUserIdentifier(
                guestUserId);

            ValidateBookingIdentifier(
                bookingId);

            await EnsureActiveGuestExistsAsync(
                guestUserId,
                cancellationToken);

            var booking =
                await _dbContext.Bookings
                    .AsNoTracking()
                    .Include(booking =>
                        booking.GuestUser)
                    .Include(booking =>
                        booking.Property)
                    .ThenInclude(property =>
                        property.Images)
                    .SingleOrDefaultAsync(
                        booking =>
                            booking.Id == bookingId
                            &&
                            booking.GuestUserId ==
                                guestUserId,
                        cancellationToken);

            if (booking is null)
            {
                throw new KeyNotFoundException(
                    "The booking was not found.");
            }

            /*
             * The success page must not expose confirmation
             * details while the booking is still waiting for
             * payment or has been cancelled/expired.
             */
            if (booking.Status !=
                    BookingStatus.Confirmed
                &&
                booking.Status !=
                    BookingStatus.Completed)
            {
                throw new InvalidOperationException(
                    "Booking confirmation details are available only after successful payment.");
            }

            /*
             * SucceededAt remains populated if a successful
             * payment later becomes partially or fully refunded.
             */
            var payment =
                await _dbContext.BookingPayments
                    .AsNoTracking()
                    .Where(payment =>
                        payment.BookingId ==
                            booking.Id
                        &&
                        payment.SucceededAt.HasValue)
                    .OrderByDescending(payment =>
                        payment.SucceededAt)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (payment is null)
            {
                throw new InvalidOperationException(
                    "The successful payment record for this booking was not found.");
            }

            var coverImageUrl =
                booking.Property.Images
                    .OrderByDescending(image =>
                        image.IsCover)
                    .ThenBy(image =>
                        image.DisplayOrder)
                    .Select(image =>
                        image.Url)
                    .FirstOrDefault();

            return new GuestBookingConfirmationResponse
            {
                BookingId =
                    booking.Id,

                Status =
                    booking.Status.ToString(),

                ConfirmedAt =
                    booking.ConfirmedAt,

                GuestEmail =
                    booking.GuestUser.Email
                    ?? string.Empty,

                Property =
                    new GuestBookingConfirmationPropertyResponse
                    {
                        Id =
                            booking.Property.Id,

                        Title =
                            booking.Property.Title,

                        PropertyType =
                            booking.Property.PropertyType
                                .ToString(),

                        CoverImageUrl =
                            coverImageUrl,

                        Country =
                            booking.Property.Country
                            ?? string.Empty,

                        City =
                            booking.Property.City
                            ?? string.Empty,

                        StreetAddress =
                            booking.Property.StreetAddress,

                        BuildingNumber =
                            booking.Property.BuildingNumber,

                        Floor =
                            booking.Property.Floor,

                        ApartmentNumber =
                            booking.Property.ApartmentNumber,

                        PostalCode =
                            booking.Property.PostalCode,

                        FullAddress =
                            BuildPropertyFullAddress(
                                booking.Property),

                        Latitude =
                            booking.Property.Latitude,

                        Longitude =
                            booking.Property.Longitude
                    },

                Stay =
                    new GuestBookingConfirmationStayResponse
                    {
                        CheckInDate =
                            booking.CheckInDate,

                        CheckOutDate =
                            booking.CheckOutDate,

                        GuestsCount =
                            booking.GuestsCount,

                        Nights =
                            booking.Nights
                    },

                Pricing =
                    new GuestBookingConfirmationPricingResponse
                    {
                        PricePerNight =
                            booking.PricePerNight,

                        Subtotal =
                            booking.Subtotal,

                        ServiceFee =
                            booking.ServiceFee,

                        TotalAmount =
                            booking.TotalAmount,

                        Currency =
                            booking.Currency
                    },

                Payment =
                    new GuestBookingConfirmationPaymentResponse
                    {
                        PaymentId =
                            payment.Id,

                        Status =
                            payment.Status.ToString(),

                        Provider =
                            payment.Provider,

                        Amount =
                            payment.Amount,

                        RefundedAmount =
                            payment.RefundedAmount,

                        Currency =
                            payment.Currency,

                        SucceededAt =
                            payment.SucceededAt
                    }
            };
        }

        private static string BuildPropertyFullAddress(
            Property property)
        {
            var addressParts =
                new[]
                {
                    property.BuildingNumber,
                    property.StreetAddress,

                    string.IsNullOrWhiteSpace(
                        property.ApartmentNumber)
                        ? null
                        : $"Apartment {property.ApartmentNumber}",

                    string.IsNullOrWhiteSpace(
                        property.Floor)
                        ? null
                        : $"Floor {property.Floor}",

                    property.City,
                    property.PostalCode,
                    property.Country
                };

            return string.Join(
                ", ",
                addressParts.Where(part =>
                    !string.IsNullOrWhiteSpace(part)));
        }
    }
}