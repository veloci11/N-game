using System;
using System.Collections.Generic;
using System.Linq;

namespace NGame.Core.Multiplayer
{
    /// <summary>Ephemeral native voice relay: PCM16 little-endian, 16 kHz, mono, 200 ms. Never captured in room snapshots.</summary>
    public sealed class RoomVoiceRelay
    {
        public const int SampleRate = 16000;
        public const int SamplesPerChunk = 3200;
        private readonly List<VoicePacket> packets = new List<VoicePacket>();
        private readonly Dictionary<string, Queue<long>> sendTimes = new Dictionary<string, Queue<long>>();
        private readonly Dictionary<string, string> receipts = new Dictionary<string, string>();
        private readonly Queue<string> receiptOrder = new Queue<string>();
        private string matchId;
        private int turn = -1;
        private long sequence;

        public VoicePollResult Poll(RoomView view, string expectedMatch, int expectedTurn, long after, long now)
        {
            var permissions = Prepare(view, now);
            RoomCommunications.RequireWindow(view, expectedMatch, expectedTurn, expectedTurn);
            if (!permissions.canListen) throw new CommunicationFailure(403, "voice_closed", permissions.reason);
            if (after < 0) throw new CommunicationFailure(400, "invalid_cursor", "Le repère audio est invalide.");
            return new VoicePollResult { voice = permissions, serverTimeUnixMs = now, cursor = sequence,
                packets = packets.Where(p => p.sequence > after && p.authorPlayerId != view.selfPlayerId)
                    .TakeLastCompatible(128).Select(p => new VoicePacket { sequence = p.sequence, authorPlayerId = p.authorPlayerId,
                        sentAtUnixMs = p.sentAtUnixMs, pcmBase64 = p.pcmBase64 }).ToArray() };
        }

        public VoicePollResult Send(RoomView view, VoiceChunkRequest request, long now)
        {
            var permissions = Prepare(view, now);
            if (request == null) throw new CommunicationFailure(400, "invalid_audio", "Fragment audio manquant.");
            RoomCommunications.RequireWindow(view, request.matchId, request.turn, request.policyEpoch);
            if (!permissions.canTransmit) throw new CommunicationFailure(403, "voice_closed", permissions.reason);
            if (string.IsNullOrEmpty(request.chunkId) || request.chunkId.Length > 80 || request.pcmBase64 == null || request.pcmBase64.Length > 8600)
                throw new CommunicationFailure(400, "invalid_audio", "Fragment audio invalide.");
            byte[] bytes;
            try { bytes = Convert.FromBase64String(request.pcmBase64); }
            catch (FormatException) { throw new CommunicationFailure(400, "invalid_audio", "Fragment audio invalide."); }
            if (bytes.Length != SamplesPerChunk * 2) throw new CommunicationFailure(400, "invalid_audio", "Le format audio doit être PCM16 mono à 16 kHz, par fragments de 200 ms.");
            var key = view.selfPlayerId + ":" + request.chunkId;
            if (receipts.TryGetValue(key, out var previous))
            {
                if (previous != request.pcmBase64) throw new CommunicationFailure(409, "audio_conflict", "Ce fragment a déjà un autre contenu.");
                return new VoicePollResult { voice = permissions, serverTimeUnixMs = now, cursor = sequence };
            }
            if (!sendTimes.TryGetValue(view.selfPlayerId, out var times)) sendTimes[view.selfPlayerId] = times = new Queue<long>();
            while (times.Count > 0 && now - times.Peek() >= 1000) times.Dequeue();
            if (times.Count >= 8) throw new CommunicationFailure(429, "voice_rate_limited", "Le débit audio est temporairement limité.");
            times.Enqueue(now);
            receipts[key] = request.pcmBase64; receiptOrder.Enqueue(key);
            while (receiptOrder.Count > 512) receipts.Remove(receiptOrder.Dequeue());
            packets.Add(new VoicePacket { sequence = ++sequence, authorPlayerId = view.selfPlayerId, sentAtUnixMs = now, pcmBase64 = request.pcmBase64 });
            while (packets.Count > 384) packets.RemoveAt(0);
            return new VoicePollResult { voice = permissions, serverTimeUnixMs = now, cursor = sequence };
        }

        public void Observe(RoomView view, long now) => Prepare(view, now);

        private VoiceView Prepare(RoomView view, long now)
        {
            var permissions = RoomCommunications.VoiceCapabilities(view);
            if (permissions.matchId != matchId || permissions.turn != turn || !permissions.available)
            {
                packets.Clear(); receipts.Clear(); receiptOrder.Clear(); sendTimes.Clear();
                sequence = 0; matchId = permissions.matchId; turn = permissions.turn;
            }
            packets.RemoveAll(p => now - p.sentAtUnixMs > 1800);
            return permissions;
        }
    }
}
