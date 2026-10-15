// Author: František Holubec
// Created: 14.10.2026

using System.Linq;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Sections of one page as cards in columns, scrolls vertically.
    public sealed class DebugPageContent : VisualElement
    {
        private readonly ScrollView _scroll;

        public DebugPage Page { get; }

        public float ScrollOffset
        {
            get => _scroll.scrollOffset.y;
            set => _scroll.schedule.Execute(() => _scroll.scrollOffset = new UnityEngine.Vector2(0, value));
        }

        // Pick mode makes rows inert, a click toggles the favourite
        public DebugPageContent(DebugPage page, int columns, bool pick = false)
        {
            Page = page;
            AddToClassList("dm-page");

            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("dm-page__scroll");
            Add(_scroll);

            var row = new VisualElement();
            row.AddToClassList("dm-columns");
            _scroll.Add(row);

            // Fewer sections than columns leave columns empty, so cards keep their width
            DebugColumns.Fill(row, columns, page.VisibleSections.Select(s => ((VisualElement) new DebugSectionCard(s, pick), s.Controls.Count + 1)));
        }
    }

    internal sealed class DebugSectionCard : VisualElement
    {
        private readonly DebugSectionNode _section;
        private readonly VisualElement _body;
        private readonly Label _chevron;

        public DebugSectionCard(DebugSectionNode section, bool pick)
        {
            _section = section;
            AddToClassList("dm-card");

            if (!section.IsUntitled)
            {
                var header = new VisualElement();
                header.AddToClassList("dm-card__header");
                var title = new Label(section.Name);
                title.AddToClassList("dm-card__title");
                header.Add(title);
                _chevron = DebugIcons.Create("expand_less", "dm-card__chevron");
                header.Add(_chevron);
                header.AddManipulator(new Clickable(ToggleCollapsed));
                Add(header);
            }
            else
            {
                AddToClassList("dm-card--untitled");
            }

            _body = new VisualElement();
            _body.AddToClassList("dm-card__body");
            foreach (var control in section.SortedControls)
            {
                _body.Add(DebugPickRow.Create(control, pick));
            }
            Add(_body);

            ApplyCollapsed();
        }

        private void ToggleCollapsed()
        {
            _section.Collapsed = !_section.Collapsed;
            ApplyCollapsed();
        }

        // Collapsed rows are detached, so their getters are not read
        private void ApplyCollapsed()
        {
            var collapsed = _section.Collapsed && !_section.IsUntitled;
            EnableInClassList("dm-card--collapsed", collapsed);
            if (collapsed) _body.RemoveFromHierarchy();
            else if (_body.parent == null) Add(_body);

            if (_chevron != null)
                DebugIcons.Set(_chevron, collapsed ? "expand_more" : "expand_less");
        }
    }
}
