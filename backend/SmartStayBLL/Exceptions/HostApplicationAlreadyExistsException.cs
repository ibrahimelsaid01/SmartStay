namespace SmartStayBLL
{
    public sealed class HostApplicationAlreadyExistsException
        : Exception
    {
        public HostApplicationAlreadyExistsException()
            : base(
                "You already have a host application.")
        {
        }
    }
}