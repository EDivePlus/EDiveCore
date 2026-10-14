// Author: František Holubec
// Created: 14.10.2026

using System.Collections.Generic;
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

        public DebugPageContent(DebugPage page, int columns)
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
            var sections = page.VisibleSections.ToList();
            columns = UnityEngine.Mathf.Max(1, columns);

            var columnElements = new List<VisualElement>();
            var heights = new int[columns];
            for (var i = 0; i < columns; i++)
            {
                var column = new VisualElement();
                column.AddToClassList("dm-column");
                row.Add(column);
                columnElements.Add(column);
            }

            // Next section goes to the shortest column, height estimated by row count
            foreach (var section in sections)
            {
                var target = 0;
                for (var i = 1; i < columns; i++)
                {
                    if (heights[i] < heights[target])
                        target = i;
                }
                columnElements[target].Add(new DebugSectionCard(section));
                heights[target] += section.Controls.Count + 1;
            }
        }
    }

    internal sealed class DebugSectionCard : VisualElement
    {
        private readonly DebugSectionNode _section;
        private readonly VisualElement _body;
        private readonly Label _chevron;

        public DebugSectionCard(DebugSectionNode section)
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
                _body.Add(control.CreateView());
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
