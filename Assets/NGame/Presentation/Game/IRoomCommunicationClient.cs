using System;
using NGame.Core.Multiplayer;

namespace NGame.Presentation.Game
{
    public interface IRoomCommunicationClient
    {
        CommunicationView Communications { get; }
        ChatMessage[] Messages { get; }
        bool IsChatBusy { get; }
        string ChatError { get; }
        VoiceView Voice { get; }
        event Action CommunicationChanged;
        void OpenChannel(string channelId);
        void CloseChannel();
        void SendChat(string channelId, string text);
        void SetMicrophone(bool enabled);
    }
}
