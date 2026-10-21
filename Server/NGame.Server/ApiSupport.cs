using System.Text.Json;
using System.Text.Json.Serialization;
using NGame.Core.Multiplayer;

namespace NGame.Server;

internal sealed class ApiFailure(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}

internal static class ApiSupport
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = false,
        MaxDepth = 16,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static IResult Reply(object value, int status = StatusCodes.Status200OK) =>
        Results.Json(value, Json, statusCode: status);

    public static IResult Failure(int status, string code, string message, RoomView? view = null) =>
        Reply(new RoomCommandResult { ok = false, code = code, message = message, view = view! }, status);

    public static async Task<T> ReadBody<T>(HttpContext context) where T : class
    {
        if (!context.Request.HasJsonContentType())
            throw new ApiFailure(415, "json_required", "Envoyez la requête au format JSON (application/json).");
        if (context.Request.ContentLength > ServerOptions.MaxBodyBytes)
            throw new ApiFailure(413, "body_too_large", "La requête dépasse la limite de 16 Kio.");

        using var body = new MemoryStream();
        var buffer = new byte[4096];
        while (true)
        {
            var read = await context.Request.Body.ReadAsync(buffer, context.RequestAborted);
            if (read == 0) break;
            if (body.Length + read > ServerOptions.MaxBodyBytes)
                throw new ApiFailure(413, "body_too_large", "La requête dépasse la limite de 16 Kio.");
            await body.WriteAsync(buffer.AsMemory(0, read), context.RequestAborted);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body.ToArray(), Json)
                ?? throw new ApiFailure(400, "invalid_json", "Le contenu JSON est vide ou invalide.");
        }
        catch (JsonException)
        {
            throw new ApiFailure(400, "invalid_json", "Le contenu JSON ne correspond pas aux champs attendus.");
        }
    }

    public static string Bearer(HttpContext context)
    {
        var values = context.Request.Headers.Authorization;
        if (values.Count != 1)
            throw new ApiFailure(401, "unauthorized", "Votre session est absente ou invalide. Rejoignez la salle.");
        var value = values[0];
        if (value is null || !value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            throw new ApiFailure(401, "unauthorized", "Votre session est absente ou invalide. Rejoignez la salle.");
        var token = value[7..];
        if (!ValidToken(token))
            throw new ApiFailure(401, "unauthorized", "Votre session est absente ou invalide. Rejoignez la salle.");
        return token;
    }

    public static bool ValidToken(string? token) => token is { Length: 43 }
        && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    public static string PlayerName(string? value)
    {
        value = value?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 24 || value.Any(char.IsControl))
            throw new ApiFailure(400, "invalid_name", "Choisissez un pseudo de 1 à 24 caractères, sans caractère de contrôle.");
        return value;
    }

    public static void Avatar(int index)
    {
        if (index is < 0 or > 5)
            throw new ApiFailure(400, "invalid_avatar", "Choisissez l’un des six avatars disponibles.");
    }

    public static void Settings(RoomSettings? settings)
    {
        if (settings is null) return;
        if (settings.mode is null || settings.mode.Length > 24 || settings.ruleset is null || settings.ruleset.Length > 48
            || settings.nightProtocol?.Length > 24)
            throw new ApiFailure(400, "invalid_settings", "Le mode ou les règles de la salle sont invalides.");
        if (settings.discussionSeconds is < 1 or > 3600 || settings.nightSeconds is < 1 or > 3600 || settings.voteSeconds is < 30 or > 300)
            throw new ApiFailure(400, "invalid_settings", "Les durées doivent être comprises entre 1 et 3600 secondes.");
        if (settings.composition is null || settings.composition.Length > 16
            || settings.composition.Any(x => x is null || string.IsNullOrWhiteSpace(x.roleId)
                || x.roleId.Length > 40 || x.count is < 0 or > ServerOptions.MaxPlayers)
            || settings.composition.Sum(x => x.count) > ServerOptions.MaxPlayers)
            throw new ApiFailure(400, "invalid_composition", "La composition est invalide ou dépasse 24 joueurs.");
    }

    public static void Command(RoomCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.commandId) || command.commandId.Length > 80
            || command.commandId.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')))
            throw new ApiFailure(400, "invalid_command_id", "L’action doit avoir un identifiant unique de 1 à 80 caractères.");
        if (string.IsNullOrWhiteSpace(command.kind) || command.kind.Length > 32 || command.turn < 0
            || command.choice?.Length > 64 || command.targetIds is null
            || command.targetIds.Length > ServerOptions.MaxPlayers
            || command.targetIds.Any(id => id is null || id.Length != 32 || !id.All(char.IsAsciiHexDigit)))
            throw new ApiFailure(400, "invalid_command", "L’action ou ses cibles sont invalides.");
        Settings(command.settings);
    }
}
