namespace NGame.Server;

internal sealed class ServerOptions
{
    public const int MaxBodyBytes = 16 * 1024;
    public const int MaxRooms = 200;
    public const int MaxPlayers = 24;
    public const int MaxReceiptsPerSession = 128;
    public TimeSpan RoomIdleTtl { get; }
    public TimeSpan ConnectedGrace { get; }
    public string DataDirectory { get; }

    public ServerOptions()
    {
        var text = Environment.GetEnvironmentVariable("NGAME_ROOM_TTL_SECONDS");
        var seconds = 7200;
        if (text is not null && (!int.TryParse(text, out seconds) || seconds < 5 || seconds > 86400))
            throw new InvalidOperationException("NGAME_ROOM_TTL_SECONDS doit être compris entre 5 et 86400.");
        RoomIdleTtl = TimeSpan.FromSeconds(seconds);
        var graceText = Environment.GetEnvironmentVariable("NGAME_CONNECTED_GRACE_SECONDS");
        var grace = 15;
        if (graceText is not null && (!int.TryParse(graceText, out grace) || grace < 1 || grace > 120))
            throw new InvalidOperationException("NGAME_CONNECTED_GRACE_SECONDS doit être compris entre 1 et 120.");
        ConnectedGrace = TimeSpan.FromSeconds(grace);
        DataDirectory = Path.GetFullPath(Environment.GetEnvironmentVariable("NGAME_DATA_DIRECTORY")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NGame", "ServerState"));
        var webRoot = Environment.GetEnvironmentVariable("NGAME_WEB_ROOT");
        if (!string.IsNullOrWhiteSpace(webRoot))
        {
            var publicRoot = Path.GetFullPath(webRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var privateRoot = DataDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (privateRoot.StartsWith(publicRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("NGAME_DATA_DIRECTORY doit rester en dehors du dossier Web public.");
        }
    }
}
