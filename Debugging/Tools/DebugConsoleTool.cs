// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Captured logs with level filters, collapse, search and the stack trace of the selected entry.
    public sealed class DebugConsoleTool : ADebugTool
    {
        public override string Name => "Console";
        public override string Icon => "terminal";
        public override int Order => -100;

        // Kept for the session, the view is recreated on every show
        internal readonly bool[] LevelFilters = {true, true, true};
        internal bool Collapse;
        internal bool AlignStackTrace;
        internal string Search = string.Empty;

        public override VisualElement CreateContent(DebugMenuView view) => new DebugConsoleView(this);

        public override VisualElement CreateBadge() => new DebugLogCountBadge(true);
    }

    internal sealed class DebugConsoleView : VisualElement
    {
        private const int ROW_HEIGHT = 56;

        private static readonly string[] LEVEL_ICONS = {"info", "warning", "error"};
        private static readonly string[] LEVEL_CLASSES = {"dm-level--log", "dm-level--warning", "dm-level--error"};

        private readonly DebugConsoleTool _tool;
        private readonly List<LogRow> _rows = new();
        private readonly Dictionary<(string, string, LogType), LogRow> _collapsed = new();
        private readonly Button[] _filterButtons = new Button[3];
        private readonly Label[] _filterCounts = new Label[3];
        private readonly Button _collapseButton;
        private readonly ListView _list;
        private readonly VisualElement _detail;
        private readonly Label _detailTitle;
        private readonly Label _detailIcon;
        private readonly DebugTextView _detailText;
        private bool _refreshScheduled;
        private LogRow _selected;

        public DebugConsoleView(DebugConsoleTool tool)
        {
            _tool = tool;
            AddToClassList("dm-console");

            var toolbar = new VisualElement();
            toolbar.AddToClassList("dm-console__toolbar");
            Add(toolbar);

            for (var i = 0; i < 3; i++)
            {
                var level = i;
                var button = new Button(() => ToggleLevel(level));
                button.AddToClassList("dm-filter");
                button.AddToClassList(LEVEL_CLASSES[i]);
                button.Add(DebugIcons.Create(LEVEL_ICONS[i], "dm-filter__icon"));
                _filterCounts[i] = new Label("0");
                _filterCounts[i].AddToClassList("dm-filter__count");
                button.Add(_filterCounts[i]);
                toolbar.Add(button);
                _filterButtons[i] = button;
            }

            _collapseButton = new Button(ToggleCollapse);
            _collapseButton.AddToClassList("dm-filter");
            _collapseButton.Add(DebugIcons.Create("unfold_less", "dm-filter__icon"));
            var collapseLabel = new Label("Collapse");
            collapseLabel.AddToClassList("dm-filter__count");
            _collapseButton.Add(collapseLabel);
            toolbar.Add(_collapseButton);

            var search = new TextField {value = tool.Search};
            search.AddToClassList("dm-field");
            search.AddToClassList("dm-console__search");
            search.textEdition.placeholder = "Search";
            search.RegisterValueChangedCallback(evt =>
            {
                _tool.Search = evt.newValue ?? string.Empty;
                RebuildRows(false);
            });
            toolbar.Add(search);

            toolbar.Add(DebugMenuView.CreateIconButton("delete_sweep", DebugLogBuffer.Clear, "dm-console__clear"));

            _list = new ListView(_rows, ROW_HEIGHT, MakeRow, BindRow)
            {
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight
            };
            _list.AddToClassList("dm-console__list");
            _list.selectionChanged += OnSelectionChanged;
            Add(_list);

            _detail = new VisualElement();
            _detail.AddToClassList("dm-console__detail");
            var detailHeader = new VisualElement();
            detailHeader.AddToClassList("dm-console__detail-header");
            _detailIcon = DebugIcons.Create("info", "dm-log__icon");
            detailHeader.Add(_detailIcon);
            _detailTitle = new Label();
            _detailTitle.AddToClassList("dm-console__detail-title");
            detailHeader.Add(_detailTitle);
            // Align only affects this preview, rows keep their alignment
            _detailText = new DebugTextView {AlignRight = tool.AlignStackTrace};
            _detailText.AddToClassList("dm-console__detail-text");
            detailHeader.Add(DebugTextView.CreateToggle("format_align_right", tool.AlignStackTrace, on => _detailText.AlignRight = _tool.AlignStackTrace = on));
            DebugTextView.AddCopyShare(detailHeader, () => _selected?.Entry.Title ?? "Log", GetSelectedText);
            // Closes the detail, the list gets the full height again
            detailHeader.Add(DebugMenuView.CreateIconButton("close", () => _list.ClearSelection()));
            _detail.Add(detailHeader);
            _detail.Add(_detailText);
            Add(_detail);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                DebugLogBuffer.Changed += ScheduleRefresh;
                RebuildRows(true);
            });
            RegisterCallback<DetachFromPanelEvent>(_ => DebugLogBuffer.Changed -= ScheduleRefresh);

            UpdateFilterButtons();
            ShowDetail(null);
        }

        private void ToggleLevel(int level)
        {
            _tool.LevelFilters[level] = !_tool.LevelFilters[level];
            UpdateFilterButtons();
            RebuildRows(false);
        }

        private void ToggleCollapse()
        {
            _tool.Collapse = !_tool.Collapse;
            UpdateFilterButtons();
            RebuildRows(false);
        }

        private void UpdateFilterButtons()
        {
            for (var i = 0; i < 3; i++)
            {
                _filterButtons[i].EnableInClassList("dm-filter--on", _tool.LevelFilters[i]);
            }
            _collapseButton.EnableInClassList("dm-filter--on", _tool.Collapse);
        }

        // Logs can arrive every frame, rebuild at most once per frame
        private void ScheduleRefresh()
        {
            if (_refreshScheduled)
                return;
            _refreshScheduled = true;
            schedule.Execute(() =>
            {
                _refreshScheduled = false;
                RebuildRows(false);
            });
        }

        private void RebuildRows(bool scrollToEnd)
        {
            var scroller = _list.Q<ScrollView>()?.verticalScroller;
            var atEnd = scrollToEnd || scroller == null || scroller.value >= scroller.highValue - 1f;

            _rows.Clear();
            _collapsed.Clear();
            var search = _tool.Search;
            foreach (var entry in DebugLogBuffer.Entries)
            {
                if (!_tool.LevelFilters[(int) entry.Level])
                    continue;
                if (search.Length > 0 && entry.Message.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (_tool.Collapse)
                {
                    var key = (entry.Message, entry.StackTrace, entry.Type);
                    if (_collapsed.TryGetValue(key, out var existing))
                    {
                        existing.Count++;
                        existing.Entry = entry;
                        continue;
                    }

                    var row = new LogRow(entry);
                    _collapsed.Add(key, row);
                    _rows.Add(row);
                    continue;
                }

                _rows.Add(new LogRow(entry));
            }

            for (var i = 0; i < 3; i++)
            {
                _filterCounts[i].text = DebugLogBuffer.Count((DebugLogLevel) i).ToString(CultureInfo.InvariantCulture);
            }

            _list.RefreshItems();
            if (atEnd && _rows.Count > 0)
                _list.ScrollToItem(_rows.Count - 1);

            if (_selected != null && !_rows.Contains(_selected))
                ShowDetail(null);
        }

        private VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("dm-log");
            row.Add(DebugIcons.Create("info", "dm-log__icon"));

            var texts = new VisualElement();
            texts.AddToClassList("dm-log__texts");
            var title = new Label();
            title.AddToClassList("dm-log__title");
            texts.Add(title);
            var source = new Label();
            source.AddToClassList("dm-log__source");
            texts.Add(source);
            row.Add(texts);

            var count = new Label();
            count.AddToClassList("dm-log__count");
            row.Add(count);

            var time = new Label();
            time.AddToClassList("dm-log__time");
            row.Add(time);
            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            var row = _rows[index];
            var entry = row.Entry;
            var level = (int) entry.Level;

            var icon = (Label) element[0];
            DebugIcons.Set(icon, LEVEL_ICONS[level]);
            for (var i = 0; i < 3; i++)
            {
                icon.EnableInClassList(LEVEL_CLASSES[i], i == level);
            }

            var texts = element[1];
            ((Label) texts[0]).text = entry.Title;
            ((Label) texts[1]).text = entry.Source;

            var count = (Label) element[2];
            count.text = row.Count.ToString(CultureInfo.InvariantCulture);
            count.style.display = row.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;

            ((Label) element[3]).text = entry.Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            LogRow selected = null;
            foreach (var item in selection)
            {
                selected = item as LogRow;
                break;
            }
            ShowDetail(selected);
        }

        private void ShowDetail(LogRow row)
        {
            _selected = row;
            _detail.style.display = row != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (row == null)
                return;

            var entry = row.Entry;
            var level = (int) entry.Level;
            DebugIcons.Set(_detailIcon, LEVEL_ICONS[level]);
            for (var i = 0; i < 3; i++)
            {
                _detailIcon.EnableInClassList(LEVEL_CLASSES[i], i == level);
            }
            _detailTitle.text = entry.Title;
            _detailText.Text = string.IsNullOrEmpty(entry.StackTrace) ? entry.Message : $"{entry.Message}\n\n{entry.StackTrace}";
        }

        private string GetSelectedText()
        {
            if (_selected == null)
                return string.Empty;
            var entry = _selected.Entry;
            return $"[{entry.Type}] {entry.Time:HH:mm:ss} {entry.Message}\n{entry.StackTrace}";
        }

        private sealed class LogRow
        {
            public DebugLogEntry Entry;
            public int Count = 1;

            public LogRow(DebugLogEntry entry) => Entry = entry;
        }
    }

    // Warning and error counts, hidden while zero. Optionally clickable through its parent.
    public sealed class DebugLogCountBadge : VisualElement
    {
        private readonly Label _warnings;
        private readonly Label _errors;
        private bool _scheduled;

        public DebugLogCountBadge(bool compact)
        {
            AddToClassList("dm-badges");
            EnableInClassList("dm-badges--compact", compact);
            pickingMode = compact ? PickingMode.Ignore : PickingMode.Position;
            _warnings = CreatePill("warning", "dm-level--warning", compact);
            _errors = CreatePill("error", "dm-level--error", compact);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                DebugLogBuffer.Changed += Schedule;
                UpdateCounts();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => DebugLogBuffer.Changed -= Schedule);
        }

        private Label CreatePill(string icon, string levelClass, bool compact)
        {
            var pill = new VisualElement {pickingMode = PickingMode.Ignore};
            pill.AddToClassList("dm-badge");
            pill.AddToClassList(levelClass);
            if (!compact)
                pill.Add(DebugIcons.Create(icon, "dm-badge__icon"));
            var count = new Label {pickingMode = PickingMode.Ignore};
            count.AddToClassList("dm-badge__count");
            pill.Add(count);
            Add(pill);
            return count;
        }

        private void Schedule()
        {
            if (_scheduled)
                return;
            _scheduled = true;
            schedule.Execute(() =>
            {
                _scheduled = false;
                UpdateCounts();
            });
        }

        private void UpdateCounts()
        {
            var warnings = DebugLogBuffer.Count(DebugLogLevel.Warning);
            var errors = DebugLogBuffer.Count(DebugLogLevel.Error);
            _warnings.text = warnings.ToString(CultureInfo.InvariantCulture);
            _errors.text = errors.ToString(CultureInfo.InvariantCulture);
            _warnings.parent.style.display = warnings > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _errors.parent.style.display = errors > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            style.display = warnings + errors > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
