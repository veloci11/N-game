using System;

namespace NGame.Core.Multiplayer
{
    // Public fields intentionally match Unity JsonUtility and the HTTP JSON wire format.
    [Serializable] public sealed class RoleCount { public string roleId; public int count; }
    [Serializable] public sealed class RoomSettings
    {
        public string mode = "in-person";
        public string ruleset = "ngame-classic-v1";
        public int discussionSeconds = 90;
        public int nightSeconds = 25;
        public int voteSeconds = 90;
        public bool revealEliminatedRoles = true;
        public RoleCount[] composition = Array.Empty<RoleCount>();
    }
    [Serializable] public sealed class PlayerView
    {
        public string id;
        public string displayName;
        public int avatarIndex;
        public bool ready;
        public bool alive = true;
        public bool connected = true;
        public string revealedRoleId;
    }
    [Serializable] public sealed class ActionView
    {
        public string kind;
        public string title;
        public string description;
        public int targetCount;
        public string[] allowedTargetIds = Array.Empty<string>();
        public string[] choices = Array.Empty<string>();
        public bool submitted;
        public string submittedChoice;
        public string[] submittedTargetIds = Array.Empty<string>();
        public bool healingAvailable;
        public bool poisonAvailable;
        public string wolfVictimId;
        public string privateInfo;
    }
    [Serializable] public sealed class VoteRecordView
    {
        public string playerId;
        public string targetId;
        public bool abstained;
    }
    [Serializable] public sealed class VoteVerdictView
    {
        public string result;
        public string eliminatedPlayerId;
        public string[] tiedPlayerIds = Array.Empty<string>();
        public VoteRecordView[] votes = Array.Empty<VoteRecordView>();
    }
    [Serializable] public sealed class PrivateFact
    {
        public string key;
        public string playerId;
        public string roleId;
        public string description;
    }
    [Serializable] public sealed class PublicEvent
    {
        public int sequence;
        public string kind;
        public string text;
        public string playerId;
    }
    [Serializable] public sealed class RoomView
    {
        public string code;
        public string mode;
        public string ruleset;
        public string matchId;
        public RoomSettings settings;
        public long revision;
        public int turn;
        public int day;
        public string phase = "lobby";
        public string phaseTitle;
        public string nightStep;
        public long serverTimeUnixMs;
        public long phaseStartedUnixMs;
        public long deadlineUnixMs;
        public bool roleAcknowledged;
        public int acknowledgedCount;
        public int voteRound;
        public int votesReceived;
        public int votesExpected;
        public string[] voteTargetIds = Array.Empty<string>();
        public string selfVoteTargetId;
        public bool selfVoteAbstained;
        public VoteVerdictView verdict;
        public string hostPlayerId;
        public string selfPlayerId;
        public string selfRoleId;
        public string selfRoleName;
        public bool isHost;
        public bool canStart;
        public string startBlockedReason;
        public bool canAdvance;
        public string statusMessage;
        public string winner;
        public string[] winnerIds = Array.Empty<string>();
        public string[] wolfAllyIds = Array.Empty<string>();
        public PlayerView[] players = Array.Empty<PlayerView>();
        public RoleCount[] composition = Array.Empty<RoleCount>();
        public ActionView action;
        public PrivateFact[] privateFacts = Array.Empty<PrivateFact>();
        public PublicEvent[] events = Array.Empty<PublicEvent>();
    }
    [Serializable] public sealed class RoomCommand
    {
        public string kind;
        public string choice;
        public string[] targetIds = Array.Empty<string>();
        public bool ready;
        public RoomSettings settings;
        public int turn;
        public string commandId;
    }
    [Serializable] public sealed class RuleResult
    {
        public bool ok;
        public string code;
        public string message;
        public static RuleResult Success(string message = null) => new RuleResult { ok = true, message = message };
        public static RuleResult Fail(string code, string message) => new RuleResult { code = code, message = message };
    }
    [Serializable] public sealed class CreateRoomRequest
    {
        public string displayName;
        public int avatarIndex;
        public RoomSettings settings;
    }
    [Serializable] public sealed class JoinRoomRequest
    {
        public string displayName;
        public int avatarIndex;
        public string resumeToken;
    }
    [Serializable] public sealed class RoomAccess
    {
        public string roomCode;
        public string playerId;
        public string sessionToken;
        public RoomView view;
    }
    [Serializable] public sealed class RoomCommandResult
    {
        public bool ok;
        public string code;
        public string message;
        public RoomView view;
    }

    // Complete authoritative save. Contains secrets: persist server-side only, never send to a player.
    [Serializable] public sealed class RoomSnapshot
    {
        public int version = 1;
        public string code, hostId, phase, matchId, winner, lastGuardTarget, protectedTarget;
        public string wolfVictim, poisonVictim, afterReaction, afterVerdict;
        public RoomSettings settings;
        public int randomSeed, randomDrawCount, nightIndex, voteRound, turn, day, eventSequence;
        public long revision, phaseStarted, deadline, lastNow;
        public bool healingAvailable, poisonAvailable, healedTonight;
        public string[] winnerIds, runoffTargets, nightSteps, hunterReactions, reactedHunters, acknowledgedIds;
        public SnapshotPlayer[] players;
        public SnapshotValue[] roles, lovers;
        public SnapshotSubmission[] submissions;
        public SnapshotFacts[] facts;
        public SnapshotReceipt[] receipts;
        public PublicEvent[] events;
        public VoteVerdictView verdict;
    }
    [Serializable] public sealed class SnapshotPlayer
    {
        public string id, name;
        public int avatar;
        public bool ready, alive, connected, departed;
    }
    [Serializable] public sealed class SnapshotValue { public string key, value; }
    [Serializable] public sealed class SnapshotSubmission { public string playerId, choice; public string[] targets; }
    [Serializable] public sealed class SnapshotFacts { public string playerId; public PrivateFact[] values; }
    [Serializable] public sealed class SnapshotReceipt { public string key, fingerprint; public RuleResult result; }
}
