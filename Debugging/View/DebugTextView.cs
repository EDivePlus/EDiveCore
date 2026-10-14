// Author: František Holubec
// Created: 15.10.2026

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Copy and native share of text. Share needs yasirkula NativeShare and a mobile platform.
    public static class DebugShare
    {
#if NATIVE_SHARE && (UNITY_ANDROID || UNITY_IOS)
        public static bool IsAvailable => true;
#else
        public static bool IsAvailable => false;
#endif

        public static void Copy(string text) => GUIUtility.systemCopyBuffer = text ?? string.Empty;

        public static void Share(string subject, string text)
        {
#if NATIVE_SHARE && (UNITY_ANDROID || UNITY_IOS)
            new NativeShare().SetSubject(subject).SetTitle(subject).SetText(text).Share();
#else
            Debug.LogWarning("DebugMenu: native share is only available on Android and iOS with NativeShare installed.");
#endif
        }
    }

    // Scrollable selectable text with right alignment and wrapping switches.
    public sealed class DebugTextView : VisualElement
    {
        private readonly ScrollView _scroll;
        private readonly Label _label;
        private bool _alignRight;
        private bool _wrap;

        public string Text
        {
            get => _label.text;
            set => _label.text = value ?? string.Empty;
        }

        // Right alignment keeps the line ends visible
        public bool AlignRight
        {
            get => _alignRight;
            set
            {
                _alignRight = value;
                EnableInClassList("dm-text--right", value);
                schedule.Execute(ScrollToLineEnds);
            }
        }

        public bool Wrap
        {
            get => _wrap;
            set
            {
                _wrap = value;
                EnableInClassList("dm-text--wrap", value);
                _scroll.mode = value ? ScrollViewMode.Vertical : ScrollViewMode.VerticalAndHorizontal;
                schedule.Execute(ScrollToLineEnds);
            }
        }

        public DebugTextView()
        {
            AddToClassList("dm-text");
            _scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            _scroll.AddToClassList("dm-text__scroll");
            _label = new Label();
            _label.AddToClassList("dm-text__label");
            _label.selection.isSelectable = true;
            _label.RegisterCallback<GeometryChangedEvent>(_ => ScrollToLineEnds());
            _scroll.Add(_label);
            Add(_scroll);
            Wrap = false;
        }

        // Unwrapped right aligned lines start off screen, scroll to their ends
        private void ScrollToLineEnds()
        {
            var x = _alignRight && !_wrap ? _scroll.horizontalScroller.highValue : 0f;
            _scroll.scrollOffset = new Vector2(x, _scroll.scrollOffset.y);
        }

        // Icon button that flips a state, shown pressed while on.
        public static Button CreateToggle(string icon, bool value, Action<bool> changed)
        {
            Button button = null;
            button = DebugMenuView.CreateIconButton(icon, () =>
            {
                var on = !button.ClassListContains("dm-icon-button--on");
                button.EnableInClassList("dm-icon-button--on", on);
                changed(on);
            });
            button.EnableInClassList("dm-icon-button--on", value);
            return button;
        }

        // Copy and, where available, share buttons for a text source.
        public static void AddCopyShare(VisualElement parent, Func<string> subject, Func<string> text)
        {
            parent.Add(DebugMenuView.CreateIconButton("content_copy", () => DebugShare.Copy(text())));
            if (DebugShare.IsAvailable)
                parent.Add(DebugMenuView.CreateIconButton("share", () => DebugShare.Share(subject(), text())));
        }
    }

    // Text shown by DebugMenu.ShowText.
    public sealed class DebugTextDocument
    {
        public string Title { get; }
        public string Text { get; }

        public DebugTextDocument(string title, string text)
        {
            Title = string.IsNullOrEmpty(title) ? "Text" : title;
            Text = text ?? string.Empty;
        }
    }

    internal sealed class DebugTextPreview : VisualElement
    {
        [EDIVE.External.DomainReloadHelper.ClearOnReload]
        private static bool _alignRight;

        [EDIVE.External.DomainReloadHelper.ClearOnReload(Value = true)]
        private static bool _wrap = true;

        public DebugTextPreview(DebugTextDocument document)
        {
            AddToClassList("dm-text-preview");

            var view = new DebugTextView {Text = document.Text, AlignRight = _alignRight, Wrap = _wrap};

            var toolbar = new VisualElement();
            toolbar.AddToClassList("dm-text-preview__toolbar");
            var info = new Label($"{document.Text.Length} characters, {CountLines(document.Text)} lines");
            info.AddToClassList("dm-text-preview__info");
            toolbar.Add(info);
            toolbar.Add(DebugTextView.CreateToggle("format_align_right", _alignRight, on => view.AlignRight = _alignRight = on));
            toolbar.Add(DebugTextView.CreateToggle("wrap_text", _wrap, on => view.Wrap = _wrap = on));
            DebugTextView.AddCopyShare(toolbar, () => document.Title, () => document.Text);
            Add(toolbar);
            Add(view);
        }

        private static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            var lines = 1;
            foreach (var c in text)
            {
                if (c == '\n') lines++;
            }
            return lines;
        }
    }
}
