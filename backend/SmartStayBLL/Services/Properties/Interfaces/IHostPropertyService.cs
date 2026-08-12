using Microsoft.AspNetCore.Http;
using SmartStayDAL;

namespace SmartStayBLL
{
    public interface IHostPropertyService
    {
        Task<PropertyDraftResponse> CreateDraftAsync(
            Guid userId,
            CreatePropertyDraftRequest request,
            CancellationToken cancellationToken = default);

        Task<PropertyDraftResponse> GetByIdAsync(
            Guid userId,
            Guid propertyId,
            CancellationToken cancellationToken = default);

        Task<PropertyEditorResponse> GetEditorAsync(
            Guid userId,
            Guid propertyId,
            CancellationToken cancellationToken = default);

        Task<PropertyDraftResponse>
            UpdateBasicInformationAsync(
                Guid userId,
                Guid propertyId,
                UpdatePropertyBasicInformationRequest request,
                CancellationToken cancellationToken = default);

        Task<PropertyLocationResponse>
            UpdateLocationAsync(
                Guid userId,
                Guid propertyId,
                UpdatePropertyLocationRequest request,
                CancellationToken cancellationToken = default);

        Task<PropertyCapacityResponse>
            UpdateCapacityAsync(
                Guid userId,
                Guid propertyId,
                UpdatePropertyCapacityRequest request,
                CancellationToken cancellationToken = default);

        Task<PropertyPricingAndPoliciesResponse>
            UpdatePricingAndPoliciesAsync(
                Guid userId,
                Guid propertyId,
                UpdatePropertyPricingAndPoliciesRequest request,
                CancellationToken cancellationToken = default);

        Task<PropertyHouseRulesResponse>
            UpdateHouseRulesAsync(
                Guid userId,
                Guid propertyId,
                UpdatePropertyHouseRulesRequest request,
                CancellationToken cancellationToken = default);

        Task<PropertyAmenitiesResponse>
            GetAmenitiesAsync(
                Guid userId,
                Guid propertyId,
                CancellationToken cancellationToken = default);

        Task<PropertyAmenitiesResponse>
            UpdateAmenitiesAsync(
                Guid userId,
                Guid propertyId,
                UpdatePropertyAmenitiesRequest request,
                CancellationToken cancellationToken = default);

        Task<PropertyImagesResponse> GetImagesAsync(
            Guid userId,
            Guid propertyId,
            CancellationToken cancellationToken = default);

        Task<PropertyImagesResponse> UploadImagesAsync(
            Guid userId,
            Guid propertyId,
            IReadOnlyCollection<IFormFile> files,
            CancellationToken cancellationToken = default);

        Task<PropertyImagesResponse> DeleteImageAsync(
            Guid userId,
            Guid propertyId,
            Guid imageId,
            CancellationToken cancellationToken = default);

        Task<PropertyImagesResponse> SetCoverImageAsync(
            Guid userId,
            Guid propertyId,
            Guid imageId,
            CancellationToken cancellationToken = default);

        Task<PropertyImagesResponse> UpdateImageOrderAsync(
            Guid userId,
            Guid propertyId,
            UpdatePropertyImageOrderRequest request,
            CancellationToken cancellationToken = default);

        Task<PropertyVerificationDocumentResponse>
            GetVerificationDocumentAsync(
                Guid userId,
                Guid propertyId,
                CancellationToken cancellationToken = default);

        Task<PropertyVerificationDocumentResponse>
            UploadVerificationDocumentAsync(
                Guid userId,
                Guid propertyId,
                PropertyVerificationDocumentType documentType,
                IReadOnlyCollection<IFormFile> files,
                CancellationToken cancellationToken = default);

        Task DeleteVerificationDocumentAsync(
            Guid userId,
            Guid propertyId,
            CancellationToken cancellationToken = default);

        Task<ImageContentResult>
            GetVerificationDocumentPageContentAsync(
                Guid userId,
                Guid propertyId,
                Guid pageId,
                CancellationToken cancellationToken = default);

        Task<PropertySubmissionResponse> SubmitAsync(
            Guid userId,
            Guid propertyId,
            CancellationToken cancellationToken = default);
    }
}