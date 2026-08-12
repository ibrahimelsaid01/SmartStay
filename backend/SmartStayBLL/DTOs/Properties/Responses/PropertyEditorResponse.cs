namespace SmartStayBLL
{
    public sealed class PropertyEditorResponse
    {
        public Guid PropertyId { get; set; }

        public PropertyDraftResponse BasicInformation
        { get; set; } = new();

        public PropertyLocationResponse Location
        { get; set; } = new();

        public PropertyCapacityResponse Capacity
        { get; set; } = new();

        public PropertyPricingAndPoliciesResponse
            PricingAndPolicies
        { get; set; } = new();

        public PropertyHouseRulesResponse HouseRules
        { get; set; } = new();

        public PropertyAmenitiesResponse Amenities
        { get; set; } = new();

        public PropertyImagesResponse Images
        { get; set; } = new();

        public PropertyVerificationDocumentResponse?
            VerificationDocument
        { get; set; }

        public PropertyEditorCompletionResponse Completion
        { get; set; } = new();

        public string Status { get; set; } =
            string.Empty;

        public string? RejectionReason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? SubmittedAt { get; set; }

        public DateTimeOffset? ReviewedAt { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }
    }

    public sealed class PropertyEditorCompletionResponse
    {
        public bool IsEditable { get; set; }

        public bool BasicInformation { get; set; }

        public bool Location { get; set; }

        public bool Capacity { get; set; }

        public bool PricingAndPolicies { get; set; }

        public bool HouseRules { get; set; }

        public bool Images { get; set; }

        public bool VerificationDocument { get; set; }

        public bool CanSubmit { get; set; }

        public IReadOnlyList<string> SubmissionErrors
        { get; set; } = Array.Empty<string>();
    }
}