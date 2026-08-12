using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartStayDAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutsRestrictionsSupportEvidenceAndAdminLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DecidedAt",
                table: "SupportTickets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DecidedByAdminId",
                table: "SupportTickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DecisionAction",
                table: "SupportTickets",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "DecisionNote",
                table: "SupportTickets",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DecisionStatus",
                table: "SupportTickets",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptedBookingTerms",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptedCancellationPolicy",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptedComplaintPolicy",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptedPropertyRules",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BookingTermsAcceptedAt",
                table: "Bookings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminActionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminActionLogs", x => x.Id);
                    table.CheckConstraint("CK_AdminActionLogs_ActionType_Valid", "[ActionType] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 99)");
                    table.CheckConstraint("CK_AdminActionLogs_Summary_NotEmpty", "LEN(LTRIM(RTRIM([Summary]))) > 0");
                    table.CheckConstraint("CK_AdminActionLogs_TargetType_Valid", "[TargetType] BETWEEN 1 AND 11");
                    table.ForeignKey(
                        name: "FK_AdminActionLogs_AspNetUsers_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingPayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HostProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HeldAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HoldReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReleaseNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BlockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BlockReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RefundedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPayouts", x => x.Id);
                    table.CheckConstraint("CK_BookingPayouts_Amount_Positive", "[Amount] > 0");
                    table.CheckConstraint("CK_BookingPayouts_Blocked_Requires_BlockedAt", "[Status] <> 5 OR [BlockedAt] IS NOT NULL");
                    table.CheckConstraint("CK_BookingPayouts_Currency_Length", "LEN([Currency]) = 3");
                    table.CheckConstraint("CK_BookingPayouts_Held_Requires_HeldAt", "[Status] <> 2 OR [HeldAt] IS NOT NULL");
                    table.CheckConstraint("CK_BookingPayouts_Paid_Requires_PaidAt", "[Status] <> 4 OR [PaidAt] IS NOT NULL");
                    table.CheckConstraint("CK_BookingPayouts_Refunded_Requires_RefundedAt", "[Status] <> 6 OR [RefundedAt] IS NOT NULL");
                    table.CheckConstraint("CK_BookingPayouts_Status_Valid", "[Status] IN (1, 2, 3, 4, 5, 6)");
                    table.ForeignKey(
                        name: "FK_BookingPayouts_BookingPayments_BookingPaymentId",
                        column: x => x.BookingPaymentId,
                        principalTable: "BookingPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingPayouts_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingPayouts_HostProfiles_HostProfileId",
                        column: x => x.HostProfileId,
                        principalTable: "HostProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupportTicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketAttachments", x => x.Id);
                    table.CheckConstraint("CK_SupportTicketAttachments_ContentType_NotEmpty", "LEN(LTRIM(RTRIM([ContentType]))) > 0");
                    table.CheckConstraint("CK_SupportTicketAttachments_FileName_NotEmpty", "LEN(LTRIM(RTRIM([FileName]))) > 0");
                    table.CheckConstraint("CK_SupportTicketAttachments_FileSize_Positive", "[FileSizeInBytes] > 0");
                    table.CheckConstraint("CK_SupportTicketAttachments_PublicId_NotEmpty", "LEN(LTRIM(RTRIM([PublicId]))) > 0");
                    table.CheckConstraint("CK_SupportTicketAttachments_Type_Valid", "[Type] BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_SupportTicketAttachments_Url_NotEmpty", "LEN(LTRIM(RTRIM([Url]))) > 0");
                    table.ForeignKey(
                        name: "FK_SupportTicketAttachments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTicketAttachments_SupportTickets_SupportTicketId",
                        column: x => x.SupportTicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBookingRestrictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CancellationCountSnapshot = table.Column<int>(type: "int", nullable: false),
                    RestrictedFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RestrictedUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBySystem = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RemovedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RemovalNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBookingRestrictions", x => x.Id);
                    table.CheckConstraint("CK_UserBookingRestrictions_CancellationCount_NonNegative", "[CancellationCountSnapshot] >= 0");
                    table.CheckConstraint("CK_UserBookingRestrictions_Reason_NotEmpty", "LEN(LTRIM(RTRIM([Reason]))) > 0");
                    table.CheckConstraint("CK_UserBookingRestrictions_Removed_State", "([Status] <> 3) OR ([RemovedAt] IS NOT NULL)");
                    table.CheckConstraint("CK_UserBookingRestrictions_RestrictedUntil_After_From", "([RestrictedUntil] IS NULL) OR ([RestrictedUntil] > [RestrictedFrom])");
                    table.CheckConstraint("CK_UserBookingRestrictions_Status_Valid", "[Status] BETWEEN 1 AND 3");
                    table.CheckConstraint("CK_UserBookingRestrictions_TemporaryRestriction_Requires_Until", "([Type] <> 2) OR ([RestrictedUntil] IS NOT NULL)");
                    table.CheckConstraint("CK_UserBookingRestrictions_Type_Valid", "[Type] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_UserBookingRestrictions_AspNetUsers_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBookingRestrictions_AspNetUsers_RemovedByAdminId",
                        column: x => x.RemovedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBookingRestrictions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_DecidedByAdminId",
                table: "SupportTickets",
                column: "DecidedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_DecisionStatus_DecisionAction",
                table: "SupportTickets",
                columns: new[] { "DecisionStatus", "DecisionAction" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_SupportTickets_DecidedBy_Requires_DecidedAt",
                table: "SupportTickets",
                sql: "([DecidedByAdminId] IS NULL) OR ([DecidedAt] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SupportTickets_DecisionAction_Valid",
                table: "SupportTickets",
                sql: "[DecisionAction] BETWEEN 1 AND 7");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SupportTickets_DecisionStatus_Valid",
                table: "SupportTickets",
                sql: "[DecisionStatus] BETWEEN 1 AND 4");

            migrationBuilder.CreateIndex(
                name: "IX_AdminActionLogs_ActionType_CreatedAt",
                table: "AdminActionLogs",
                columns: new[] { "ActionType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminActionLogs_AdminUserId",
                table: "AdminActionLogs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminActionLogs_CreatedAt",
                table: "AdminActionLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminActionLogs_Target_CreatedAt",
                table: "AdminActionLogs",
                columns: new[] { "TargetType", "TargetId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayouts_Host_Status_CreatedAt",
                table: "BookingPayouts",
                columns: new[] { "HostProfileId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayouts_HostProfileId",
                table: "BookingPayouts",
                column: "HostProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayouts_Status_AvailableAt",
                table: "BookingPayouts",
                columns: new[] { "Status", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "UX_BookingPayouts_BookingId",
                table: "BookingPayouts",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_BookingPayouts_BookingPaymentId",
                table: "BookingPayouts",
                column: "BookingPaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketAttachments_CreatedAt",
                table: "SupportTicketAttachments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketAttachments_TicketId",
                table: "SupportTicketAttachments",
                column: "SupportTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketAttachments_UploadedByUserId",
                table: "SupportTicketAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBookingRestrictions_CreatedByAdminId",
                table: "UserBookingRestrictions",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBookingRestrictions_RemovedByAdminId",
                table: "UserBookingRestrictions",
                column: "RemovedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBookingRestrictions_Status_CreatedAt",
                table: "UserBookingRestrictions",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBookingRestrictions_User_Status_Type_Until",
                table: "UserBookingRestrictions",
                columns: new[] { "UserId", "Status", "Type", "RestrictedUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBookingRestrictions_UserId",
                table: "UserBookingRestrictions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportTickets_AspNetUsers_DecidedByAdminId",
                table: "SupportTickets",
                column: "DecidedByAdminId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportTickets_AspNetUsers_DecidedByAdminId",
                table: "SupportTickets");

            migrationBuilder.DropTable(
                name: "AdminActionLogs");

            migrationBuilder.DropTable(
                name: "BookingPayouts");

            migrationBuilder.DropTable(
                name: "SupportTicketAttachments");

            migrationBuilder.DropTable(
                name: "UserBookingRestrictions");

            migrationBuilder.DropIndex(
                name: "IX_SupportTickets_DecidedByAdminId",
                table: "SupportTickets");

            migrationBuilder.DropIndex(
                name: "IX_SupportTickets_DecisionStatus_DecisionAction",
                table: "SupportTickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SupportTickets_DecidedBy_Requires_DecidedAt",
                table: "SupportTickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SupportTickets_DecisionAction_Valid",
                table: "SupportTickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SupportTickets_DecisionStatus_Valid",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "DecidedAt",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "DecidedByAdminId",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "DecisionAction",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "DecisionNote",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "DecisionStatus",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "AcceptedBookingTerms",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AcceptedCancellationPolicy",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AcceptedComplaintPolicy",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AcceptedPropertyRules",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookingTermsAcceptedAt",
                table: "Bookings");
        }
    }
}
