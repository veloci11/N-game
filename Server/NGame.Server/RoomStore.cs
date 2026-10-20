using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NGame.Core.Multiplayer;

namespace NGame.Server;

internal sealed class RoomStore
{
    private const string CodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private readonly ServerOptions options;
    private readonly ILogger<RoomStore> logger;
    private readonly ConcurrentDictionary<string, Entry> rooms = new(StringComparer.Ordinal);
    private readonly object createGate = new();
    private static readonly JsonSerializerOptions SnapshotJson = new() { IncludeFields = true, MaxDepth = 64 };

    public RoomStore(ServerOptions options, ILogger<RoomStore> logger)
    {
        this.options = options;
        this.logger = logger;
        Directory.CreateDirectory(options.DataDirectory);
        Protect(options.DataDirectory, true);
        Load();
    }

    private sealed class Entry(WerewolfRoom engine, long now)
    {
        public object Gate { get; } = new();
        public WerewolfRoom Engine { get; } = engine;
        public long LastActivity { get; set; } = now;
        public long LastSaved { get; set; }
        public long SavedRevision { get; set; } = -1;
        public Dictionary<string, Session> Sessions { get; } = new(StringComparer.Ordinal);
        public RoomCommunications Chat { get; set; } = new();
        public RoomVoiceRelay Voice { get; } = new();
    }

    private sealed class Session(string playerId)
    {
        public string PlayerId { get; } = playerId;
        public long LastSeen { get; set; } = Now;
        public bool Left { get; set; }
        public Dictionary<string, Receipt> Receipts { get; } = new(StringComparer.Ordinal);
        public Queue<string> ReceiptOrder { get; } = new();
    }

    private sealed record Receipt(string Fingerprint, bool Ok, string? Code, string? Message);
    private sealed class SavedRoom
    {
        public int Version { get; set; } = 1;
        public long LastActivity { get; set; }
        public RoomSnapshot Engine { get; set; } = null!;
        public CommunicationSnapshot Chat { get; set; } = null!;
        public SavedSession[] Sessions { get; set; } = [];
    }
    private sealed class SavedSession
    {
        public string TokenHash { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public long LastSeen { get; set; }
        public bool Left { get; set; }
        public KeyValuePair<string, Receipt>[] Receipts { get; set; } = [];
    }

    public int Count => rooms.Count;
    private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public RoomAccess Create(CreateRoomRequest request)
    {
        var name = ApiSupport.PlayerName(request.displayName);
        ApiSupport.Avatar(request.avatarIndex);
        ApiSupport.Settings(request.settings);
        lock (createGate)
        {
            if (rooms.Count >= ServerOptions.MaxRooms)
                throw new ApiFailure(503, "room_capacity", "Le serveur accueille déjà 200 salles. Réessayez plus tard.");
            var code = NewCode();
            while (rooms.ContainsKey(code)) code = NewCode();
            WerewolfRoom engine;
            try { engine = new WerewolfRoom(code, request.settings ?? new RoomSettings(), RandomNumberGenerator.GetInt32(int.MaxValue)); }
            catch (ArgumentException) { throw new ApiFailure(400, "invalid_settings", "Ces règles ou cette composition ne sont pas prises en charge."); }
            var entry = new Entry(engine, Now);
            var access = Add(entry, name, request.avatarIndex);
            if (!rooms.TryAdd(code, entry)) throw new ApiFailure(503, "room_capacity", "Impossible de créer la salle. Réessayez.");
            Save(entry, true);
            return access;
        }
    }

    public RoomAccess Join(string code, JoinRoomRequest request)
    {
        var entry = Find(code);
        lock (entry.Gate)
        {
            EnsureLive(entry);
            if (!string.IsNullOrEmpty(request.resumeToken))
            {
                var session = Authenticate(entry, request.resumeToken, true);
                if (!entry.Engine.ContainsPlayer(session.PlayerId))
                    throw new ApiFailure(401, "unauthorized", "Cette place a été quittée. Rejoins le salon avec une nouvelle identité.");
                session.Left = false;
                Touch(entry, session);
                var resumed = Access(entry, session.PlayerId, request.resumeToken);
                Save(entry, true);
                return resumed;
            }
            var name = ApiSupport.PlayerName(request.displayName);
            ApiSupport.Avatar(request.avatarIndex);
            if (entry.Sessions.Values.Count(session => entry.Engine.ContainsPlayer(session.PlayerId)) >= ServerOptions.MaxPlayers)
                throw new ApiFailure(409, "room_full", "Cette salle accueille déjà 24 joueurs.");
            var access = Add(entry, name, request.avatarIndex);
            entry.LastActivity = Now;
            Save(entry, true);
            return access;
        }
    }

    public RoomView State(string code, string token)
    {
        var entry = Find(code);
        lock (entry.Gate)
        {
            EnsureLive(entry);
            var session = Authenticate(entry, token);
            Touch(entry, session);
            var view = Project(entry, session.PlayerId);
            entry.Voice.Observe(view, Now);
            Save(entry);
            return view;
        }
    }

    public (int Status, RoomCommandResult Result) Command(string code, string token, RoomCommand command)
    {
        ApiSupport.Command(command);
        var entry = Find(code);
        lock (entry.Gate)
        {
            EnsureLive(entry);
            var session = Authenticate(entry, token, command.kind == "leave");
            if (!session.Left) Touch(entry, session);
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(command, ApiSupport.Json)));
            if (session.Receipts.TryGetValue(command.commandId, out var previous))
            {
                if (previous.Fingerprint != fingerprint)
                    return (409, new RoomCommandResult { code = "command_conflict", message = "Cet identifiant d’action a déjà été utilisé avec un autre contenu.", view = TryProject(entry, session)! });
                Save(entry, true);
                return Result(entry, session, previous);
            }
            var outcome = entry.Engine.Apply(session.PlayerId, command, Now);
            var receipt = new Receipt(fingerprint, outcome.ok, outcome.code, outcome.message);
            session.Receipts.Add(command.commandId, receipt);
            session.ReceiptOrder.Enqueue(command.commandId);
            while (session.ReceiptOrder.Count > ServerOptions.MaxReceiptsPerSession) session.Receipts.Remove(session.ReceiptOrder.Dequeue());
            if (command.kind == "leave" && outcome.ok) session.Left = true;
            // Expelled lobby members (and departed seats removed at rematch) lose their capabilities immediately.
            // Keep the actor's own leave receipt so a lost departure response can still be retried safely.
            foreach (var removed in entry.Sessions.Where(pair => !ReferenceEquals(pair.Value, session)
                && !entry.Engine.ContainsPlayer(pair.Value.PlayerId)).Select(pair => pair.Key).ToArray())
                entry.Sessions.Remove(removed);
            var result = Result(entry, session, receipt);
            Save(entry, true);
            return result;
        }
    }

    public CommunicationView Communications(string code, string token) => WithMember(code, token, (entry, view) => entry.Chat.Capabilities(view, Now));
    public ChatPage Messages(string code, string token, string channel, long after) => WithMember(code, token, (entry, view) => entry.Chat.Read(view, channel, after, Now));
    public ChatSendResult SendMessage(string code, string token, ChatSendRequest request) => WithMember(code, token, (entry, view) => entry.Chat.Send(view, request, Now), true);
    public VoicePollResult Voice(string code, string token, string matchId, int turn, long after) => WithMember(code, token, (entry, view) => entry.Voice.Poll(view, matchId, turn, after, Now));
    public VoicePollResult SendVoice(string code, string token, VoiceChunkRequest request) => WithMember(code, token, (entry, view) => entry.Voice.Send(view, request, Now));

    private T WithMember<T>(string code, string token, Func<Entry, RoomView, T> work, bool save = false)
    {
        var entry = Find(code);
        lock (entry.Gate)
        {
            EnsureLive(entry);
            var session = Authenticate(entry, token);
            Touch(entry, session);
            // Projection ticks the engine before every ACL decision, including audio and delayed requests.
            var view = Project(entry, session.PlayerId);
            entry.Voice.Observe(view, Now);
            var result = work(entry, view);
            Save(entry, save);
            return result;
        }
    }

    private (int, RoomCommandResult) Result(Entry entry, Session session, Receipt receipt) =>
        (receipt.Ok ? 200 : 409, new RoomCommandResult { ok = receipt.Ok, code = receipt.Code!, message = receipt.Message!, view = TryProject(entry, session)! });

    private RoomView? TryProject(Entry entry, Session session) => session.Left || !entry.Engine.ContainsPlayer(session.PlayerId) ? null : Project(entry, session.PlayerId);

    private static void Touch(Entry entry, Session session)
    {
        entry.LastActivity = session.LastSeen = Now;
        entry.Engine.SetConnected(session.PlayerId, true);
    }

    private RoomView Project(Entry entry, string playerId)
    {
        var now = Now;
        var view = entry.Engine.StateFor(playerId, now);
        var lastSeen = entry.Sessions.Values.ToDictionary(s => s.PlayerId, s => s);
        foreach (var player in view.players)
            player.connected = player.connected && lastSeen.TryGetValue(player.id, out var session) && !session.Left
                && now - session.LastSeen <= options.ConnectedGrace.TotalMilliseconds;
        return view;
    }

    public void Tick(ILogger tickLogger)
    {
        var now = Now;
        foreach (var pair in rooms)
        {
            var entry = pair.Value;
            lock (entry.Gate)
            {
                if (now - entry.LastActivity > options.RoomIdleTtl.TotalMilliseconds)
                {
                    rooms.TryRemove(pair.Key, out _);
                    DeleteSaved(pair.Key);
                    continue;
                }
                try
                {
                    foreach (var session in entry.Sessions.Values)
                        if (session.Left || now - session.LastSeen > options.ConnectedGrace.TotalMilliseconds)
                            entry.Engine.SetConnected(session.PlayerId, false);
                    entry.Engine.Tick(now);
                    var actor = entry.Sessions.Values.FirstOrDefault(s => entry.Engine.ContainsPlayer(s.PlayerId));
                    if (actor != null) entry.Voice.Observe(entry.Engine.StateFor(actor.PlayerId, now), now);
                    Save(entry);
                }
                catch (Exception error) { tickLogger.LogError("Le moteur ou la sauvegarde d’une salle a interrompu son tick ({ErrorType}).", error.GetType().Name); }
            }
        }
    }

    private Entry Find(string? rawCode)
    {
        var code = rawCode?.Trim().ToUpperInvariant();
        if (code is null || code.Length != 6 || !code.All(CodeAlphabet.Contains) || !rooms.TryGetValue(code, out var entry))
            throw new ApiFailure(404, "room_not_found", "Cette salle est introuvable ou a expiré. Vérifiez son code.");
        return entry;
    }
    private void EnsureLive(Entry entry)
    {
        if (!rooms.TryGetValue(entry.Engine.Code, out var current) || !ReferenceEquals(current, entry)
            || Now - entry.LastActivity > options.RoomIdleTtl.TotalMilliseconds)
            throw new ApiFailure(404, "room_not_found", "Cette salle est introuvable ou a expiré. Vérifiez son code.");
    }
    private static Session Authenticate(Entry entry, string? token, bool allowLeft = false)
    {
        if (!ApiSupport.ValidToken(token) || !entry.Sessions.TryGetValue(TokenHash(token!), out var session)
            || (!entry.Engine.ContainsPlayer(session.PlayerId) && !allowLeft) || (session.Left && !allowLeft))
            throw new ApiFailure(401, "unauthorized", "Votre session est absente ou invalide. Rejoignez la salle.");
        return session;
    }
    private RoomAccess Add(Entry entry, string name, int avatar)
    {
        foreach (var departed in entry.Sessions.Where(p => !entry.Engine.ContainsPlayer(p.Value.PlayerId)).Select(p => p.Key).ToArray())
            entry.Sessions.Remove(departed);
        var playerId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var result = entry.Engine.AddPlayer(playerId, name, avatar);
        if (!result.ok) throw new ApiFailure(409, result.code ?? "join_refused", result.message ?? "Impossible de rejoindre cette salle.");
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        entry.Sessions.Add(TokenHash(token), new Session(playerId));
        return Access(entry, playerId, token);
    }
    private RoomAccess Access(Entry entry, string id, string token) => new() { roomCode = entry.Engine.Code, playerId = id, sessionToken = token, view = Project(entry, id) };

    private void Save(Entry entry, bool force = false)
    {
        if (!force && entry.SavedRevision == entry.Engine.Revision && Now - entry.LastSaved < 5000) return;
        var saved = new SavedRoom { LastActivity = entry.LastActivity, Engine = entry.Engine.CaptureSnapshot(), Chat = entry.Chat.CaptureSnapshot(),
            Sessions = entry.Sessions.Select(pair => new SavedSession { TokenHash = pair.Key, PlayerId = pair.Value.PlayerId,
                LastSeen = pair.Value.LastSeen, Left = pair.Value.Left,
                Receipts = pair.Value.ReceiptOrder.Where(pair.Value.Receipts.ContainsKey).Select(id => new KeyValuePair<string, Receipt>(id, pair.Value.Receipts[id])).ToArray() }).ToArray() };
        var path = Path.Combine(options.DataDirectory, entry.Engine.Code + ".json");
        var temporary = path + ".tmp";
        using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            Protect(temporary, false);
            JsonSerializer.Serialize(file, saved, SnapshotJson);
            file.Flush(true);
        }
        File.Move(temporary, path, true);
        Protect(path, false);
        entry.LastSaved = Now;
        entry.SavedRevision = entry.Engine.Revision;
    }
    private void Load()
    {
        foreach (var path in Directory.EnumerateFiles(options.DataDirectory, "*.json").Take(ServerOptions.MaxRooms))
        {
            try
            {
                if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new InvalidDataException();
                var saved = JsonSerializer.Deserialize<SavedRoom>(File.ReadAllBytes(path), SnapshotJson);
                if (saved is null || saved.Version != 1 || saved.Engine is null) throw new InvalidDataException();
                if (Now - saved.LastActivity > options.RoomIdleTtl.TotalMilliseconds) { File.Delete(path); continue; }
                var engine = WerewolfRoom.RestoreSnapshot(saved.Engine);
                if (engine.Code.Length != 6 || !engine.Code.All(CodeAlphabet.Contains)) throw new InvalidDataException();
                var entry = new Entry(engine, saved.LastActivity) { Chat = RoomCommunications.RestoreSnapshot(saved.Chat), LastSaved = Now, SavedRevision = engine.Revision };
                foreach (var item in saved.Sessions.Take(ServerOptions.MaxPlayers))
                {
                    if (item.TokenHash.Length != 64 || !item.TokenHash.All(char.IsAsciiHexDigit)) throw new InvalidDataException();
                    var session = new Session(item.PlayerId) { LastSeen = item.LastSeen, Left = item.Left };
                    foreach (var pair in item.Receipts.TakeLast(ServerOptions.MaxReceiptsPerSession))
                    { session.Receipts[pair.Key] = pair.Value; session.ReceiptOrder.Enqueue(pair.Key); }
                    entry.Sessions.Add(item.TokenHash, session);
                    engine.SetConnected(item.PlayerId, false);
                }
                rooms.TryAdd(engine.Code, entry);
                Protect(path, false);
            }
            catch (Exception error) { logger.LogWarning("Une sauvegarde de salle est indisponible ({ErrorType}). Elle est conservée pour diagnostic.", error.GetType().Name); }
        }
    }
    private void DeleteSaved(string code)
    { var path = Path.Combine(options.DataDirectory, code + ".json"); if (File.Exists(path)) File.Delete(path); }
    private static void Protect(string path, bool directory)
    {
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, directory ? UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute : UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
    private static string TokenHash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string NewCode() => new(Enumerable.Range(0, 6).Select(_ => CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)]).ToArray());
}

internal sealed class RoomTicker(RoomStore rooms, ILogger<RoomTicker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        try { while (await timer.WaitForNextTickAsync(stoppingToken)) rooms.Tick(logger); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
