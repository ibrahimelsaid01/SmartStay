using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using SmartStayDAL;
using System.Security.Claims;

namespace SmartStay.Api
{
    public sealed class ActiveAccountJwtBearerEvents
        : JwtBearerEvents
    {
        public override async Task TokenValidated(
            TokenValidatedContext context)
        {
            var userIdValue =
                context.Principal?
                    .FindFirstValue(
                        ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(
                    userIdValue,
                    out var userId))
            {
                context.Fail(
                    "The access token does not contain a valid user identifier.");

                return;
            }

            var dbContext =
                context.HttpContext
                    .RequestServices
                    .GetRequiredService<
                        SmartStayDbContext>();

            var isActiveUser =
                await dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.Id == userId
                            &&
                            user.IsActive,
                        context.HttpContext
                            .RequestAborted);

            if (!isActiveUser)
            {
                context.Fail(
                    "The user account is inactive.");
            }
        }
    }
}