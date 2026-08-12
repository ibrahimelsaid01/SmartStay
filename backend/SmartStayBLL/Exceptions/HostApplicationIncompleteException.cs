namespace SmartStayBLL
{
    public sealed class HostApplicationIncompleteException
        : Exception
    {
        public HostApplicationIncompleteException(
            IEnumerable<string> missingRequirements)
            : base(
                "The host application is incomplete. Missing: " +
                string.Join(
                    ", ",
                    missingRequirements) +
                ".")
        {
        }
    }
}