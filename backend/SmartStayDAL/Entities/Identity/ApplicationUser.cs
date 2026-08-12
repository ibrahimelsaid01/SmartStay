using Microsoft.AspNetCore.Identity;

namespace SmartStayDAL
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? ProfileImageUrl { get; set; }

        public string? ProfileImagePublicId { get; set; }

        public UserGender? Gender { get; set; }

        public DateOnly? Birthday { get; set; }

        public string? Country { get; set; }

        public string? Address { get; set; }

        public string? ZipCode { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsProfileCompleted { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        public HostProfile? HostProfile { get; set; }

        public ICollection<RefreshToken> RefreshTokens
        { get; set; } = new List<RefreshToken>();

        public ICollection<OtpCode> OtpCodes
        { get; set; } = new List<OtpCode>();
    }
}