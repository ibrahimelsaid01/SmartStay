using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartStayDAL
{
    public class RoleConfiguration
        : IEntityTypeConfiguration<IdentityRole<Guid>>
    {
        public void Configure(
            EntityTypeBuilder<IdentityRole<Guid>> builder)
        {
            var adminRoleId = Guid.Parse(
                "9a1b2c3d-4e5f-6a7b-8c9d-0e1f2a3b4c5d");

            var hostRoleId = Guid.Parse(
                "8b2c3d4e-5f6a-7b8c-9d0e-1f2a3b4c5d6e");

            var userRoleId = Guid.Parse(
                "7c3d4e5f-6a7b-8c9d-0e1f-2a3b4c5d6e7f");

            builder.HasData(
                new IdentityRole<Guid>
                {
                    Id = adminRoleId,
                    Name = RoleNames.Admin,
                    NormalizedName = RoleNames.Admin.ToUpperInvariant(),
                    ConcurrencyStamp =
                        "1a1b2c3d-4e5f-6a7b-8c9d-0e1f2a3b4c5d"
                },
                new IdentityRole<Guid>
                {
                    Id = hostRoleId,
                    Name = RoleNames.Host,
                    NormalizedName = RoleNames.Host.ToUpperInvariant(),
                    ConcurrencyStamp =
                        "2b2c3d4e-5f6a-7b8c-9d0e-1f2a3b4c5d"
                },
                new IdentityRole<Guid>
                {
                    Id = userRoleId,
                    Name = RoleNames.User,
                    NormalizedName = RoleNames.User.ToUpperInvariant(),
                    ConcurrencyStamp =
                        "3c3d4e5f-6a7b-8c9d-0e1f2a3b4c5d"
                }
            );
        }
    }
}