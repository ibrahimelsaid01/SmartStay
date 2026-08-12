namespace SmartStayBLL
{
    public interface IPropertyRatingQueryService
    {
        Task<PropertyRatingSnapshot> GetAsync(
            Guid propertyId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, PropertyRatingSnapshot>>
            GetManyAsync(
                IEnumerable<Guid> propertyIds,
                CancellationToken cancellationToken = default);
    }
}