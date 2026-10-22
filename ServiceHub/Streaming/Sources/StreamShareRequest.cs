// Author: František Holubec
// Created: 20.10.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.ServiceHub.Presence;

namespace EDIVE.ServiceHub.Streaming.Sources
{
    // Someone asks to watch this user's view. ServiceHub runs the countdown, no answer is Deny
    public class StreamShareRequest
    {
        private readonly UniTaskCompletionSource<bool> _answer = new();

        public string From { get; }
        public string FromName { get; }
        public bool FromWeb { get; }
        public DateTime ExpiresAt { get; }
        public TimeSpan Remaining => ExpiresAt - DateTime.UtcNow;
        public bool IsAnswered => _answer.Task.Status != UniTaskStatus.Pending;

        // Chosen in the dialog, menus on, sound off by default
        public bool IncludeMenus { get; private set; } = true;
        public bool IncludeHearing { get; private set; }
        public bool IncludeMic { get; private set; }

        // Answered here, withdrawn or expired
        public event Action<StreamShareRequest> Closed;

        internal UniTask<bool> Answer => _answer.Task;

        internal StreamShareRequest(PresenceCommand command)
        {
            From = command.GetString("from");
            FromName = command.GetString("from_name") ?? From;
            FromWeb = command.GetString("from_kind") != "app";
            ExpiresAt = command.ExpiresAt.ToUniversalTime();
        }

        public void Allow(bool includeMenus = true, bool includeHearing = false, bool includeMic = false)
        {
            IncludeMenus = includeMenus;
            IncludeHearing = includeHearing;
            IncludeMic = includeMic;
            _answer.TrySetResult(true);
        }

        public void Deny() => _answer.TrySetResult(false);

        internal void Close()
        {
            _answer.TrySetResult(false);
            Closed?.Invoke(this);
            Closed = null;
        }
    }
}
