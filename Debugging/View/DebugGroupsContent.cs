// Author: František Holubec
// Created: 15.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Controls gathered from several sections, one card per section titled by its path. Search and favourites.
    internal sealed class DebugGroupsContent : VisualElement
    {
        public DebugGroupsContent(IEnumerable<DebugControlGroup> groups, int columns, bool pick, Action<DebugPage> navigate, VisualElement top = null, string emptyText = null)
        {
            AddToClassList("dm-page");

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("dm-page__scroll");
            Add(scroll);

            if (top != null)
                scroll.Add(top);

            var cards = groups.Select(g => (card: (VisualElement) new DebugGroupCard(g, pick, navigate), weight: g.Controls.Count + 1)).ToList();
            if (cards.Count == 0)
            {
                if (emptyText != null)
                {
                    var empty = new Label(emptyText);
                    empty.AddToClassList("dm-empty");
                    scroll.Add(empty);
                }
                return;
            }

            var row = new VisualElement();
            row.AddToClassList("dm-columns");
            scroll.Add(row);
            DebugColumns.Fill(row, columns, cards);
        }
    }

    internal sealed class DebugControlGroup
    {
        public DebugSectionNode Section { get; }
        public List<ADebugControl> Controls { get; } = new();

        public DebugControlGroup(DebugSectionNode section) => Section = section;

        // Keeps the order of the controls, one group per section.
        public static List<DebugControlGroup> FromControls(IEnumerable<ADebugControl> controls)
        {
            var groups = new List<DebugControlGroup>();
            foreach (var control in controls)
            {
                var section = control.Handle?.Node;
                if (section == null)
                    continue;

                var group = groups.FirstOrDefault(g => g.Section == section);
                if (group == null)
                {
                    group = new DebugControlGroup(section);
                    groups.Add(group);
                }
                group.Controls.Add(control);
            }
            return groups;
        }
    }

    internal sealed class DebugGroupCard : VisualElement
    {
        public DebugGroupCard(DebugControlGroup group, bool pick, Action<DebugPage> navigate)
        {
            AddToClassList("dm-card");
            AddToClassList("dm-card--group");

            var header = new VisualElement();
            header.AddToClassList("dm-card__header");
            var path = new Label(GetPath(group.Section));
            path.AddToClassList("dm-card__path");
            header.Add(path);
            header.Add(DebugMenuView.CreateIconButton("arrow_forward", () => navigate(group.Section.Page), "dm-card__open"));
            Add(header);

            var body = new VisualElement();
            body.AddToClassList("dm-card__body");
            foreach (var control in group.Controls)
            {
                body.Add(DebugPickRow.Create(control, pick));
            }
            Add(body);
        }

        private static string GetPath(DebugSectionNode section)
        {
            var parts = new List<string>();
            for (var page = section.Page; page != null; page = page.Parent)
            {
                parts.Insert(0, page.Title);
            }
            if (!section.IsUntitled)
                parts.Add(section.Name);
            return string.Join("  ›  ", parts);
        }
    }

    // Row in pick mode: the control is inert, a click toggles the favourite.
    internal sealed class DebugPickRow : VisualElement
    {
        private readonly ADebugControl _control;

        public static VisualElement Create(ADebugControl control, bool pick) => pick ? new DebugPickRow(control) : (VisualElement) control.CreateView();

        private DebugPickRow(ADebugControl control)
        {
            _control = control;
            AddToClassList("dm-pick-row");

            var view = control.CreateView();
            view.AddToClassList("dm-pick-row__view");
            Add(view);

            // Takes every click before the control does
            var blocker = new VisualElement();
            blocker.AddToClassList("dm-pick-row__blocker");
            Add(blocker);

            var star = new VisualElement {pickingMode = PickingMode.Ignore};
            star.AddToClassList("dm-pick-row__star");
            star.Add(DebugIcons.Create("star"));
            Add(star);

            this.AddManipulator(new Clickable(() => DebugFavourites.Toggle(_control)));
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                DebugFavourites.Changed += UpdateState;
                UpdateState();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => DebugFavourites.Changed -= UpdateState);
        }

        private void UpdateState() => EnableInClassList("dm-pick-row--on", DebugFavourites.Contains(_control));
    }

    internal static class DebugColumns
    {
        // Each card goes to the shortest column, height estimated by weight
        public static void Fill(VisualElement row, int columns, IEnumerable<(VisualElement card, int weight)> cards)
        {
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

            foreach (var (card, weight) in cards)
            {
                var target = 0;
                for (var i = 1; i < columns; i++)
                {
                    if (heights[i] < heights[target])
                        target = i;
                }
                columnElements[target].Add(card);
                heights[target] += weight;
            }
        }
    }
}
