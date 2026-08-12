namespace SmartStayDAL
{
    public sealed class AdminActionLog
    {
        public Guid Id { get; set; }

        public Guid AdminUserId { get; set; }

        public AdminActionType ActionType { get; set; } =
            AdminActionType.Other;

        public AdminActionTargetType TargetType { get; set; } =
            AdminActionTargetType.System;

        public Guid? TargetId { get; set; }

        public string? TargetReference { get; set; }

        public string Summary { get; set; } =
            string.Empty;

        public string? Details { get; set; }

        public string? MetadataJson { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public ApplicationUser AdminUser { get; set; } =
            null!;
    }
}