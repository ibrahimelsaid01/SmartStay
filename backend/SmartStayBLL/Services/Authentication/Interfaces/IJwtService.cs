using SmartStayDAL;

namespace SmartStayBLL
{
    public interface IJwtService
    {
        AccessTokenResult GenerateAccessToken(
            ApplicationUser user,
            IReadOnlyCollection<string> roles);
    }
}