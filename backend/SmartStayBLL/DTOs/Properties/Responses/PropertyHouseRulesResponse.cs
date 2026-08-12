namespace SmartStayBLL
{
    public sealed class PropertyHouseRulesResponse
    {
        public Guid Id { get; set; }

        public bool AllowsSmoking { get; set; }

        public bool AllowsPets { get; set; }

        public bool AllowsParties { get; set; }

        public bool AllowsChildren { get; set; }

        public string? AdditionalHouseRules { get; set; }

        public string Status { get; set; } =
            string.Empty;

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}