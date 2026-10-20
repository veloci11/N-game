using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using NGame.Core.Multiplayer;
using NGame.Server;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://127.0.0.1:8787");
builder.WebHost.ConfigureKestrel(server => server.Limits.MaxRequestBodySize = ServerOptions.MaxBodyBytes);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Services.AddSingleton<ServerOptions>();
builder.Services.AddSingleton<RoomStore>();
builder.Services.AddHostedService<RoomTicker>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.IncludeFields = true;
    options.SerializerOptions.PropertyNameCaseInsensitive = false;
    options.SerializerOptions.MaxDepth = 16;
});
builder.Services.AddRateLimiter(options =>
{
    options.AddTokenBucketLimiter("room-entry", limit =>
    {
        limit.TokenLimit = 100;
        limit.TokensPerPeriod = 100;
        limit.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        limit.AutoReplenishment = true;
        limit.QueueLimit = 0;
    });
    options.OnRejected = async (context, cancellation) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new RoomCommandResult { ok = false,
            code = "rate_limited", message = "Trop de créations ou d’entrées dans les salles. Réessayez dans une minute." }, ApiSupport.Json, cancellation);
    };
});

var allowedOrigins = Environment.GetEnvironmentVariable("NGAME_ALLOWED_ORIGINS")?
    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0) policy.WithOrigins(allowedOrigins).WithMethods("GET", "POST").AllowAnyHeader();
}));

var app = builder.Build();
// Force option validation during startup, before accepting any room.
_ = app.Services.GetRequiredService<ServerOptions>();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers.CacheControl = "no-store, private";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.XContentTypeOptions = "nosniff";
    }
    try { await next(context); }
    catch (ApiFailure error)
    {
        if (error.Status == 401) context.Response.Headers.WWWAuthenticate = "Bearer";
        await ApiSupport.Failure(error.Status, error.Code, error.Message).ExecuteAsync(context);
    }
    catch (CommunicationFailure error)
    {
        await ApiSupport.Failure(error.Status, error.Code, error.Message).ExecuteAsync(context);
    }
    catch (BadHttpRequestException error)
    {
        var large = error.StatusCode == 413;
        await ApiSupport.Failure(large ? 413 : 400, large ? "body_too_large" : "invalid_request",
            large ? "La requête dépasse la limite de 16 Kio." : "La requête HTTP est invalide.").ExecuteAsync(context);
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
    catch (IOException) when (context.RequestAborted.IsCancellationRequested) { }
    catch (Exception error)
    {
        app.Logger.LogError("Une requête a échoué ({ErrorType}).", error.GetType().Name);
        await ApiSupport.Failure(500, "server_error", "Le serveur n’a pas pu traiter cette action. Réessayez.").ExecuteAsync(context);
    }
});
var webRoot = Environment.GetEnvironmentVariable("NGAME_WEB_ROOT");
if (!string.IsNullOrWhiteSpace(webRoot))
{
    webRoot = Path.GetFullPath(webRoot);
    if (!Directory.Exists(webRoot)) throw new InvalidOperationException("Le dossier NGAME_WEB_ROOT est introuvable.");
    var files = new PhysicalFileProvider(webRoot);
    var types = new FileExtensionContentTypeProvider();
    types.Mappings[".wasm"] = "application/wasm";
    types.Mappings[".data"] = "application/octet-stream";
    types.Mappings[".unityweb"] = "application/octet-stream";
    types.Mappings[".br"] = "application/octet-stream";
    types.Mappings[".gz"] = "application/octet-stream";
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = files,
        ContentTypeProvider = types,
        OnPrepareResponse = context =>
        {
            var path = context.File.Name;
            if (path.EndsWith(".br", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            {
                var extension = Path.GetExtension(path);
                context.Context.Response.Headers.ContentEncoding = extension == ".br" ? "br" : "gzip";
                if (types.TryGetContentType(path[..^extension.Length], out var type)) context.Context.Response.ContentType = type;
            }
            context.Context.Response.Headers.XContentTypeOptions = "nosniff";
        }
    });
}

app.UseRouting();
app.UseCors();
app.UseRateLimiter();

app.MapGet("/health", (RoomStore rooms) => ApiSupport.Reply(new { status = "ok", service = "NGame.Server", apiVersion = 2, rooms = rooms.Count }));
app.MapPost("/api/rooms", async (HttpContext context, RoomStore rooms) =>
    ApiSupport.Reply(rooms.Create(await ApiSupport.ReadBody<CreateRoomRequest>(context)), 201)).RequireRateLimiting("room-entry");
app.MapPost("/api/rooms/{code}/join", async (string code, HttpContext context, RoomStore rooms) =>
    ApiSupport.Reply(rooms.Join(code, await ApiSupport.ReadBody<JoinRoomRequest>(context)))).RequireRateLimiting("room-entry");
app.MapGet("/api/rooms/{code}/state", (string code, HttpContext context, RoomStore rooms) =>
    ApiSupport.Reply(rooms.State(code, ApiSupport.Bearer(context))));
app.MapPost("/api/rooms/{code}/commands", async (string code, HttpContext context, RoomStore rooms) =>
{
    var token = ApiSupport.Bearer(context);
    var command = await ApiSupport.ReadBody<RoomCommand>(context);
    var result = rooms.Command(code, token, command);
    return ApiSupport.Reply(result.Result, result.Status);
});
app.MapGet("/api/rooms/{code}/communications", (string code, HttpContext context, RoomStore rooms) =>
    ApiSupport.Reply(rooms.Communications(code, ApiSupport.Bearer(context))));
app.MapGet("/api/rooms/{code}/messages/{channel}", (string code, string channel, long? after, HttpContext context, RoomStore rooms) =>
    ApiSupport.Reply(rooms.Messages(code, ApiSupport.Bearer(context), channel, after ?? 0)));
app.MapPost("/api/rooms/{code}/messages", async (string code, HttpContext context, RoomStore rooms) =>
    ApiSupport.Reply(rooms.SendMessage(code, ApiSupport.Bearer(context), await ApiSupport.ReadBody<ChatSendRequest>(context))));
app.MapGet("/api/rooms/{code}/voice", (string code, string matchId, int turn, long? after, HttpContext context, RoomStore rooms) =>
    ApiSupport.Reply(rooms.Voice(code, ApiSupport.Bearer(context), matchId, turn, after ?? 0)));
app.MapPost("/api/rooms/{code}/voice", async (string code, HttpContext context, RoomStore rooms) =>
    ApiSupport.Reply(rooms.SendVoice(code, ApiSupport.Bearer(context), await ApiSupport.ReadBody<VoiceChunkRequest>(context))));
app.MapFallback((HttpContext context) => ApiSupport.Failure(404, "not_found", "Cette adresse est introuvable."));
app.Run();
