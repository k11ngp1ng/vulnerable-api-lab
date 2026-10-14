using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using VulnerableApiLab.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<PasswordHasherOptions>(options => options.IterationCount = 600_000);
builder.Services.AddSingleton<IPasswordHasher<LabUser>, PasswordHasher<LabUser>>();
builder.Services.AddSingleton(provider => new LabAuth(
    Environment.GetEnvironmentVariable("LAB_DB_CONNECTION_STRING") ?? "",
    provider.GetRequiredService<IPasswordHasher<LabUser>>()));
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("local-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromSeconds(5),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return ValueTask.CompletedTask;
    };
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "LabAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });
var app = builder.Build();
// Development is used for the local exercises, but HTTP errors must not expose
// exception details (including database diagnostics) to a caller.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    SuppressDiagnosticsCallback = _ => true,
    ExceptionHandler = async context =>
    {
        context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("LabErrors")
            .LogWarning("A request failed with an internal error.");
        await Results.Problem(statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(context);
    }
});
app.UseAuthentication();
app.UseRouting();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Authentication is a local learning exercise. The launch profile binds to loopback.
if (app.Environment.IsDevelopment())
{
    var localAuth = app.MapGroup("/api/auth").AddEndpointFilter(async (invocation, next) =>
    {
        var remote = invocation.HttpContext.Connection.RemoteIpAddress;
        if (remote is not null && !IPAddress.IsLoopback(remote))
        {
            return Results.NotFound();
        }

        return await next(invocation);
    });

    localAuth.MapPost("/register", async (Credentials credentials, LabAuth auth, CancellationToken cancellationToken) =>
    {
        if (!LabAuth.TryNormalize(credentials, out var username))
        {
            return Results.BadRequest();
        }

        var user = await auth.RegisterAsync(username, credentials.Password!, cancellationToken);
        return user is null ? Results.Conflict() : Results.Created("/api/auth/me", user);
    });

    localAuth.MapPost("/login", async (Credentials credentials, LabAuth auth, HttpContext context, CancellationToken cancellationToken) =>
    {
        if (!LabAuth.TryNormalize(credentials, out var username))
        {
            return Results.BadRequest();
        }

        var user = await auth.AuthenticateAsync(username, credentials.Password!, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Name, user.Username)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Results.Ok(user);
    }).RequireRateLimiting("local-login");

    localAuth.MapGet("/me", (HttpContext context) =>
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new LabUser(
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            context.User.Identity.Name!));
    });

    // Only the verified cookie identity can select an owned note in this local exercise.
    app.MapGet("/api/notes/{id:int}", async (int id, HttpContext context, LabAuth auth, CancellationToken cancellationToken) =>
    {
        var remote = context.Connection.RemoteIpAddress;
        if (remote is not null && !IPAddress.IsLoopback(remote))
        {
            return Results.NotFound();
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (context.User.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var note = await auth.ReadOwnedNoteAsync(id, userId, cancellationToken);
        return note is null ? Results.NotFound() : Results.Ok(note);
    });
}

// Production has no local login or note reader. Write routes remain closed.
if (!app.Environment.IsDevelopment())
{
    app.MapGet("/api/notes/{id:int}", DenyPrivateNotes);
}
app.MapPut("/api/notes/{id:int}", DenyPrivateNotes);
app.MapDelete("/api/notes/{id:int}", DenyPrivateNotes);
app.MapPost("/api/notes", DenyPrivateNotes);

app.Run();

static IResult DenyPrivateNotes() => Results.StatusCode(StatusCodes.Status403Forbidden);

public partial class Program;
