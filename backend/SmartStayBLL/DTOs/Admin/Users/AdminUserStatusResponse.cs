namespace SmartStayBLL
{
    public sealed class AdminUserStatusResponse
    {
        public Guid UserId { get; set; }

        public bool IsActive { get; set; }

        public int RevokedRefreshTokensCount { get; set; }

        public int UnpublishedPropertiesCount { get; set; }

        public string Message { get; set; } =
            string.Empty;
    }
}