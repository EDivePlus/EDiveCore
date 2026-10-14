// Author: František Holubec
// Created: 14.10.2026

using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Built-in screen listed above the panels in the sidebar, like Console or System.
    public abstract class ADebugTool
    {
        public abstract string Name { get; }
        public abstract string Icon { get; }
        public virtual int Order => 0;

        // Content of the main area, created each time the tool is shown.
        public abstract VisualElement CreateContent(DebugMenuView view);

        // Optional element on the right of the sidebar item.
        public virtual VisualElement CreateBadge() => null;
    }

    // Tool whose content is a page of sections, built once.
    public abstract class ADebugPageTool : ADebugTool
    {
        private DebugPage _page;

        protected abstract void Build(DebugPage page);

        public override VisualElement CreateContent(DebugMenuView view)
        {
            if (_page == null)
            {
                _page = DebugPage.CreateDetached(Name);
                Build(_page);
            }
            return new DebugPageContent(_page, view.Columns);
        }
    }
}
