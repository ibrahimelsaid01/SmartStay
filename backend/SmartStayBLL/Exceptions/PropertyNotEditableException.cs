namespace SmartStayBLL
{
    public sealed class PropertyNotEditableException
        : Exception
    {
        public PropertyNotEditableException(
            string currentStatus)
            : base(
                "The property cannot be edited while " +
                $"its status is {currentStatus}.")
        {
        }
    }
}