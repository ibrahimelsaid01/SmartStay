using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/properties")]
    [AllowAnonymous]
    public sealed class PropertiesController
        : ControllerBase
    {
        private readonly IPublicPropertyService
            _publicPropertyService;

        private readonly IBookingService
            _bookingService;

        public PropertiesController(
            IPublicPropertyService publicPropertyService,
            IBookingService bookingService)
        {
            ArgumentNullException.ThrowIfNull(
                publicPropertyService);

            ArgumentNullException.ThrowIfNull(
                bookingService);

            _publicPropertyService =
                publicPropertyService;

            _bookingService =
                bookingService;
        }

        /*
         * GET:
         * /api/properties
         *
         * Example:
         * /api/properties
         * ?city=Mansoura
         * &propertyType=Apartment
         * &minPrice=500
         * &maxPrice=1500
         * &minGuests=2
         * &sort=PriceLowToHigh
         * &page=1
         * &pageSize=12
         */
        [HttpGet]
        [ProducesResponseType(
            typeof(PublicPropertiesResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        public async Task<
            ActionResult<PublicPropertiesResponse>>
            SearchAsync(
                [FromQuery]
                PublicPropertySearchRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            var response =
                await _publicPropertyService
                    .SearchAsync(
                        request,
                        cancellationToken);

            return Ok(
                response);
        }

        /*
         * GET:
         * /api/properties/{propertyId}
         */
        [HttpGet("{propertyId:guid}")]
        [ProducesResponseType(
            typeof(PublicPropertyDetailsResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<PublicPropertyDetailsResponse>>
            GetByIdAsync(
                Guid propertyId,
                CancellationToken cancellationToken =
                    default)
        {
            var response =
                await _publicPropertyService
                    .GetByIdAsync(
                        propertyId,
                        cancellationToken);

            return Ok(
                response);
        }

        /*
         * GET:
         * /api/properties/{propertyId}/availability
         *
         * Example:
         * /api/properties/{propertyId}/availability
         * ?checkInDate=2026-07-10
         * &checkOutDate=2026-07-15
         * &guestsCount=3
         */
        [HttpGet("{propertyId:guid}/availability")]
        [ProducesResponseType(
            typeof(PropertyAvailabilityResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        public async Task<
            ActionResult<PropertyAvailabilityResponse>>
            CheckAvailabilityAsync(
                Guid propertyId,
                [FromQuery]
                BookingPeriodRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            var response =
                await _bookingService
                    .CheckAvailabilityAsync(
                        propertyId,
                        request,
                        cancellationToken);

            return Ok(
                response);
        }

        /*
         * GET:
         * /api/properties/{propertyId}/booking-quote
         *
         * Example:
         * /api/properties/{propertyId}/booking-quote
         * ?checkInDate=2026-07-10
         * &checkOutDate=2026-07-15
         * &guestsCount=3
         */
        [HttpGet("{propertyId:guid}/booking-quote")]
        [ProducesResponseType(
            typeof(BookingQuoteResponse),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            StatusCodes.Status409Conflict)]
        public async Task<
            ActionResult<BookingQuoteResponse>>
            GetBookingQuoteAsync(
                Guid propertyId,
                [FromQuery]
                BookingPeriodRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            var response =
                await _bookingService
                    .GetQuoteAsync(
                        propertyId,
                        request,
                        cancellationToken);

            return Ok(
                response);
        }
    }
}