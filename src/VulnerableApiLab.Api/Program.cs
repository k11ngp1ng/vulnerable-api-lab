var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Private-note routes are deliberately closed until authentication and
// object-level authorization exist. Caller-provided identity is not trusted.
app.MapGet("/api/notes/{id:int}", DenyPrivateNotes);
app.MapPut("/api/notes/{id:int}", DenyPrivateNotes);
app.MapDelete("/api/notes/{id:int}", DenyPrivateNotes);
app.MapPost("/api/notes", DenyPrivateNotes);

app.Run();

static IResult DenyPrivateNotes() => Results.StatusCode(StatusCodes.Status403Forbidden);

public partial class Program;
