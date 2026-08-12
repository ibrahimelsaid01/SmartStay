namespace SmartStayBLL
{
    public sealed class HostApplicationNotPendingException
        : Exception
    {
        public HostApplicationNotPendingException(
            string currentStatus)
            : base(
                "Only pending host applications can be reviewed. " +
                $"The current status is {currentStatus}.")
        {
        }
    }
}