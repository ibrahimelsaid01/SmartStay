using Microsoft.AspNetCore.Mvc;
using SmartStayBLL;

namespace SmartStay.Api
{
    [ApiController]
    [Route("api/amenities")]
    public sealed class AmenitiesController
        : ControllerBase
    {
        private readonly IAmenityService
            _amenityService;

        public AmenitiesController(
            IAmenityService amenityService)
        {
            ArgumentNullException.ThrowIfNull(
                amenityService);

            _amenityService =
                amenityService;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(IReadOnlyList<AmenityResponse>),
            StatusCodes.Status200OK)]
        public async Task<
            ActionResult<
                IReadOnlyList<AmenityResponse>>>
            GetActiveAmenitiesAsync(
                CancellationToken cancellationToken)
        {
            var response =
                await _amenityService
                    .GetActiveAmenitiesAsync(
                        cancellationToken);

            return Ok(response);
        }
    }
}