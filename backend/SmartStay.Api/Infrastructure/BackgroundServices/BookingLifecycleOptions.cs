namespace SmartStay.Api
{
    public sealed class BookingLifecycleOptions
    {
        public const string SectionName =
            "BookingLifecycle";

        /*
         * Allows the background process to be disabled
         * through configuration without removing its
         * service registration.
         */
        public bool Enabled { get; set; } = true;

        /*
         * The lifecycle process runs once every
         * configured number of seconds.
         *
         * Recommended production value: 60 seconds.
         */
        public int ProcessingIntervalSeconds { get; set; } = 60;

        /*
         * When true, the lifecycle process runs once
         * immediately after the application starts.
         */
        public bool RunImmediatelyOnStartup { get; set; } = true;
    }
}