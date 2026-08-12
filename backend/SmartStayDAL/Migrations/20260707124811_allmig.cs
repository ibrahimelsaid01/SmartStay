using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartStayDAL.Migrations
{
    /// <inheritdoc />
    public partial class allmig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Amenities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    IconKey = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amenities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProfileImagePublicId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    Birthday = table.Column<DateOnly>(type: "date", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsProfileCompleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ProviderEventId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentWebhookEvents", x => x.Id);
                    table.CheckConstraint("CK_PaymentWebhookEvents_EventType_NotEmpty", "LEN(LTRIM(RTRIM([EventType]))) > 0");
                    table.CheckConstraint("CK_PaymentWebhookEvents_Provider_NotEmpty", "LEN(LTRIM(RTRIM([Provider]))) > 0");
                    table.CheckConstraint("CK_PaymentWebhookEvents_ProviderEventId_NotEmpty", "LEN(LTRIM(RTRIM([ProviderEventId]))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HostProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ProfileImagePublicId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HostProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReferenceType = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeduplicationKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.CheckConstraint("CK_Notifications_ReadAt_Valid", "[ReadAt] IS NULL OR [ReadAt] >= [CreatedAt]");
                    table.CheckConstraint("CK_Notifications_Reference_Valid", "([ReferenceType] = 0 AND [ReferenceId] IS NULL) OR ([ReferenceType] <> 0 AND [ReferenceId] IS NOT NULL)");
                    table.CheckConstraint("CK_Notifications_ReferenceType_Valid", "[ReferenceType] BETWEEN 0 AND 5");
                    table.CheckConstraint("CK_Notifications_Type_Valid", "[Type] BETWEEN 1 AND 16");
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OtpCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InvalidatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtpCodes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId",
                        column: x => x.ReplacedByTokenId,
                        principalTable: "RefreshTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WishLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WishLists_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HostIdentityDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HostProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrontPublicId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FrontFormat = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BackPublicId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BackFormat = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostIdentityDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HostIdentityDocuments_HostProfiles_HostProfileId",
                        column: x => x.HostProfileId,
                        principalTable: "HostProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HostProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    PropertyType = table.Column<int>(type: "int", nullable: false),
                    SpaceType = table.Column<int>(type: "int", nullable: false),
                    MaxGuests = table.Column<int>(type: "int", nullable: true),
                    Bedrooms = table.Column<int>(type: "int", nullable: true),
                    Beds = table.Column<int>(type: "int", nullable: true),
                    Bathrooms = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    PricePerNight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StreetAddress = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    BuildingNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Floor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ApartmentNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    CheckInTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CheckOutTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CancellationPolicy = table.Column<int>(type: "int", nullable: true, defaultValue: 2),
                    AllowsSmoking = table.Column<bool>(type: "bit", nullable: true),
                    AllowsPets = table.Column<bool>(type: "bit", nullable: true),
                    AllowsParties = table.Column<bool>(type: "bit", nullable: true),
                    AllowsChildren = table.Column<bool>(type: "bit", nullable: true),
                    AdditionalHouseRules = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Properties_HostProfiles_HostProfileId",
                        column: x => x.HostProfileId,
                        principalTable: "HostProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuestUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOutDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GuestsCount = table.Column<int>(type: "int", nullable: false),
                    Nights = table.Column<int>(type: "int", nullable: false),
                    PricePerNight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ServiceFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    CancellationPolicySnapshot = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "DATEADD(MINUTE, 15, TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00'))"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.CheckConstraint("CK_Bookings_CancellationPolicySnapshot_Valid", "[CancellationPolicySnapshot] IN (1, 2, 3)");
                    table.CheckConstraint("CK_Bookings_Cancelled_Requires_CancelledAt", "[Status] <> 3 OR [CancelledAt] IS NOT NULL");
                    table.CheckConstraint("CK_Bookings_CheckOutDate_After_CheckInDate", "[CheckOutDate] > [CheckInDate]");
                    table.CheckConstraint("CK_Bookings_Completed_Requires_CompletedAt", "[Status] <> 4 OR [CompletedAt] IS NOT NULL");
                    table.CheckConstraint("CK_Bookings_Confirmed_Requires_ConfirmedAt", "[Status] <> 2 OR [ConfirmedAt] IS NOT NULL");
                    table.CheckConstraint("CK_Bookings_Expired_Requires_ExpiredAt", "[Status] <> 5 OR [ExpiredAt] IS NOT NULL");
                    table.CheckConstraint("CK_Bookings_GuestsCount_Positive", "[GuestsCount] > 0");
                    table.CheckConstraint("CK_Bookings_Nights_Positive", "[Nights] > 0");
                    table.CheckConstraint("CK_Bookings_Pending_Requires_ExpiresAt", "[Status] <> 1 OR [ExpiresAt] IS NOT NULL");
                    table.CheckConstraint("CK_Bookings_PricePerNight_NonNegative", "[PricePerNight] >= 0");
                    table.CheckConstraint("CK_Bookings_ServiceFee_NonNegative", "[ServiceFee] >= 0");
                    table.CheckConstraint("CK_Bookings_Status_Valid", "[Status] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_Bookings_Subtotal_NonNegative", "[Subtotal] >= 0");
                    table.CheckConstraint("CK_Bookings_TotalAmount_NonNegative", "[TotalAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_Bookings_AspNetUsers_GuestUserId",
                        column: x => x.GuestUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PropertyAmenities",
                columns: table => new
                {
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmenityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyAmenities", x => new { x.PropertyId, x.AmenityId });
                    table.ForeignKey(
                        name: "FK_PropertyAmenities_Amenities_AmenityId",
                        column: x => x.AmenityId,
                        principalTable: "Amenities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyAmenities_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Format = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    IsCover = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyImages_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyVerificationDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyVerificationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyVerificationDocuments_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WishListItems",
                columns: table => new
                {
                    WishListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AddedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishListItems", x => new { x.WishListId, x.PropertyId });
                    table.ForeignKey(
                        name: "FK_WishListItems_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WishListItems_WishLists_WishListId",
                        column: x => x.WishListId,
                        principalTable: "WishLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Provider = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ProviderPaymentId = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    ProviderReference = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    FailureCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SucceededAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RefundedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPayments", x => x.Id);
                    table.CheckConstraint("CK_BookingPayments_Amount_Positive", "[Amount] > 0");
                    table.CheckConstraint("CK_BookingPayments_Cancelled_Requires_CancelledAt", "[Status] <> 4 OR [CancelledAt] IS NOT NULL");
                    table.CheckConstraint("CK_BookingPayments_Currency_Length", "LEN([Currency]) = 3");
                    table.CheckConstraint("CK_BookingPayments_Failed_Requires_FailedAt", "[Status] <> 3 OR [FailedAt] IS NOT NULL");
                    table.CheckConstraint("CK_BookingPayments_FullRefund_Valid", "[Status] <> 6 OR ([SucceededAt] IS NOT NULL AND [RefundedAt] IS NOT NULL AND [RefundedAmount] = [Amount])");
                    table.CheckConstraint("CK_BookingPayments_IdempotencyKey_NotEmpty", "LEN(LTRIM(RTRIM([IdempotencyKey]))) > 0");
                    table.CheckConstraint("CK_BookingPayments_NonRefund_Status_Valid", "[Status] IN (5, 6) OR ([RefundedAmount] = 0 AND [RefundedAt] IS NULL)");
                    table.CheckConstraint("CK_BookingPayments_PartialRefund_Valid", "[Status] <> 5 OR ([SucceededAt] IS NOT NULL AND [RefundedAt] IS NOT NULL AND [RefundedAmount] > 0 AND [RefundedAmount] < [Amount])");
                    table.CheckConstraint("CK_BookingPayments_Provider_NotEmpty", "LEN(LTRIM(RTRIM([Provider]))) > 0");
                    table.CheckConstraint("CK_BookingPayments_RefundedAmount_Valid", "[RefundedAmount] >= 0 AND [RefundedAmount] <= [Amount]");
                    table.CheckConstraint("CK_BookingPayments_Status_Valid", "[Status] IN (1, 2, 3, 4, 5, 6)");
                    table.CheckConstraint("CK_BookingPayments_Succeeded_Requires_SucceededAt", "[Status] <> 2 OR [SucceededAt] IS NOT NULL");
                    table.CheckConstraint("CK_BookingPayments_SucceededAt_Status_Valid", "[SucceededAt] IS NULL OR [Status] IN (2, 5, 6)");
                    table.ForeignKey(
                        name: "FK_BookingPayments_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    PositiveComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NegativeComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModeratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModeratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.CheckConstraint("CK_Reviews_Comment_Required", "LEN(LTRIM(RTRIM(ISNULL([PositiveComment], '')))) > 0 OR LEN(LTRIM(RTRIM(ISNULL([NegativeComment], '')))) > 0");
                    table.CheckConstraint("CK_Reviews_Pending_State_Valid", "[Status] <> 1 OR ([ModeratedByUserId] IS NULL AND [ModeratedAt] IS NULL AND [PublishedAt] IS NULL AND [RejectedAt] IS NULL AND [RejectionReason] IS NULL)");
                    table.CheckConstraint("CK_Reviews_Posted_State_Valid", "[Status] <> 2 OR ([ModeratedByUserId] IS NOT NULL AND [ModeratedAt] IS NOT NULL AND [PublishedAt] IS NOT NULL AND [RejectedAt] IS NULL AND [RejectionReason] IS NULL)");
                    table.CheckConstraint("CK_Reviews_Rating_Valid", "[Rating] BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_Reviews_Rejected_State_Valid", "[Status] <> 3 OR ([ModeratedByUserId] IS NOT NULL AND [ModeratedAt] IS NOT NULL AND [RejectedAt] IS NOT NULL AND [PublishedAt] IS NULL AND LEN(LTRIM(RTRIM(ISNULL([RejectionReason], '')))) > 0)");
                    table.CheckConstraint("CK_Reviews_Status_Valid", "[Status] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_Reviews_AspNetUsers_ModeratedByUserId",
                        column: x => x.ModeratedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Urgency = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                    table.CheckConstraint("CK_SupportTickets_Category_Valid", "[Category] BETWEEN 1 AND 9");
                    table.CheckConstraint("CK_SupportTickets_Description_NotEmpty", "LEN(LTRIM(RTRIM([Description]))) > 0");
                    table.CheckConstraint("CK_SupportTickets_Resolved_State", "([Status] IN (3, 4) AND [ResolvedAt] IS NOT NULL) OR ([Status] IN (1, 2) AND [ResolvedAt] IS NULL)");
                    table.CheckConstraint("CK_SupportTickets_ResolvedBy_Requires_ResolvedAt", "([ResolvedByAdminId] IS NULL) OR ([ResolvedAt] IS NOT NULL)");
                    table.CheckConstraint("CK_SupportTickets_Status_Valid", "[Status] BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_SupportTickets_Subject_NotEmpty", "LEN(LTRIM(RTRIM([Subject]))) > 0");
                    table.CheckConstraint("CK_SupportTickets_Urgency_Valid", "[Urgency] BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_SupportTickets_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTickets_AspNetUsers_ResolvedByAdminId",
                        column: x => x.ResolvedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PropertyVerificationDocumentPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerificationDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Format = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyVerificationDocumentPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyVerificationDocumentPages_PropertyVerificationDocuments_VerificationDocumentId",
                        column: x => x.VerificationDocumentId,
                        principalTable: "PropertyVerificationDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingPaymentRefunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Provider = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ProviderRefundId = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FailureReason = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SucceededAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPaymentRefunds", x => x.Id);
                    table.CheckConstraint("CK_BookingPaymentRefunds_Amount_Positive", "[Amount] > 0");
                    table.CheckConstraint("CK_BookingPaymentRefunds_Cancelled_Requires_CancelledAt", "[Status] <> 5 OR [CancelledAt] IS NOT NULL");
                    table.CheckConstraint("CK_BookingPaymentRefunds_CancelledAt_Status_Valid", "[CancelledAt] IS NULL OR [Status] = 5");
                    table.CheckConstraint("CK_BookingPaymentRefunds_CancelledAt_Valid", "[CancelledAt] IS NULL OR [CancelledAt] >= [CreatedAt]");
                    table.CheckConstraint("CK_BookingPaymentRefunds_Currency_Length", "LEN([Currency]) = 3");
                    table.CheckConstraint("CK_BookingPaymentRefunds_Failed_Requires_FailedAt", "[Status] <> 4 OR [FailedAt] IS NOT NULL");
                    table.CheckConstraint("CK_BookingPaymentRefunds_FailedAt_Status_Valid", "[FailedAt] IS NULL OR [Status] = 4");
                    table.CheckConstraint("CK_BookingPaymentRefunds_FailedAt_Valid", "[FailedAt] IS NULL OR [FailedAt] >= [CreatedAt]");
                    table.CheckConstraint("CK_BookingPaymentRefunds_IdempotencyKey_NotEmpty", "LEN(LTRIM(RTRIM([IdempotencyKey]))) > 0");
                    table.CheckConstraint("CK_BookingPaymentRefunds_Provider_NotEmpty", "LEN(LTRIM(RTRIM([Provider]))) > 0");
                    table.CheckConstraint("CK_BookingPaymentRefunds_ProviderRefundId_NotEmpty", "[ProviderRefundId] IS NULL OR LEN(LTRIM(RTRIM([ProviderRefundId]))) > 0");
                    table.CheckConstraint("CK_BookingPaymentRefunds_Status_Valid", "[Status] IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_BookingPaymentRefunds_Succeeded_Requires_SucceededAt", "[Status] <> 3 OR [SucceededAt] IS NOT NULL");
                    table.CheckConstraint("CK_BookingPaymentRefunds_SucceededAt_Status_Valid", "[SucceededAt] IS NULL OR [Status] = 3");
                    table.CheckConstraint("CK_BookingPaymentRefunds_SucceededAt_Valid", "[SucceededAt] IS NULL OR [SucceededAt] >= [CreatedAt]");
                    table.CheckConstraint("CK_BookingPaymentRefunds_UpdatedAt_Valid", "[UpdatedAt] IS NULL OR [UpdatedAt] >= [CreatedAt]");
                    table.ForeignKey(
                        name: "FK_BookingPaymentRefunds_BookingPayments_BookingPaymentId",
                        column: x => x.BookingPaymentId,
                        principalTable: "BookingPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReviewHelpfulVotes",
                columns: table => new
                {
                    ReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewHelpfulVotes", x => new { x.ReviewId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ReviewHelpfulVotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewHelpfulVotes_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewReplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HostProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewReplies_HostProfiles_HostProfileId",
                        column: x => x.HostProfileId,
                        principalTable: "HostProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewReplies_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupportTicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsAdminMessage = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketMessages", x => x.Id);
                    table.CheckConstraint("CK_SupportTicketMessages_Message_NotEmpty", "LEN(LTRIM(RTRIM([Message]))) > 0");
                    table.ForeignKey(
                        name: "FK_SupportTicketMessages_AspNetUsers_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTicketMessages_SupportTickets_SupportTicketId",
                        column: x => x.SupportTicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Amenities",
                columns: new[] { "Id", "Category", "Code", "DisplayOrder", "IconKey", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), 1, "wifi", 1, "wifi", true, "Wi-Fi" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), 1, "air_conditioning", 2, "snowflake", true, "Air Conditioning" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), 1, "heating", 3, "flame", true, "Heating" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), 1, "washer", 4, "washer", true, "Washer" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), 1, "iron", 5, "iron", true, "Iron" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), 1, "workspace", 6, "briefcase", true, "Dedicated Workspace" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), 1, "bed_linens", 7, "bed", true, "Bed Linens" },
                    { new Guid("20000000-0000-0000-0000-000000000001"), 2, "kitchen", 1, "cooking-pot", true, "Kitchen" },
                    { new Guid("20000000-0000-0000-0000-000000000002"), 2, "refrigerator", 2, "refrigerator", true, "Refrigerator" },
                    { new Guid("20000000-0000-0000-0000-000000000003"), 2, "microwave", 3, "microwave", true, "Microwave" },
                    { new Guid("20000000-0000-0000-0000-000000000004"), 2, "oven", 4, "oven", true, "Oven" },
                    { new Guid("20000000-0000-0000-0000-000000000005"), 2, "stove", 5, "stove", true, "Stove" },
                    { new Guid("20000000-0000-0000-0000-000000000006"), 2, "coffee_maker", 6, "coffee", true, "Coffee Maker" },
                    { new Guid("20000000-0000-0000-0000-000000000007"), 2, "kettle", 7, "kettle", true, "Electric Kettle" },
                    { new Guid("20000000-0000-0000-0000-000000000008"), 2, "dining_area", 8, "utensils", true, "Dining Area" },
                    { new Guid("30000000-0000-0000-0000-000000000001"), 3, "hot_water", 1, "shower-head", true, "Hot Water" },
                    { new Guid("30000000-0000-0000-0000-000000000002"), 3, "hair_dryer", 2, "wind", true, "Hair Dryer" },
                    { new Guid("30000000-0000-0000-0000-000000000003"), 3, "bathtub", 3, "bath", true, "Bathtub" },
                    { new Guid("30000000-0000-0000-0000-000000000004"), 3, "towels", 4, "towel", true, "Towels" },
                    { new Guid("30000000-0000-0000-0000-000000000005"), 3, "toiletries", 5, "package", true, "Toiletries" },
                    { new Guid("40000000-0000-0000-0000-000000000001"), 4, "tv", 1, "tv", true, "TV" },
                    { new Guid("40000000-0000-0000-0000-000000000002"), 4, "streaming_services", 2, "play", true, "Streaming Services" },
                    { new Guid("40000000-0000-0000-0000-000000000003"), 4, "books", 3, "book-open", true, "Books" },
                    { new Guid("40000000-0000-0000-0000-000000000004"), 4, "board_games", 4, "gamepad", true, "Board Games" },
                    { new Guid("50000000-0000-0000-0000-000000000001"), 5, "balcony", 1, "building", true, "Balcony" },
                    { new Guid("50000000-0000-0000-0000-000000000002"), 5, "garden", 2, "trees", true, "Garden" },
                    { new Guid("50000000-0000-0000-0000-000000000003"), 5, "patio", 3, "armchair", true, "Patio" },
                    { new Guid("50000000-0000-0000-0000-000000000004"), 5, "bbq_area", 4, "cooking-pot", true, "BBQ Area" },
                    { new Guid("50000000-0000-0000-0000-000000000005"), 5, "swimming_pool", 5, "waves", true, "Swimming Pool" },
                    { new Guid("60000000-0000-0000-0000-000000000001"), 6, "free_parking", 1, "car", true, "Free Parking" },
                    { new Guid("60000000-0000-0000-0000-000000000002"), 6, "paid_parking", 2, "circle-dollar-sign", true, "Paid Parking" },
                    { new Guid("60000000-0000-0000-0000-000000000003"), 6, "street_parking", 3, "parking-circle", true, "Street Parking" },
                    { new Guid("60000000-0000-0000-0000-000000000004"), 6, "private_entrance", 4, "door-open", true, "Private Entrance" },
                    { new Guid("70000000-0000-0000-0000-000000000001"), 7, "smoke_alarm", 1, "alarm-smoke", true, "Smoke Alarm" },
                    { new Guid("70000000-0000-0000-0000-000000000002"), 7, "carbon_monoxide_alarm", 2, "badge-alert", true, "Carbon Monoxide Alarm" },
                    { new Guid("70000000-0000-0000-0000-000000000003"), 7, "fire_extinguisher", 3, "fire-extinguisher", true, "Fire Extinguisher" },
                    { new Guid("70000000-0000-0000-0000-000000000004"), 7, "first_aid_kit", 4, "briefcase-medical", true, "First Aid Kit" },
                    { new Guid("70000000-0000-0000-0000-000000000005"), 7, "safe", 5, "lock-keyhole", true, "Safe" },
                    { new Guid("80000000-0000-0000-0000-000000000001"), 8, "elevator", 1, "arrow-up-down", true, "Elevator" },
                    { new Guid("80000000-0000-0000-0000-000000000002"), 8, "step_free_entrance", 2, "move-horizontal", true, "Step-Free Entrance" },
                    { new Guid("80000000-0000-0000-0000-000000000003"), 8, "wheelchair_accessible", 3, "accessibility", true, "Wheelchair Accessible" },
                    { new Guid("80000000-0000-0000-0000-000000000004"), 8, "accessible_parking", 4, "square-parking", true, "Accessible Parking" },
                    { new Guid("80000000-0000-0000-0000-000000000005"), 8, "wide_doorways", 5, "door-open", true, "Wide Doorways" }
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("7c3d4e5f-6a7b-8c9d-0e1f-2a3b4c5d6e7f"), "3c3d4e5f-6a7b-8c9d-0e1f2a3b4c5d", "User", "USER" },
                    { new Guid("8b2c3d4e-5f6a-7b8c-9d0e-1f2a3b4c5d6e"), "2b2c3d4e-5f6a-7b8c-9d0e-1f2a3b4c5d", "Host", "HOST" },
                    { new Guid("9a1b2c3d-4e5f-6a7b-8c9d-0e1f2a3b4c5d"), "1a1b2c3d-4e5f-6a7b-8c9d-0e1f2a3b4c5d", "Admin", "ADMIN" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Amenities_Code",
                table: "Amenities",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Amenities_IsActive_Category_DisplayOrder",
                table: "Amenities",
                columns: new[] { "IsActive", "Category", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingPaymentRefunds_Payment_CreatedAt",
                table: "BookingPaymentRefunds",
                columns: new[] { "BookingPaymentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingPaymentRefunds_Status_CreatedAt",
                table: "BookingPaymentRefunds",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_BookingPaymentRefunds_Payment_IdempotencyKey",
                table: "BookingPaymentRefunds",
                columns: new[] { "BookingPaymentId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_BookingPaymentRefunds_Provider_RefundId",
                table: "BookingPaymentRefunds",
                columns: new[] { "Provider", "ProviderRefundId" },
                unique: true,
                filter: "[ProviderRefundId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayments_Booking_CreatedAt",
                table: "BookingPayments",
                columns: new[] { "BookingId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayments_Status_CreatedAt",
                table: "BookingPayments",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_BookingPayments_Booking_IdempotencyKey",
                table: "BookingPayments",
                columns: new[] { "BookingId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_BookingPayments_Booking_Pending",
                table: "BookingPayments",
                column: "BookingId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_BookingPayments_Booking_Succeeded",
                table: "BookingPayments",
                column: "BookingId",
                unique: true,
                filter: "[SucceededAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_BookingPayments_Provider_PaymentId",
                table: "BookingPayments",
                columns: new[] { "Provider", "ProviderPaymentId" },
                unique: true,
                filter: "[ProviderPaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Guest_CreatedAt",
                table: "Bookings",
                columns: new[] { "GuestUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Property_Status_Dates",
                table: "Bookings",
                columns: new[] { "PropertyId", "Status", "CheckInDate", "CheckOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status_CheckOutDate",
                table: "Bookings",
                columns: new[] { "Status", "CheckOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status_ExpiresAt",
                table: "Bookings",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HostIdentityDocuments_HostProfileId",
                table: "HostIdentityDocuments",
                column: "HostProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HostProfiles_UserId",
                table: "HostProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Reference",
                table: "Notifications",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_DeduplicationKey_Unique",
                table: "Notifications",
                columns: new[] { "UserId", "DeduplicationKey" },
                unique: true,
                filter: "[DeduplicationKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_ReadAt_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "ReadAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_ExpiresAt",
                table: "OtpCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_NormalizedEmail_Purpose_CreatedAt",
                table: "OtpCodes",
                columns: new[] { "NormalizedEmail", "Purpose", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_UserId",
                table: "OtpCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentWebhookEvents_ReceivedAt",
                table: "PaymentWebhookEvents",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "UX_PaymentWebhookEvents_Provider_EventId",
                table: "PaymentWebhookEvents",
                columns: new[] { "Provider", "ProviderEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Properties_HostProfileId",
                table: "Properties",
                column: "HostProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_HostProfileId_Status",
                table: "Properties",
                columns: new[] { "HostProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Status",
                table: "Properties",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Status_Country_City",
                table: "Properties",
                columns: new[] { "Status", "Country", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAmenities_AmenityId",
                table: "PropertyAmenities",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyImages_PropertyId",
                table: "PropertyImages",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyImages_PropertyId_DisplayOrder",
                table: "PropertyImages",
                columns: new[] { "PropertyId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyImages_PropertyId_IsCover",
                table: "PropertyImages",
                columns: new[] { "PropertyId", "IsCover" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyVerificationDocumentPages_VerificationDocumentId",
                table: "PropertyVerificationDocumentPages",
                column: "VerificationDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyVerificationDocumentPages_VerificationDocumentId_PageNumber",
                table: "PropertyVerificationDocumentPages",
                columns: new[] { "VerificationDocumentId", "PageNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyVerificationDocuments_PropertyId",
                table: "PropertyVerificationDocuments",
                column: "PropertyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplacedByTokenId",
                table: "RefreshTokens",
                column: "ReplacedByTokenId",
                unique: true,
                filter: "[ReplacedByTokenId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewHelpfulVotes_CreatedAt",
                table: "ReviewHelpfulVotes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewHelpfulVotes_UserId",
                table: "ReviewHelpfulVotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewReplies_CreatedAt",
                table: "ReviewReplies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewReplies_HostProfileId",
                table: "ReviewReplies",
                column: "HostProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewReplies_ReviewId_Unique",
                table: "ReviewReplies",
                column: "ReviewId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookingId_Unique",
                table: "Reviews",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ModeratedByUserId",
                table: "Reviews",
                column: "ModeratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Property_Status_CreatedAt",
                table: "Reviews",
                columns: new[] { "PropertyId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Status_CreatedAt",
                table: "Reviews",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_User_CreatedAt",
                table: "Reviews",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketMessages_CreatedAt",
                table: "SupportTicketMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketMessages_SenderUserId",
                table: "SupportTicketMessages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketMessages_SupportTicketId",
                table: "SupportTicketMessages",
                column: "SupportTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_BookingId",
                table: "SupportTickets",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Category_Urgency",
                table: "SupportTickets",
                columns: new[] { "Category", "Urgency" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CreatedByUserId",
                table: "SupportTickets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_PropertyId",
                table: "SupportTickets",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_ResolvedByAdminId",
                table: "SupportTickets",
                column: "ResolvedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Status_CreatedAt",
                table: "SupportTickets",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WishListItems_AddedAt",
                table: "WishListItems",
                column: "AddedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WishListItems_PropertyId",
                table: "WishListItems",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_WishLists_UserId",
                table: "WishLists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WishLists_UserId_Name",
                table: "WishLists",
                columns: new[] { "UserId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BookingPaymentRefunds");

            migrationBuilder.DropTable(
                name: "HostIdentityDocuments");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OtpCodes");

            migrationBuilder.DropTable(
                name: "PaymentWebhookEvents");

            migrationBuilder.DropTable(
                name: "PropertyAmenities");

            migrationBuilder.DropTable(
                name: "PropertyImages");

            migrationBuilder.DropTable(
                name: "PropertyVerificationDocumentPages");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ReviewHelpfulVotes");

            migrationBuilder.DropTable(
                name: "ReviewReplies");

            migrationBuilder.DropTable(
                name: "SupportTicketMessages");

            migrationBuilder.DropTable(
                name: "WishListItems");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "BookingPayments");

            migrationBuilder.DropTable(
                name: "Amenities");

            migrationBuilder.DropTable(
                name: "PropertyVerificationDocuments");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropTable(
                name: "WishLists");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.DropTable(
                name: "HostProfiles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
