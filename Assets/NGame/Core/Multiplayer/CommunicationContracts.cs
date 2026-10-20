using System;

namespace NGame.Core.Multiplayer
{
    [Serializable] public sealed class ChannelView
    {
        public string id;
        public string name;
        public bool canRead;
        public bool canSend;
        public bool isPrivate;
        public string reason;
    }
    [Serializable] public sealed class VoiceView
    {
        public string provider = "ngame-pcm16";
        public string matchId;
        public int policyEpoch;
        public int turn;
        public bool available;
        public bool canListen;
        public bool canTransmit;
        public bool microphoneActive;
        public string reason;
        public long closesAtUnixMs;
    }
    [Serializable] public sealed class CommunicationView
    {
        public string matchId;
        public int policyEpoch;
        public int turn;
        public long serverTimeUnixMs;
        public ChannelView[] channels = Array.Empty<ChannelView>();
        public VoiceView voice;
    }
    [Serializable] public sealed class ChatMessage
    {
        public string id;
        public string clientMessageId;
        public long sequence;
        public string matchId;
        public string channelId;
        public string authorPlayerId;
        public string authorName;
        public long sentAtUnixMs;
        public string text;
    }
    [Serializable] public sealed class ChatPage
    {
        public string channelId;
        public string matchId;
        public long cursor;
        public bool hasMore;
        public ChatMessage[] messages = Array.Empty<ChatMessage>();
    }
    [Serializable] public sealed class ChatSendRequest
    {
        public string channelId;
        public string matchId;
        public int turn;
        public int policyEpoch;
        public string clientMessageId;
        public string text;
    }
    [Serializable] public sealed class ChatSendResult
    {
        public bool ok;
        public string code;
        public string message;
        public ChatMessage sent;
        public CommunicationView communications;
    }
    [Serializable] public sealed class VoiceChunkRequest
    {
        public string matchId;
        public int turn;
        public int policyEpoch;
        public string chunkId;
        public string pcmBase64;
    }
    [Serializable] public sealed class VoicePacket
    {
        public long sequence;
        public string authorPlayerId;
        public long sentAtUnixMs;
        public string pcmBase64;
    }
    [Serializable] public sealed class VoicePollResult
    {
        public VoiceView voice;
        public long serverTimeUnixMs;
        public long cursor;
        public VoicePacket[] packets = Array.Empty<VoicePacket>();
    }
    [Serializable] public sealed class CommunicationSnapshot
    {
        public string matchId;
        public long sequence;
        public ChatMessage[] messages = Array.Empty<ChatMessage>();
    }
}
