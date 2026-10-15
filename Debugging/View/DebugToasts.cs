// Author: František Holubec
// Created: 15.10.2026

using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    public enum DebugToastLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    // Short messages at the top of a host, over the menu or the app. Newest at the bottom.
    internal sealed class DebugToastStack : VisualElement
    {
        private const long DURATION_MS = 2500;
        private const long FADE_MS = 200;
        private const int MAX_TOASTS = 3;

        private static readonly string[] ICONS = {"info", "check_circle", "warning", "error"};
        private static readonly string[] CLASSES = {"dm-toast--info", "dm-toast--success", "dm-toast--warning", "dm-toast--error"};

        public DebugToastStack()
        {
            AddToClassList("dm-toasts");
            pickingMode = PickingMode.Ignore;
        }

        public void Show(string text, DebugToastLevel level)
        {
            var toast = new VisualElement {pickingMode = PickingMode.Ignore};
            toast.AddToClassList("dm-toast");
            toast.AddToClassList(CLASSES[(int) level]);
            toast.Add(DebugIcons.Create(ICONS[(int) level], "dm-toast__icon"));
            var label = new Label(text) {pickingMode = PickingMode.Ignore};
            label.AddToClassList("dm-toast__label");
            toast.Add(label);
            Add(toast);

            while (childCount > MAX_TOASTS)
            {
                RemoveAt(0);
            }

            // Class added a frame later, so the fade in transition runs
            toast.schedule.Execute(() => toast.AddToClassList("dm-toast--shown")).StartingIn(16);
            toast.schedule.Execute(() =>
            {
                toast.RemoveFromClassList("dm-toast--shown");
                toast.schedule.Execute(toast.RemoveFromHierarchy).StartingIn(FADE_MS);
            }).StartingIn(DURATION_MS);
        }
    }
}
