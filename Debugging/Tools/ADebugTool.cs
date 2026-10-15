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

    // Content that puts its own controls into the menu header, like the console filters.
    public interface IDebugHeaderContent
    {
        VisualElement HeaderContent { get; }
    }

    // Content with header controls next to it.
    public sealed class DebugHeaderContent : VisualElement, IDebugHeaderContent
    {
        public VisualElement HeaderContent { get; }

        public DebugHeaderContent(VisualElement content, VisualElement headerContent)
        {
            HeaderContent = headerContent;
            style.flexGrow = 1;
            Add(content);
        }
    }

    // Tool whose content is a page of sections, built once.
    public abstract class ADebugPageTool : ADebugTool
    {
        private DebugPage _page;

        protected abstract void Build(DebugPage page);

        protected virtual VisualElement CreateHeaderContent(DebugPage page) => null;

        public override VisualElement CreateContent(DebugMenuView view)
        {
            if (_page == null)
            {
                _page = DebugPage.CreateDetached(Name);
                Build(_page);
            }
            var content = new DebugPageContent(_page, view.Columns);
            var header = CreateHeaderContent(_page);
            return header != null ? new DebugHeaderContent(content, header) : content;
        }
    }
}
