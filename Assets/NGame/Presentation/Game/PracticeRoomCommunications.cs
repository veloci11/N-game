using System;
using System.Linq;
using NGame.Core.Multiplayer;

namespace NGame.Presentation.Game
{
    public sealed partial class PracticeRoomClient
    {
        private RoomCommunications practiceChat = new RoomCommunications();
        private string practiceChannel;
        private int conversationTurn = -1;
        private int conversationLine;
        private float replyAfter = -1;
        private const string BotLabel = "[Bot · entraînement] ";
        private static readonly string[] VoteExamples =
        {
            "Un vote indique un choix, pas une preuve de rôle. Je vais comparer les décisions publiques.",
            "Un villageois peut se tromper. Tu peux voter contre un suspect ou t’abstenir si tu hésites.",
            "Nous sommes des bots : ces échanges illustrent une discussion. En vraie soirée, confrontez vos arguments avant de voter."
        };
        public CommunicationView Communications { get; private set; }
        public ChatMessage[] Messages { get; private set; } = Array.Empty<ChatMessage>();
        public bool IsChatBusy => false;
        public string ChatError { get; private set; }
        public VoiceView Voice => Communications?.voice;
        public event Action CommunicationChanged;

        public void OpenChannel(string channelId)
        {
            practiceChannel = channelId;
            if (View != null) UpdatePracticeCommunications(View);
        }
        public void CloseChannel()
        { practiceChannel = null; Messages = Array.Empty<ChatMessage>(); ChatError = null; CommunicationChanged?.Invoke(); }
        public void SendChat(string channelId, string text)
        {
            if (room == null) return;
            try
            {
                var view = room.StateFor(HumanId, Now);
                var permissions = practiceChat.Capabilities(view, Now);
                practiceChat.Send(view, new ChatSendRequest { channelId = channelId, text = text,
                    matchId = permissions.matchId, turn = permissions.turn, policyEpoch = permissions.policyEpoch,
                    clientMessageId = Guid.NewGuid().ToString("N") }, Now);
                ChatError = null;
                // One delayed, clearly simulated response. No role, target or hidden fact is inferred from text.
                if (channelId == "village" && view.phase == "vote")
                    replyAfter = practiceElapsed + 1.6f;
                UpdatePracticeCommunications(view);
            }
            catch (CommunicationFailure error) { ChatError = error.Message; CommunicationChanged?.Invoke(); }
        }
        public void SetMicrophone(bool enabled)
        {
            if (Communications?.voice != null)
            {
                Communications.voice.microphoneActive = false;
                Communications.voice.reason = "L’entraînement est individuel. Le vocal sert aux votes des parties distantes.";
            }
            CommunicationChanged?.Invoke();
        }
        private void UpdatePracticeCommunications(RoomView view)
        {
            var previous = Communications?.matchId;
            Communications = practiceChat.Capabilities(view, Now);
            if (previous != Communications.matchId) Messages = Array.Empty<ChatMessage>();
            if (practiceChannel != null)
            {
                var allowed = Communications.channels.FirstOrDefault(c => c.id == practiceChannel && c.canRead);
                if (allowed == null) { CloseChannel(); SecretsMustHide?.Invoke(); }
                else Messages = practiceChat.Read(view, practiceChannel, 0, Now).messages;
            }
            CommunicationChanged?.Invoke();
        }
        private void ResetPracticeConversation()
        { conversationTurn = -1; conversationLine = 0; replyAfter = -1; }

        private void UpdatePracticeConversation(RoomView self)
        {
            if (conversationTurn != self.turn)
            {
                conversationTurn = self.turn;
                conversationLine = 0;
                replyAfter = -1;
            }
            if (self.phase != "vote") return;
            string text = null;
            if (conversationLine < VoteExamples.Length
                && practiceElapsed - turnBegan >= 1.2f + conversationLine * 3.5f)
            { text = VoteExamples[conversationLine]; conversationLine++; }
            else if (replyAfter >= 0 && practiceElapsed >= replyAfter)
            { text = "Message reçu. Ceci est une réponse d’entraînement générique ; elle ne confirme ni ton soupçon ni un rôle."; replyAfter = -1; }
            if (text == null) return;
            // Authoritative channel checks also apply to bots. Eliminated bots cannot speak to the Village.
            var speaker = bots.Where(id => self.players.Any(p => p.id == id && p.alive)).ToArray();
            if (speaker.Length == 0) return;
            var bot = room.StateFor(speaker[(Math.Max(1, conversationLine) - 1) % speaker.Length], Now);
            var access = practiceChat.Capabilities(bot, Now);
            if (!access.channels.Any(c => c.id == "village" && c.canSend)) return;
            try
            {
                practiceChat.Send(bot, new ChatSendRequest
                {
                    channelId = "village", text = BotLabel + text, matchId = access.matchId,
                    turn = access.turn, policyEpoch = access.policyEpoch,
                    clientMessageId = Guid.NewGuid().ToString("N")
                }, Now);
                UpdatePracticeCommunications(self);
            }
            catch (CommunicationFailure) { /* A phase change must fail closed, never bypass channel permissions. */ }
        }

        private void ClearPracticeCommunications()
        {
            practiceChat = new RoomCommunications(); practiceChannel = null;
            ResetPracticeConversation();
            Communications = null; Messages = Array.Empty<ChatMessage>(); ChatError = null;
            CommunicationChanged?.Invoke();
        }
    }
}
