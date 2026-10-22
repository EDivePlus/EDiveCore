// Author: František Holubec
// Created: 21.10.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.ServiceHub.Presence;

namespace EDIVE.ServiceHub.Streaming.Displays
{
    // Someone wants the display this user presents on. ServiceHub runs the countdown, no answer hands it over
    public class DisplayHandoverRequest
    {
        private const string WEB_PREFIX = "web-";

        private readonly UniTaskCompletionSource<bool> _answer = new();

        public string DisplayId { get; }
        public string DisplayName { get; }
        public string By { get; }
        public string ByName { get; }
        public bool ByWeb => By != null && By.StartsWith(WEB_PREFIX, StringComparison.Ordinal);
        public DateTime ExpiresAt { get; }
        public TimeSpan Remaining => ExpiresAt - DateTime.UtcNow;

        // Answered here, withdrawn or expired
        public event Action<DisplayHandoverRequest> Closed;

        // True keeps the display
        internal UniTask<bool> Answer => _answer.Task;

        internal DisplayHandoverRequest(PresenceCommand command, string displayName)
        {
            DisplayId = command.GetString("display_id");
            DisplayName = displayName;
            By = command.GetString("by");
            ByName = command.GetString("by_name") ?? By;
            ExpiresAt = command.ExpiresAt.ToUniversalTime();
        }

        public void Keep() => _answer.TrySetResult(true);

        public void HandOver() => _answer.TrySetResult(false);

        internal void Close()
        {
            _answer.TrySetResult(false);
            Closed?.Invoke(this);
            Closed = null;
        }
    }
}
