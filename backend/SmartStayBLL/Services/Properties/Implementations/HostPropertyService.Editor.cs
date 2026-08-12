using Microsoft.EntityFrameworkCore;
using SmartStayDAL;

namespace SmartStayBLL
{
    public sealed partial class HostPropertyService
    {
        public async Task<PropertyEditorResponse>
            GetEditorAsync(
                Guid userId,
                Guid propertyId,
                CancellationToken cancellationToken = default)
        {
            var property =
                await _dbContext.Properties
                    .AsNoTracking()
                    .Include(property =>
                        property.HostProfile)
                    .ThenInclude(hostProfile =>
                        hostProfile.User)
                    .Include(property =>
                        property.Images)
                    .Include(property =>
                        property.PropertyAmenities)
                    .ThenInclude(propertyAmenity =>
                        propertyAmenity.Amenity)
                    .Include(property =>
                        property.VerificationDocument)
                    .ThenInclude(document =>
                        document.Pages)
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(
                        property =>
                            property.Id == propertyId
                            &&
                            property.HostProfile.UserId ==
                                userId,
                        cancellationToken);

            if (property is null)
            {
                throw new KeyNotFoundException(
                    "The property was not found.");
            }

            if (!property.HostProfile.User.IsActive)
            {
                throw new UnauthorizedAccessException(
                    "This account is inactive.");
            }

            if (property.HostProfile.Status !=
                HostApplicationStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Only approved hosts can manage properties.");
            }

            var submissionErrors =
                GetPropertySubmissionValidationErrors(
                    property);

            var isEditable =
                property.Status == PropertyStatus.Draft
                ||
                property.Status == PropertyStatus.Rejected
                ||
                property.Status == PropertyStatus.Unpublished;

            return new PropertyEditorResponse
            {
                PropertyId =
                    property.Id,

                BasicInformation =
                    MapToDraftResponse(
                        property),

                Location =
                    MapToLocationResponse(
                        property),

                Capacity =
                    MapToCapacityResponse(
                        property),

                PricingAndPolicies =
                    MapToPricingAndPoliciesResponse(
                        property),

                HouseRules =
                    MapToHouseRulesResponse(
                        property),

                Amenities =
                    MapToAmenitiesResponse(
                        property),

                Images =
                    MapToImagesResponse(
                        property),

                VerificationDocument =
                    property.VerificationDocument is null
                        ? null
                        : MapToVerificationDocumentResponse(
                            property),

                Completion =
                    new PropertyEditorCompletionResponse
                    {
                        IsEditable =
                            isEditable,

                        BasicInformation =
                            IsSubmissionSectionComplete(
                                submissionErrors,
                                "Basic information:"),

                        Location =
                            IsSubmissionSectionComplete(
                                submissionErrors,
                                "Location:"),

                        Capacity =
                            IsSubmissionSectionComplete(
                                submissionErrors,
                                "Capacity:"),

                        PricingAndPolicies =
                            IsSubmissionSectionComplete(
                                submissionErrors,
                                "Pricing and policies:"),

                        HouseRules =
                            IsSubmissionSectionComplete(
                                submissionErrors,
                                "House rules:"),

                        Images =
                            IsSubmissionSectionComplete(
                                submissionErrors,
                                "Images:"),

                        VerificationDocument =
                            IsSubmissionSectionComplete(
                                submissionErrors,
                                "Verification document:"),

                        CanSubmit =
                            isEditable
                            &&
                            submissionErrors.Count == 0,

                        SubmissionErrors =
                            submissionErrors
                    },

                Status =
                    property.Status.ToString(),

                RejectionReason =
                    property.RejectionReason,

                CreatedAt =
                    property.CreatedAt,

                UpdatedAt =
                    property.UpdatedAt,

                SubmittedAt =
                    property.SubmittedAt,

                ReviewedAt =
                    property.ReviewedAt,

                PublishedAt =
                    property.PublishedAt
            };
        }

        private static bool IsSubmissionSectionComplete(
            IReadOnlyList<string> submissionErrors,
            string sectionPrefix)
        {
            return !submissionErrors.Any(error =>
                error.StartsWith(
                    sectionPrefix,
                    StringComparison.Ordinal));
        }
    }
}