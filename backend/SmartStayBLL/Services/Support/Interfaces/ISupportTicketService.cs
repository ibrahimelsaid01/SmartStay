using Microsoft.AspNetCore.Http;

namespace SmartStayBLL
{
    public interface ISupportTicketService
    {
        Task<SupportTicketResponse> CreateTicketAsync(
            Guid userId,
            CreateSupportTicketRequest request,
            CancellationToken cancellationToken = default);

        Task<SupportTicketsResponse> GetMyTicketsAsync(
            Guid userId,
            SupportTicketSearchRequest request,
            CancellationToken cancellationToken = default);

        Task<SupportTicketResponse> GetMyTicketByIdAsync(
            Guid userId,
            Guid ticketId,
            CancellationToken cancellationToken = default);

        Task<SupportTicketResponse> AddUserMessageAsync(
            Guid userId,
            Guid ticketId,
            CreateSupportTicketMessageRequest request,
            CancellationToken cancellationToken = default);

        Task<UploadSupportTicketAttachmentResponse> UploadUserAttachmentAsync(
            Guid userId,
            Guid ticketId,
            IFormFile file,
            string? type,
            CancellationToken cancellationToken = default);

        Task<SupportTicketsResponse> GetAdminTicketsAsync(
            SupportTicketSearchRequest request,
            CancellationToken cancellationToken = default);

        Task<SupportTicketResponse> GetAdminTicketByIdAsync(
            Guid ticketId,
            CancellationToken cancellationToken = default);

        Task<SupportTicketResponse> AddAdminReplyAsync(
            Guid adminUserId,
            Guid ticketId,
            CreateSupportTicketMessageRequest request,
            CancellationToken cancellationToken = default);

        Task<SupportTicketResponse> ApplyAdminDecisionAsync(
            Guid adminUserId,
            Guid ticketId,
            ApplySupportTicketDecisionRequest request,
            CancellationToken cancellationToken = default);

        Task<SupportTicketResponse> ResolveTicketAsync(
            Guid adminUserId,
            Guid ticketId,
            ResolveSupportTicketRequest request,
            CancellationToken cancellationToken = default);
    }
}