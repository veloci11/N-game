using System;
using System.Collections.Generic;
using System.Linq;

namespace NGame.Core.Multiplayer
{
    public sealed class CommunicationFailure : Exception
    {
        public int Status { get; }
        public string Code { get; }
        public CommunicationFailure(int status, string code, string message) : base(message)
        { Status = status; Code = code; }
    }

    /// <summary>Call under the room lock with a fresh, recipient-only projection. No client supplies identity or role.</summary>
    public sealed class RoomCommunications
    {
        private const int HistoryLimit = 200;
        private readonly List<ChatMessage> messages = new List<ChatMessage>();
        private readonly Dictionary<string, Queue<long>> sendTimes = new Dictionary<string, Queue<long>>();
        private string matchId;
        private long sequence;

        public CommunicationView Capabilities(RoomView view, long now)
        {
            EnsureMatch(view);
            var self = Self(view);
            // Text conversations exist only during the public voting window.
            // Dead players follow the living discussion in read-only mode and speak in their own room.
            var channels = new List<ChannelView>();
            if (view.phase == "vote")
            {
                channels.Add(new ChannelView { id = "village", name = "Village", canRead = true, canSend = self.alive,
                    reason = self.alive ? null : "Tu peux suivre le Village. Écris dans Observateurs pour parler aux éliminés." });
                if (!self.alive)
                    channels.Add(new ChannelView { id = "observateurs", name = "Observateurs", isPrivate = true,
                        canRead = true, canSend = true });
            }
            return new CommunicationView { matchId = matchId, policyEpoch = view.turn, turn = view.turn,
                serverTimeUnixMs = now, channels = channels.ToArray(), voice = VoiceCapabilities(view) };
        }

        public static VoiceView VoiceCapabilities(RoomView view)
        {
            var self = Self(view);
            var available = view.mode == "remote" && view.phase == "vote";
            return new VoiceView
            {
                matchId = Identity(view), turn = view.turn, policyEpoch = view.turn,
                available = available, canListen = available, canTransmit = available && self.alive,
                closesAtUnixMs = available ? view.deadlineUnixMs : 0,
                reason = available ? self.alive ? "Micro coupé. Active-le si tu souhaites parler." : "Écoute seule : tu es éliminé."
                    : view.mode == "remote" ? "Le vocal s’ouvre uniquement pendant le vote de jour." : "En présence, discutez dans la pièce."
            };
        }

        public ChatPage Read(RoomView view, string channelId, long after, long now)
        {
            var capabilities = Capabilities(view, now);
            var channel = capabilities.channels.FirstOrDefault(c => c.id == channelId && c.canRead);
            if (channel == null) throw new CommunicationFailure(403, "channel_forbidden", "Cette conversation n’est pas accessible à ce moment.");
            if (after < 0) throw new CommunicationFailure(400, "invalid_cursor", "Le repère de conversation est invalide.");
            var allowed = messages.Where(m => m.channelId == channelId && m.sequence > after).ToArray();
            // On opening, show the most recent page; later polls follow the cursor.
            var page = after == 0 ? allowed.Skip(Math.Max(0, allowed.Length - 50)).ToArray() : allowed.Take(50).ToArray();
            return new ChatPage { channelId = channelId, matchId = matchId, messages = page.Select(Copy).ToArray(),
                cursor = page.Length == 0 ? after : page[page.Length - 1].sequence, hasMore = after > 0 && allowed.Length > page.Length };
        }

        public ChatSendResult Send(RoomView view, ChatSendRequest request, long now)
        {
            var capabilities = Capabilities(view, now);
            if (request == null || !ValidId(request.clientMessageId))
                throw new CommunicationFailure(400, "invalid_message", "Le message doit avoir un identifiant valide.");
            if (request.matchId != capabilities.matchId || request.turn > view.turn || request.policyEpoch != request.turn)
                throw new CommunicationFailure(409, "stale_communication", "La partie a avancé. Rouvre la conversation du tour actuel.");
            var channel = capabilities.channels.FirstOrDefault(c => c.id == request.channelId);
            var text = request.text == null ? string.Empty : request.text.Trim();
            if (text.Length == 0 || text.Length > 500 || text.Any(c => char.IsControl(c) && c != '\n' && c != '\t'))
                throw new CommunicationFailure(400, "invalid_message", "Écris un message de 1 à 500 caractères.");
            var previous = messages.FirstOrDefault(m => m.authorPlayerId == view.selfPlayerId && m.clientMessageId == request.clientMessageId);
            if (previous != null)
            {
                if (previous.channelId != request.channelId || previous.text != text)
                    throw new CommunicationFailure(409, "message_conflict", "Cet identifiant désigne déjà un autre message.");
                // Acknowledge the sender's already accepted intent without redisclosing its content
                // after the vote closed or the sender lost access (for example on elimination).
                return new ChatSendResult { ok = true, code = "already_accepted",
                    sent = channel != null && channel.canRead ? Copy(previous) : null,
                    communications = capabilities };
            }
            // A new stale intention never sends, even when the same channel opens in a later vote.
            RequireWindow(view, request.matchId, request.turn, request.policyEpoch);
            if (channel == null || !channel.canSend)
                throw new CommunicationFailure(403, "channel_forbidden", "Tu ne peux pas écrire dans cette conversation maintenant.");
            if (!sendTimes.TryGetValue(view.selfPlayerId, out var times)) sendTimes[view.selfPlayerId] = times = new Queue<long>();
            while (times.Count > 0 && now - times.Peek() >= 60000) times.Dequeue();
            if (times.Count >= 30 || times.Count(t => now - t < 2000) >= 3)
                throw new CommunicationFailure(429, "chat_rate_limited", "Tu écris très vite. Attends quelques secondes avant de réessayer.");
            times.Enqueue(now);
            var author = Self(view);
            var message = new ChatMessage { id = Guid.NewGuid().ToString("N"), clientMessageId = request.clientMessageId,
                sequence = ++sequence, matchId = matchId, channelId = channel.id, authorPlayerId = author.id,
                authorName = author.displayName, sentAtUnixMs = now, text = text };
            messages.Add(message);
            while (messages.Count(m => m.channelId == channel.id) > HistoryLimit)
                messages.Remove(messages.First(m => m.channelId == channel.id));
            return new ChatSendResult { ok = true, sent = Copy(message), communications = capabilities };
        }

        public CommunicationSnapshot CaptureSnapshot() => new CommunicationSnapshot
        { matchId = matchId, sequence = sequence, messages = messages.Select(Copy).ToArray() };

        public static RoomCommunications RestoreSnapshot(CommunicationSnapshot snapshot)
        {
            var result = new RoomCommunications();
            if (snapshot == null) return result;
            result.matchId = snapshot.matchId;
            result.sequence = snapshot.sequence;
            foreach (var item in (snapshot.messages ?? Array.Empty<ChatMessage>()).TakeLastCompatible(HistoryLimit * 3))
                result.messages.Add(Copy(item));
            return result;
        }

        public static void RequireWindow(RoomView view, string expectedMatch, int expectedTurn, int expectedEpoch)
        {
            if (expectedMatch != Identity(view) || expectedTurn != view.turn || expectedEpoch != view.turn)
                throw new CommunicationFailure(409, "stale_communication", "La partie a avancé. Rouvre la conversation du tour actuel.");
        }

        private void EnsureMatch(RoomView view)
        {
            var next = Identity(view);
            if (next == matchId) return;
            matchId = next;
            messages.Clear(); sendTimes.Clear(); sequence = 0;
        }

        private static string Identity(RoomView view) => string.IsNullOrEmpty(view.matchId) ? "lobby:" + view.code : view.matchId;
        private static PlayerView Self(RoomView view)
        {
            var self = view == null ? null : view.players.FirstOrDefault(p => p.id == view.selfPlayerId);
            if (self == null) throw new CommunicationFailure(403, "not_member", "Tu n’appartiens pas à cette partie.");
            return self;
        }
        private static bool ValidId(string id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 80
            && id.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.');
        private static ChatMessage Copy(ChatMessage value) => new ChatMessage { id = value.id, clientMessageId = value.clientMessageId,
            sequence = value.sequence, matchId = value.matchId, channelId = value.channelId, authorPlayerId = value.authorPlayerId,
            authorName = value.authorName, sentAtUnixMs = value.sentAtUnixMs, text = value.text };
    }

    internal static class CommunicationEnumerable
    {
        public static IEnumerable<T> TakeLastCompatible<T>(this IEnumerable<T> source, int count)
        { var array = source.ToArray(); return array.Skip(Math.Max(0, array.Length - count)); }
    }
}
