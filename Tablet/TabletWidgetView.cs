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
        public TabletController Controller { get; }
        public TabletWidgetDefinition Definition { get; }

        public TabletWidgetViewContext(TabletController controller, TabletWidgetDefinition definition)
        {
            Controller = controller;
            Definition = definition;
        }
    }
}
