using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SmartStay.Api;
using SmartStayBLL;
using SmartStayDAL;
using System.Security.Claims;
using System.Text;

var builder =
    WebApplication.CreateBuilder(args);

// ======================================================
// 1. Controllers and global error handling
// ======================================================

builder.Services.AddControllers();

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();


// ======================================================
// 2. Database configuration
// ======================================================

var connectionString =
    builder.Configuration
        .GetConnectionString(
            "DefaultConnection")
    ??
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<
    SmartStayDbContext>(
        options =>
        {
            options.UseSqlServer(
                connectionString);

            /*
             * Creates booking/payment and moderation
             * notifications in the same SaveChanges call
             * as the associated domain state transition.
             */
            options.AddInterceptors(
                new BookingPaymentNotificationInterceptor(),
                new ModerationNotificationInterceptor());
        });


// ======================================================
// 3. ASP.NET Core Identity
// ======================================================

builder.Services
    .AddIdentityCore<ApplicationUser>(
        options =>
        {
            options.User.RequireUniqueEmail =
                true;

            /*
             * The user's email is considered confirmed
             * after successful OTP verification.
             */
            options.SignIn.RequireConfirmedEmail =
                true;
        })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<
        SmartStayDbContext>();


// ======================================================
// 4. OTP settings
// ======================================================

builder.Services
    .AddOptions<OtpSettings>()
    .Bind(
        builder.Configuration.GetSection(
            OtpSettings.SectionName))
    .Validate(
        settings =>
            settings.CodeLength == 6,
        "OTP code length must be 6.")
    .Validate(
        settings =>
            settings.ExpirationMinutes > 0,
        "OTP expiration must be greater than zero.")
    .Validate(
        settings =>
            settings.ResendCooldownSeconds > 0,
        "OTP resend cooldown must be greater than zero.")
    .Validate(
        settings =>
            settings.MaximumFailedAttempts > 0,
        "Maximum OTP failed attempts must be greater than zero.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.HashKey)
            &&
            Encoding.UTF8.GetByteCount(
                settings.HashKey) >= 32,
        "OtpSettings:HashKey must be at least 32 bytes.")
    .ValidateOnStart();


// ======================================================
// 5. SMTP settings
// ======================================================

builder.Services
    .AddOptions<SmtpSettings>()
    .Bind(
        builder.Configuration.GetSection(
            SmtpSettings.SectionName))
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.Host),
        "SmtpSettings:Host is required.")
    .Validate(
        settings =>
            settings.Port > 0,
        "SmtpSettings:Port must be greater than zero.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.Username),
        "SmtpSettings:Username is required.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.Password),
        "SmtpSettings:Password is required.")
    .ValidateOnStart();


// ======================================================
// 6. JWT settings
// ======================================================

builder.Services
    .AddOptions<JwtSettings>()
    .Bind(
        builder.Configuration.GetSection(
            JwtSettings.SectionName))
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.Key),
        "JwtSettings:Key is required.")
    .Validate(
        settings =>
            Encoding.UTF8.GetByteCount(
                settings.Key) >= 32,
        "JwtSettings:Key must be at least 32 bytes.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.Issuer),
        "JwtSettings:Issuer is required.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.Audience),
        "JwtSettings:Audience is required.")
    .Validate(
        settings =>
            settings.AccessTokenExpirationMinutes > 0,
        "Access token expiration must be greater than zero.")
    .Validate(
        settings =>
            settings.RefreshTokenExpirationDays > 0,
        "Refresh token expiration must be greater than zero.")
    .ValidateOnStart();


// ======================================================
// 7. External authentication settings
// ======================================================

builder.Services.Configure<
    GoogleAuthSettings>(
        builder.Configuration.GetSection(
            GoogleAuthSettings.SectionName));

builder.Services
    .AddOptions<FacebookAuthSettings>()
    .Bind(
        builder.Configuration.GetSection(
            FacebookAuthSettings.SectionName))
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.AppId),
        "Authentication:Facebook:AppId is required.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.AppSecret),
        "Authentication:Facebook:AppSecret is required.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.GraphApiVersion),
        "Authentication:Facebook:GraphApiVersion is required.")
    .ValidateOnStart();


// ======================================================
// 8. Cloudinary settings
// ======================================================

builder.Services
    .AddOptions<CloudinarySettings>()
    .Bind(
        builder.Configuration.GetSection(
            CloudinarySettings.SectionName))
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.CloudName),
        "Cloudinary:CloudName is required.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.ApiKey),
        "Cloudinary:ApiKey is required.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.ApiSecret),
        "Cloudinary:ApiSecret is required.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.BaseFolder),
        "Cloudinary:BaseFolder is required.")
    .ValidateOnStart();


// ======================================================
// 9. Initial admin settings
// ======================================================

builder.Services
    .AddOptions<InitialAdminSettings>()
    .Bind(
        builder.Configuration.GetSection(
            InitialAdminSettings.SectionName))
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.Email),
        "InitialAdmin:Email is required.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.FirstName),
        "InitialAdmin:FirstName is required.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.LastName),
        "InitialAdmin:LastName is required.")
    .ValidateOnStart();


// ======================================================
// 10. Booking lifecycle settings
// ======================================================

builder.Services
    .AddOptions<BookingLifecycleOptions>()
    .Bind(
        builder.Configuration.GetSection(
            BookingLifecycleOptions.SectionName))
    .Validate(
        options =>
            !options.Enabled
            ||
            options.ProcessingIntervalSeconds
                is >= 10 and <= 3600,
        "BookingLifecycle:ProcessingIntervalSeconds " +
        "must be between 10 and 3600 seconds when " +
        "the background service is enabled.")
    .ValidateOnStart();


// ======================================================
// 11. Stripe settings
// ======================================================

builder.Services
    .AddOptions<StripeSettings>()
    .Bind(
        builder.Configuration.GetSection(
            StripeSettings.SectionName))
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.SecretKey),
        "Stripe:SecretKey is required.")
    .Validate(
        settings =>
            settings.SecretKey.StartsWith(
                "sk_test_",
                StringComparison.Ordinal)
            ||
            settings.SecretKey.StartsWith(
                "sk_live_",
                StringComparison.Ordinal),
        "Stripe:SecretKey must be a valid Stripe secret key.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.PublishableKey),
        "Stripe:PublishableKey is required.")
    .Validate(
        settings =>
            settings.PublishableKey.StartsWith(
                "pk_test_",
                StringComparison.Ordinal)
            ||
            settings.PublishableKey.StartsWith(
                "pk_live_",
                StringComparison.Ordinal),
        "Stripe:PublishableKey must be a valid Stripe publishable key.")
    .Validate(
        settings =>
            !string.IsNullOrWhiteSpace(
                settings.WebhookSecret),
        "Stripe:WebhookSecret is required.")
    .Validate(
        settings =>
            settings.WebhookSecret.StartsWith(
                "whsec_",
                StringComparison.Ordinal),
        "Stripe:WebhookSecret must be a valid Stripe webhook secret.")
    .ValidateOnStart();


// ======================================================
// 12. JWT authentication
// ======================================================

var jwtSettings =
    builder.Configuration
        .GetSection(
            JwtSettings.SectionName)
        .Get<JwtSettings>()
    ??
    throw new InvalidOperationException(
        "JwtSettings section is missing.");

builder.Services
    .AddAuthentication(
        options =>
        {
            options.DefaultAuthenticateScheme =
                JwtBearerDefaults.AuthenticationScheme;

            options.DefaultChallengeScheme =
                JwtBearerDefaults.AuthenticationScheme;
        })
    .AddJwtBearer(
        options =>
        {
            options.RequireHttpsMetadata =
                true;

            options.EventsType =
                typeof(ActiveAccountJwtBearerEvents);

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer =
                        true,

                    ValidIssuer =
                        jwtSettings.Issuer,

                    ValidateAudience =
                        true,

                    ValidAudience =
                        jwtSettings.Audience,

                    ValidateIssuerSigningKey =
                        true,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                jwtSettings.Key)),

                    ValidateLifetime =
                        true,

                    ClockSkew =
                        TimeSpan.FromSeconds(
                            30),

                    NameClaimType =
                        ClaimTypes.NameIdentifier,

                    RoleClaimType =
                        ClaimTypes.Role
                };
        });

builder.Services.AddAuthorization();


// ======================================================
// 13. CORS
// ======================================================

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "AllowFrontEnd",
            policy =>
            {
                policy
                    .WithOrigins(
                        /*
                         * Angular development server.
                         */
                        "http://localhost:4200",
                        "https://localhost:4200",

                        /*
                         * Vite development server.
                         */
                        "http://localhost:5173",
                        "https://localhost:5173",
                        "http://127.0.0.1:5173",

                        /*
                         * VS Code Live Server.
                         */
                        "http://127.0.0.1:5500",
                        "http://localhost:5500")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
    });


// ======================================================
// 14. Swagger
// ======================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(
    options =>
    {
        options.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title =
                    "SmartStay API",

                Version =
                    "v1"
            });

        options.AddSecurityDefinition(
            "bearer",
            new OpenApiSecurityScheme
            {
                Type =
                    SecuritySchemeType.Http,

                Scheme =
                    "bearer",

                BearerFormat =
                    "JWT",

                Description =
                    "Enter the JWT access token."
            });

        options.AddSecurityRequirement(
            document =>
                new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecuritySchemeReference(
                            "bearer",
                            document)
                    ] = []
                });
    });


// ======================================================
// 15. Authentication and account services
// ======================================================

builder.Services.AddScoped<
    IEmailService,
    EmailService>();

builder.Services.AddScoped<
    IOtpService,
    OtpService>();

builder.Services.AddScoped<
    IJwtService,
    JwtService>();

builder.Services.AddScoped<
    IRefreshTokenService,
    RefreshTokenService>();

builder.Services
    .AddHttpClient<
        IExternalAuthService,
        ExternalAuthService>(
            client =>
            {
                client.Timeout =
                    TimeSpan.FromSeconds(
                        20);
            });

builder.Services.AddScoped<
    IAuthService,
    AuthService>();

builder.Services.AddScoped<
    IProfileService,
    ProfileService>();

builder.Services.AddScoped<
    IAccountService,
    AccountService>();

builder.Services.AddScoped<
    ActiveAccountJwtBearerEvents>();


// ======================================================
// Notification services
// ======================================================

builder.Services.AddScoped<
    INotificationService,
    NotificationService>();

builder.Services.AddScoped<
    INotificationPublisher,
    NotificationPublisher>();


// ======================================================
// 16. Image storage services
// ======================================================

builder.Services
    .AddHttpClient<
        IImageStorageService,
        CloudinaryImageStorageService>(
            client =>
            {
                client.Timeout =
                    TimeSpan.FromSeconds(
                        30);
            });


// ======================================================
// 17. Host, amenities, property, admin, and support services
// ======================================================

builder.Services.AddScoped<
    IHostApplicationService,
    HostApplicationService>();

builder.Services.AddScoped<
    IAdminHostApplicationService,
    AdminHostApplicationService>();

builder.Services.AddScoped<
    IHostPropertyService,
    HostPropertyService>();

builder.Services.AddScoped<
    IAdminPropertyService,
    AdminPropertyService>();

builder.Services.AddScoped<
    IHostPropertyManagementService,
    HostPropertyManagementService>();

builder.Services.AddScoped<
    IAmenityService,
    AmenityService>();

builder.Services.AddScoped<
    IAdminDashboardService,
    AdminDashboardService>();

builder.Services.AddScoped<
    IAdminUserService,
    AdminUserService>();

builder.Services.AddScoped<
    IAdminVerificationQueueService,
    AdminVerificationQueueService>();

builder.Services.AddScoped<
    ISupportTicketService,
    SupportTicketService>();

builder.Services.AddScoped<
    IBookingPayoutService,
    BookingPayoutService>();

builder.Services.AddScoped<
    IUserBookingRestrictionService,
    UserBookingRestrictionService>();

builder.Services.AddScoped<
    IAdminActionLogService,
    AdminActionLogService>();

builder.Services.AddScoped<
    IAdminFinancialService,
    AdminFinancialService>();


// ======================================================
// 18. Ratings and reviews services
// ======================================================

builder.Services.AddScoped<
    IPropertyRatingQueryService,
    PropertyRatingQueryService>();

builder.Services.AddScoped<
    IReviewService,
    ReviewService>();

builder.Services.AddScoped<
    HostReviewService>();

builder.Services.AddScoped<
    IHostReviewService,
    HostReviewNotificationDecorator>();

builder.Services.AddScoped<
    AdminReviewService>();

builder.Services.AddScoped<
    IAdminReviewService,
    AdminReviewNotificationDecorator>();


// ======================================================
// 19. Public property services with rating decorator
// ======================================================

/*
 * PublicPropertyService contains the original property
 * search and details logic.
 *
 * PublicPropertyRatingDecorator calls the original service
 * and adds AverageRating and ReviewsCount to its responses.
 */
builder.Services.AddScoped<
    PublicPropertyService>();

builder.Services.AddScoped<
    IPublicPropertyService,
    PublicPropertyRatingDecorator>();


// ======================================================
// 20. Wish list services with rating decorator
// ======================================================

/*
 * WishListService contains the original wish-list logic.
 *
 * WishListRatingDecorator calls the original service
 * and adds AverageRating and ReviewsCount to property items.
 */
builder.Services.AddScoped<
    WishListService>();

builder.Services.AddScoped<
    IWishListService,
    WishListRatingDecorator>();


// ======================================================
// 21. Booking services
// ======================================================

builder.Services.AddScoped<
    IBookingService,
    BookingService>();

builder.Services.AddScoped<
    IHostBookingService,
    HostBookingService>();

builder.Services.AddScoped<
    IAdminBookingService,
    AdminBookingService>();

builder.Services.AddScoped<
    IBookingLifecycleService,
    BookingLifecycleService>();

builder.Services.AddHostedService<
    BookingLifecycleBackgroundService>();


// ======================================================
// 22. Stripe payment services
// ======================================================

builder.Services.AddSingleton<
    IStripePaymentGateway,
    StripePaymentGateway>();

builder.Services.AddScoped<
    IPaymentRefundService,
    PaymentRefundService>();

builder.Services.AddScoped<
    IPaymentService,
    PaymentService>();

builder.Services.AddScoped<
    IStripeWebhookService,
    StripeWebhookService>();


// ======================================================
// 23. Seeders
// ======================================================

builder.Services.AddScoped<
    InitialAdminSeeder>();


// ======================================================
// Build application
// ======================================================

var app =
    builder.Build();


// ======================================================
// Seed initial administrator
// ======================================================

using (var scope =
       app.Services.CreateScope())
{
    var adminSeeder =
        scope.ServiceProvider
            .GetRequiredService<
                InitialAdminSeeder>();

    await adminSeeder.SeedAsync();
}


// ======================================================
// HTTP middleware pipeline
// ======================================================

app.UseExceptionHandler();

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "My API V1");

    options.RoutePrefix =
        string.Empty;
});

app.UseHttpsRedirection();

app.UseCors(
    "AllowFrontEnd");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();