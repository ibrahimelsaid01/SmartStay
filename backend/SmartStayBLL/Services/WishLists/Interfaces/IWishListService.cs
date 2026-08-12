namespace SmartStayBLL
{
    public interface IWishListService
    {
        Task<WishListsResponse> GetAllAsync(
            Guid userId,
            Guid? propertyId,
            CancellationToken cancellationToken = default);

        Task<WishListDetailsResponse> GetByIdAsync(
            Guid userId,
            Guid wishListId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<WishListSummaryResponse> CreateAsync(
            Guid userId,
            CreateWishListRequest request,
            CancellationToken cancellationToken = default);

        Task<WishListSummaryResponse> UpdateAsync(
            Guid userId,
            Guid wishListId,
            UpdateWishListRequest request,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid userId,
            Guid wishListId,
            CancellationToken cancellationToken = default);

        Task<WishListItemResponse> AddItemAsync(
            Guid userId,
            Guid wishListId,
            AddWishListItemRequest request,
            CancellationToken cancellationToken = default);

        Task RemoveItemAsync(
            Guid userId,
            Guid wishListId,
            Guid propertyId,
            CancellationToken cancellationToken = default);

        Task<WishListItemResponse> UpdateItemNoteAsync(
            Guid userId,
            Guid wishListId,
            Guid propertyId,
            UpdateWishListItemNoteRequest request,
            CancellationToken cancellationToken = default);
    }
}