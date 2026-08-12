using Microsoft.Extensions.Options;
using SmartStayBLL;

namespace SmartStay.Api
{
    public sealed class BookingLifecycleBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory
            _serviceScopeFactory;

        private readonly IOptions<BookingLifecycleOptions>
            _options;

        private readonly ILogger<
            BookingLifecycleBackgroundService>
            _logger;

        public BookingLifecycleBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            IOptions<BookingLifecycleOptions> options,
            ILogger<BookingLifecycleBackgroundService> logger)
        {
            ArgumentNullException.ThrowIfNull(
                serviceScopeFactory);

            ArgumentNullException.ThrowIfNull(
                options);

            ArgumentNullException.ThrowIfNull(
                logger);

            _serviceScopeFactory =
                serviceScopeFactory;

            _options =
                options;

            _logger =
                logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            var configuration =
                _options.Value;

            if (!configuration.Enabled)
            {
                _logger.LogInformation(
                    "The booking lifecycle background service is disabled.");

                return;
            }

            var processingInterval =
                TimeSpan.FromSeconds(
                    configuration
                        .ProcessingIntervalSeconds);

            _logger.LogInformation(
                "The booking lifecycle background service started. " +
                "Processing interval: {ProcessingIntervalSeconds} seconds.",
                configuration.ProcessingIntervalSeconds);

            /*
             * Run once immediately after application startup.
             *
             * This allows stale bookings to be processed
             * without waiting for the first timer interval.
             */
            if (configuration.RunImmediatelyOnStartup)
            {
                await ProcessLifecycleSafelyAsync(
                    stoppingToken);
            }

            using var timer =
                new PeriodicTimer(
                    processingInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(
                           stoppingToken))
                {
                    /*
                     * We await every execution before waiting
                     * for the next one.
                     *
                     * Therefore lifecycle executions do not
                     * overlap within this application instance.
                     */
                    await ProcessLifecycleSafelyAsync(
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                /*
                 * Expected during graceful application
                 * shutdown.
                 */
            }

            _logger.LogInformation(
                "The booking lifecycle background service stopped.");
        }

        private async Task ProcessLifecycleSafelyAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                /*
                 * BookingLifecycleService is Scoped because
                 * it depends on SmartStayDbContext.
                 *
                 * BackgroundService is Singleton, so a new
                 * dependency-injection scope must be created
                 * for every execution.
                 */
                await using var scope =
                    _serviceScopeFactory
                        .CreateAsyncScope();

                var lifecycleService =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IBookingLifecycleService>();

                var result =
                    await lifecycleService
                        .ProcessLifecycleAsync(
                            stoppingToken);

                if (result.TotalUpdatedBookingsCount > 0)
                {
                    _logger.LogInformation(
                        "Booking lifecycle processing completed. " +
                        "Expired bookings: {ExpiredBookingsCount}. " +
                        "Completed bookings: {CompletedBookingsCount}. " +
                        "Total updated bookings: {TotalUpdatedBookingsCount}. " +
                        "Processed at: {ProcessedAt}.",
                        result.ExpiredBookingsCount,
                        result.CompletedBookingsCount,
                        result.TotalUpdatedBookingsCount,
                        result.ProcessedAt);
                }
                else
                {
                    _logger.LogDebug(
                        "Booking lifecycle processing completed " +
                        "without updating any bookings. " +
                        "Processed at: {ProcessedAt}.",
                        result.ProcessedAt);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                /*
                 * Do not log application shutdown as an error.
                 */
            }
            catch (Exception exception)
            {
                /*
                 * A failure in one execution must not stop the
                 * background service permanently.
                 *
                 * The next timer iteration will try again.
                 */
                _logger.LogError(
                    exception,
                    "An error occurred while processing " +
                    "the booking lifecycle.");
            }
        }
    }
}