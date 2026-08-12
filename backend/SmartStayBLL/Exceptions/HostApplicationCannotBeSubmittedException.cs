namespace SmartStayBLL
{
    public sealed class HostApplicationCannotBeSubmittedException
        : Exception
    {
        public HostApplicationCannotBeSubmittedException(
            string currentStatus)
            : base(
                $"The host application cannot be submitted while its status is {currentStatus}.")
        {
        }
    }
}