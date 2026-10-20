using System;
using System.Collections;
using System.Text;
using NGame.Core.Multiplayer;
using UnityEngine;
using UnityEngine.Networking;

namespace NGame.Presentation.Game
{
    /// <summary>HTTP adapter. Bearer tokens and private projections stay in memory and are never logged.</summary>
    public sealed partial class UnityRoomClient : MonoBehaviour, IRoomClient, IRoomCommunicationClient
    {
        private const string ServerPreference = "ngame.network.server";
        private string token;
        private string playerId;
        private bool active;
        private bool polling;
        private bool pollInFlight;
        private int generation;
        private int failedPolls;
        private float nextPoll;
        private RoomCommand pending;
        public RoomView View { get; private set; }
        public string RoomCode { get; private set; }
        public bool HasSession => !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(RoomCode);
        public bool IsBusy { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsTraining => false;
        public string StatusError { get; private set; }
        public string ServerUrl { get; private set; }
        public bool HasPendingCommand => pending != null;
        public event Action Changed;
        public event Action SecretsMustHide;

        private void Awake()
        {
            var fallback = "http://127.0.0.1:8787";
            if (Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out var origin)
                && (origin.Scheme == "http" || origin.Scheme == "https")) fallback = origin.GetLeftPart(UriPartial.Authority);
            ServerUrl = PlayerPrefs.GetString(ServerPreference, fallback).TrimEnd('/');
            var saved = RoomResumeStore.Read(ServerUrl);
            if (saved != null)
            {
                token = saved.token; playerId = saved.playerId; RoomCode = saved.roomCode;
                StatusError = "Ta place est sauvegardée. Reprends la connexion au village.";
            }
        }

        public bool ConfigureServer(string address)
        {
            if (!Uri.TryCreate(address?.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != "http" && uri.Scheme != "https") || !string.IsNullOrEmpty(uri.UserInfo)) return false;
            var next = uri.GetLeftPart(UriPartial.Authority) + uri.AbsolutePath.TrimEnd('/');
            if (next == ServerUrl) return true;
            ForgetRoom();
            ServerUrl = next;
            PlayerPrefs.SetString(ServerPreference, next);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        public void SetActive(bool enabled)
        {
            active = enabled;
            if (!enabled) { StopCommunications(); SecretsMustHide?.Invoke(); }
            else { nextPoll = 0; EnsurePollLoop(); EnsureCommunicationLoop(); }
        }

        public void CreateRoom(string displayName, int avatarIndex, RoomSettings settings)
        {
            if (IsBusy) return;
            var request = new CreateRoomRequest { displayName = displayName?.Trim(), avatarIndex = avatarIndex, settings = settings };
            StartAccess("/api/rooms", JsonUtility.ToJson(request));
        }

        public void JoinRoom(string code, string displayName, int avatarIndex)
        {
            if (IsBusy) return;
            var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized.Length == 0) { Report("Entre le code du village."); return; }
            var request = new JoinRoomRequest
            {
                displayName = displayName?.Trim(), avatarIndex = avatarIndex,
                resumeToken = normalized == RoomCode ? token : null
            };
            StartAccess("/api/rooms/" + Uri.EscapeDataString(normalized) + "/join", JsonUtility.ToJson(request));
        }

        private void StartAccess(string path, string json)
        {
            SecretsMustHide?.Invoke();
            IsBusy = true;
            StatusError = null;
            Changed?.Invoke();
            var requestGeneration = generation;
            StartCoroutine(Request("POST", path, json, false, requestGeneration, payload =>
            {
                var access = Parse<RoomAccess>(payload);
                IsBusy = false;
                if (access == null || string.IsNullOrEmpty(access.sessionToken) || access.view == null)
                { Fail("Le serveur a renvoyé une réponse incomplète."); return; }
                generation++;
                token = access.sessionToken;
                playerId = access.playerId;
                RoomCode = access.roomCode;
                pending = null;
                RoomResumeStore.Save(ServerUrl, RoomCode, playerId, token);
                Apply(access.view);
                nextPoll = 0;
                EnsurePollLoop();
            }, (message, status) => { IsBusy = false; Fail(message); }));
        }

        public void Send(RoomCommand command)
        {
            if (!HasSession || IsBusy || command == null) return;
            if (pending != null) { Report("Vérifie la dernière action avant d’en envoyer une autre."); return; }
            if (string.IsNullOrEmpty(command.commandId)) command.commandId = Guid.NewGuid().ToString("N");
            pending = command;
            SendPending();
        }

        private void SendPending()
        {
            if (pending == null || !HasSession || IsBusy) return;
            IsBusy = true;
            StatusError = null;
            Changed?.Invoke();
            var sent = pending;
            StartCoroutine(Request("POST", RoomPath + "/commands", SerializeCommand(sent), true, generation, payload =>
            {
                IsBusy = false;
                var result = Parse<RoomCommandResult>(payload);
                pending = null;
                if (sent.kind == "leave" && result != null && result.ok) { ForgetRoom(); return; }
                if (result?.view != null) Apply(result.view);
                if (result == null || !result.ok) { Report(result?.message ?? "L’action n’a pas été acceptée."); return; }
                StatusError = null;
                IsConnected = true;
                Changed?.Invoke();
                Refresh();
            }, (message, status) =>
            {
                IsBusy = false;
                // A timeout is ambiguous. Retrying reuses this exact commandId instead of applying an action twice.
                if (status > 0) pending = null;
                if (status > 0 && status != 401) Report(message);
                else Fail(message);
                if (status == 409) Refresh();
            }));
        }

        public void RetryPending()
        {
            if (pending == null) { Refresh(); return; }
            if (View != null && pending.turn != View.turn)
            {
                pending = null;
                Report("La partie a avancé. Consulte le tour actuel avant de choisir.");
                Refresh();
                return;
            }
            SendPending();
        }

        public void Refresh()
        {
            nextPoll = 0;
            EnsurePollLoop();
        }

        private string RoomPath => "/api/rooms/" + Uri.EscapeDataString(RoomCode ?? string.Empty);

        private void EnsurePollLoop()
        {
            if (polling || !active || !HasSession) return;
            polling = true;
            StartCoroutine(PollLoop());
        }

        private IEnumerator PollLoop()
        {
            while (active && HasSession)
            {
                if (!pollInFlight && Time.unscaledTime >= nextPoll)
                {
                    pollInFlight = true;
                    var current = generation;
                    yield return Request("GET", RoomPath + "/state", null, true, current, payload =>
                    {
                        var state = Parse<RoomView>(payload);
                        if (state == null) { Fail("L’état du village n’a pas pu être lu."); failedPolls++; return; }
                        failedPolls = 0;
                        Apply(state);
                    }, (message, status) => { failedPolls++; Fail(message); });
                    pollInFlight = false;
                    nextPoll = Time.unscaledTime + (failedPolls == 0 ? 1 : Mathf.Min(8, Mathf.Pow(2, failedPolls - 1)));
                }
                yield return null;
            }
            polling = false;
        }

        private IEnumerator Request(string method, string path, string body, bool authorized, int current,
            Action<string> success, Action<string, long> failure)
        {
            var requestBegan = Time.realtimeSinceStartup;
            using (var request = new UnityWebRequest(ServerUrl + path, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 8;
                request.SetRequestHeader("Accept", "application/json");
                if (body != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                if (authorized) request.SetRequestHeader("Authorization", "Bearer " + token);
                yield return request.SendWebRequest();
                if (current != generation) yield break;
                lastRequestDuration = Mathf.Max(0, Time.realtimeSinceStartup - requestBegan);
                if (request.result == UnityWebRequest.Result.Success)
                {
                    success(request.downloadHandler.text);
                    yield break;
                }
                var error = Parse<RoomCommandResult>(request.downloadHandler.text);
                if (error?.view != null) Apply(error.view);
                if (request.responseCode == 401 || request.responseCode == 404)
                {
                    SecretsMustHide?.Invoke();
                    StopCommunications(true);
                    RoomResumeStore.Clear();
                    token = null;
                    playerId = null;
                    View = null;
                    pending = null;
                }
                var message = request.responseCode == 0 ? "Connexion interrompue. Vérifie l’adresse du serveur et le réseau."
                    : error?.message ?? "Le serveur n’a pas pu répondre à cette demande.";
                failure(message, request.responseCode);
            }
        }

        private static T Parse<T>(string value) where T : class
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            try { return JsonUtility.FromJson<T>(value); }
            catch (ArgumentException) { return null; }
        }

        /// <summary>Only configure carries settings; Unity otherwise materializes a null inline object.</summary>
        public static string SerializeCommand(RoomCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (command.kind == "configure") return JsonUtility.ToJson(command);
            return JsonUtility.ToJson(new OrdinaryCommand
            {
                kind = command.kind, choice = command.choice, targetIds = command.targetIds,
                ready = command.ready, turn = command.turn, commandId = command.commandId
            });
        }

        [Serializable]
        private sealed class OrdinaryCommand
        {
            public string kind;
            public string choice;
            public string[] targetIds;
            public bool ready;
            public int turn;
            public string commandId;
        }

        private void Apply(RoomView value)
        {
            if (value == null || (View != null && value.revision < View.revision)) return;
            if (View == null || value.turn != View.turn || value.phase != View.phase || value.selfRoleId != View.selfRoleId)
            { StopVoice(); SecretsMustHide?.Invoke(); }
            View = value;
            IsConnected = true;
            EnsureCommunicationLoop();
            UpdateVoice();
            if (pending == null) StatusError = null;
            Changed?.Invoke();
        }

        private void Fail(string message)
        {
            SecretsMustHide?.Invoke();
            StopVoice();
            IsConnected = false;
            StatusError = message;
            Changed?.Invoke();
        }

        private void Report(string message)
        {
            SecretsMustHide?.Invoke();
            StatusError = message;
            Changed?.Invoke();
        }

        public void ForgetRoom()
        {
            generation++;
            SecretsMustHide?.Invoke();
            StopCommunications(true);
            RoomResumeStore.Clear();
            token = playerId = RoomCode = null;
            View = null;
            pending = null;
            IsBusy = false;
            IsConnected = false;
            StatusError = null;
            Changed?.Invoke();
        }

        private void OnApplicationPause(bool pause)
        {
            appPaused = pause;
            if (pause) { StopVoice(); SecretsMustHide?.Invoke(); }
            else { nextPoll = 0; EnsurePollLoop(); }
        }
        private void OnApplicationFocus(bool focused)
        {
            appFocused = focused;
            if (!focused) { StopVoice(); SecretsMustHide?.Invoke(); }
            else { nextPoll = 0; EnsurePollLoop(); }
        }
        private void OnDisable()
        {
            SecretsMustHide?.Invoke();
            StopCommunications();
            StopAllCoroutines();
            generation++;
            polling = pollInFlight = false;
            communicationPolling = false;
            IsChatBusy = false;
            IsBusy = false;
        }
        private void OnEnable() { if (active) { nextPoll = 0; EnsurePollLoop(); EnsureCommunicationLoop(); } }
    }
}
