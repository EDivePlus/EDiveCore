// Author: Michal Petr
// Created: 03.03.2026

namespace EDIVE.Tablet
{
    public class TabletWidgetView : ATabletView<TabletWidgetViewContext>
    {
        public TabletWidgetDefinition Definition => Context.Definition;
    }

    public class TabletWidgetViewContext : ITabletViewContext
    {
        public TabletWidgetDefinition Definition { get; }

        public TabletWidgetViewContext(TabletWidgetDefinition definition)
        {
            Definition = definition;
        }
    }
}
