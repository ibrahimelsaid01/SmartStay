namespace SmartStayBLL
{
    public sealed class HostApplicationNotEditableException
        : Exception
    {
        public HostApplicationNotEditableException(
            string currentStatus)
            : base(
                $"The host application cannot be edited while its status is {currentStatus}.")
        {
        }
    }
}