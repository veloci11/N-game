using System;
using NGame.Core.Multiplayer;

namespace NGame.Presentation.Game
{
    public interface IRoomClient
    {
        RoomView View { get; }
        string RoomCode { get; }
        bool HasSession { get; }
        bool IsBusy { get; }
        bool IsConnected { get; }
        bool IsTraining { get; }
        string StatusError { get; }
        event Action Changed;
        event Action SecretsMustHide;
        void CreateRoom(string displayName, int avatarIndex, RoomSettings settings);
        void JoinRoom(string code, string displayName, int avatarIndex);
        void Send(RoomCommand command);
        void Refresh();
        void SetActive(bool active);
        void ForgetRoom();
        void RetryPending();
    }
}
