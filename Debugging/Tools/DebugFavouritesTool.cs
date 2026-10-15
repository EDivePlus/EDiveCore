// Author: František Holubec
// Created: 15.10.2026

using System.Globalization;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Live copies of favourite controls, grouped by where they live. Add picks from search, Edit removes.
    public sealed class DebugFavouritesTool : ADebugTool
    {
        public override string Name => "Favourites";
        public override string Icon => "star";
        public override int Order => -80;

        public override VisualElement CreateContent(DebugMenuView view) => new DebugFavouritesView(view);

        public override VisualElement CreateBadge() => new DebugFavouritesCount();
    }

    internal sealed class DebugFavouritesView : VisualElement, IDebugHeaderContent
    {
        private readonly DebugMenuView _view;

        public VisualElement HeaderContent { get; }

        public DebugFavouritesView(DebugMenuView view)
        {
            _view = view;
            style.flexGrow = 1;

            var header = new VisualElement();
            header.AddToClassList("dm-header__group");
            var add = new Button(() => _view.OpenSearch(true));
            add.AddToClassList("dm-toggle");
            add.Add(DebugIcons.Create("add", "dm-toggle__icon"));
            var addLabel = new Label("Add");
            addLabel.AddToClassList("dm-toggle__label");
            add.Add(addLabel);
            header.Add(add);
            if (!view.PickMode)
                header.Add(DebugMenuView.CreateIconButton("edit", () => _view.SetPickMode(true)));
            HeaderContent = header;

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                DebugFavourites.Changed += OnFavouritesChanged;
                Build();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => DebugFavourites.Changed -= OnFavouritesChanged);
        }

        // Pick mode keeps removed rows, so a mistake can be undone
        private void OnFavouritesChanged()
        {
            if (!_view.PickMode)
                Build();
        }

        private void Build()
        {
            Clear();
            var groups = DebugControlGroup.FromControls(DebugFavourites.FindControls());
            Add(new DebugGroupsContent(groups, _view.Columns, _view.PickMode, _view.Navigate,
                emptyText: "No favourites yet. Use the star in a page header, or Add."));
        }
    }

    internal sealed class DebugFavouritesCount : Label
    {
        public DebugFavouritesCount()
        {
            AddToClassList("dm-nav-count");
            pickingMode = PickingMode.Ignore;
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                DebugFavourites.Changed += UpdateCount;
                UpdateCount();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => DebugFavourites.Changed -= UpdateCount);
        }

        private void UpdateCount()
        {
            var count = DebugFavourites.FindControls().Count;
            text = count.ToString(CultureInfo.InvariantCulture);
            style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
