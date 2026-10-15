// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.External.DomainReloadHelper;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Whole debug menu: sidebar, header, page row and content. Same element on every host,
    // narrow widths turn the sidebar into a drawer and the column count follows the width.
    public sealed class DebugMenuView : VisualElement
    {
        public const string ROOT_CLASS = "dm-root";
        private const string NARROW_CLASS = "dm-root--narrow";
        private const string DRAWER_OPEN_CLASS = "dm-root--drawer-open";
        private const string SELECTED_CLASS = "dm-nav-item--selected";

        private const float NARROW_WIDTH = 900f;
        private const float SIDEBAR_WIDTH = 208f;
        private const float CONTENT_PADDING = 32f;
        private const float COLUMN_GAP = 12f;
        private const int MAX_SEARCH_RESULTS = 100;
        private const float COLUMN_MIN_WIDTH = 300f;
        private const int MAX_CRUMBS = 2;

        // Last location of any host, reopened next time
        [ClearOnReload]
        private static string _lastLocation;

        private readonly VisualElement _sidebar;
        private readonly VisualElement _toolList;
        private readonly VisualElement _panelList;
        private readonly Label _panelsCaption;
        private readonly VisualElement _main;
        private readonly VisualElement _header;
        private readonly VisualElement _subHeader;
        private readonly Button _menuButton;
        private readonly Button _backButton;
        private readonly VisualElement _crumbs;
        private readonly VisualElement _searchBox;
        private readonly TextField _searchField;
        private readonly Label _searchInfo;
        private readonly VisualElement _headerSpacer;
        private readonly VisualElement _headerExtra;
        private readonly Button _pickButton;
        private readonly Button _doneButton;
        private readonly Button _searchButton;
        private readonly ScrollView _pages;
        private readonly VisualElement _content;
        private readonly VisualElement _scrim;

        private ADebugTool _tool;
        private DebugPage _page;
        // Text from ShowText, shown over the current location until back
        private DebugTextDocument _document;
        private VisualElement _contentElement;
        private bool _rebuildScheduled;
        private bool _narrow;
        private bool _searchMode;
        private bool _pickMode;
        private string _searchText = string.Empty;
        private IVisualElementScheduledItem _searchRebuild;

        public int Columns { get; private set; } = 1;
        public bool PickMode => _pickMode;
        public bool IsNarrow => _narrow;
        public ADebugTool CurrentTool => _tool;
        public DebugPage CurrentPage => _page;

        public event Action CloseRequested;

        public DebugMenuView(bool showCloseButton = true)
        {
            AddToClassList(ROOT_CLASS);

            // Sidebar
            _sidebar = new VisualElement();
            _sidebar.AddToClassList("dm-sidebar");

            var brand = new VisualElement();
            brand.AddToClassList("dm-brand");
            brand.Add(DebugIcons.Create("bug_report", "dm-brand__icon"));
            var brandLabel = new Label("Debug");
            brandLabel.AddToClassList("dm-brand__label");
            brand.Add(brandLabel);
            _sidebar.Add(brand);

            var navScroll = new ScrollView(ScrollViewMode.Vertical);
            navScroll.AddToClassList("dm-sidebar__scroll");
            _sidebar.Add(navScroll);

            _toolList = new VisualElement();
            _toolList.AddToClassList("dm-nav-list");
            navScroll.Add(_toolList);

            _panelsCaption = new Label("Panels");
            _panelsCaption.AddToClassList("dm-sidebar__caption");
            navScroll.Add(_panelsCaption);

            _panelList = new VisualElement();
            _panelList.AddToClassList("dm-nav-list");
            navScroll.Add(_panelList);

            var version = new Label(Application.version);
            version.AddToClassList("dm-sidebar__version");
            _sidebar.Add(version);

            // Main area
            _main = new VisualElement();
            _main.AddToClassList("dm-main");

            // Tool controls sit in the header row when wide, in the row below with the page chips when narrow
            _header = new VisualElement();
            _header.AddToClassList("dm-header");
            _menuButton = CreateIconButton("menu", () => SetDrawerOpen(true), "dm-header__menu");
            _header.Add(_menuButton);
            _backButton = CreateIconButton("arrow_back", NavigateBack, "dm-header__back");
            _header.Add(_backButton);
            _crumbs = new VisualElement();
            _crumbs.AddToClassList("dm-crumbs");
            _header.Add(_crumbs);

            _searchBox = new VisualElement();
            _searchBox.AddToClassList("dm-search");
            _searchBox.Add(DebugIcons.Create("search", "dm-search__icon"));
            _searchField = new TextField();
            _searchField.AddToClassList("dm-search__field");
            _searchField.textEdition.placeholder = "Search controls and pages";
            _searchField.RegisterValueChangedCallback(evt => SetSearchText(evt.newValue));
            _searchBox.Add(_searchField);
            _searchInfo = new Label();
            _searchInfo.AddToClassList("dm-search__info");
            _searchBox.Add(_searchInfo);
            _searchBox.Add(CreateIconButton("close", CloseSearch, "dm-search__close"));
            _header.Add(_searchBox);

            _pages = new ScrollView(ScrollViewMode.Horizontal)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden
            };
            _pages.AddToClassList("dm-pages");

            _headerSpacer = new VisualElement();
            _headerSpacer.AddToClassList("dm-header__spacer");
            _header.Add(_headerSpacer);

            _headerExtra = new VisualElement();
            _headerExtra.AddToClassList("dm-header__extra");
            _header.Add(_headerExtra);

            _pickButton = CreateIconButton("star", () => SetPickMode(true), "dm-header__pick");
            _pickButton.tooltip = "Pick favourites";
            _header.Add(_pickButton);
            _doneButton = new Button(() => SetPickMode(false)) {text = "Done"};
            _doneButton.AddToClassList("dm-header__done");
            _header.Add(_doneButton);
            _searchButton = CreateIconButton("search", () => OpenSearch(false), "dm-header__search");
            _header.Add(_searchButton);
            if (showCloseButton)
                _header.Add(CreateIconButton("close", () => CloseRequested?.Invoke(), "dm-header__close"));
            _main.Add(_header);

            _subHeader = new VisualElement();
            _subHeader.AddToClassList("dm-subheader");
            _subHeader.Add(_pages);
            _subHeader.style.display = DisplayStyle.None;
            _main.Add(_subHeader);

            _content = new VisualElement();
            _content.AddToClassList("dm-content");
            _main.Add(_content);

            _scrim = new VisualElement();
            _scrim.AddToClassList("dm-scrim");
            _scrim.RegisterCallback<PointerDownEvent>(_ => SetDrawerOpen(false));

            Add(_sidebar);
            Add(_main);
            Add(_scrim);

            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        // Page path or tool name, null keeps the current or last location.
        public void Navigate(string location)
        {
            if (string.IsNullOrEmpty(location))
                return;

            var tool = DebugMenu.FindTool(location);
            if (tool != null)
            {
                Navigate(tool);
                return;
            }

            if (DebugMenu.TryFindPage(location, out var page))
                Navigate(page);
        }

        public void Navigate(DebugPage page)
        {
            SetLocation(null, page);
        }

        public void Navigate(ADebugTool tool)
        {
            SetLocation(tool, null);
        }

        public void Show(DebugTextDocument document)
        {
            _document = document;
            SetDrawerOpen(false);
            if (panel != null)
                Rebuild(false);
        }

        // Search results are live controls. With pick on, a click on a result toggles its favourite.
        public void OpenSearch(bool pick)
        {
            _searchMode = true;
            _pickMode = pick;
            SetDrawerOpen(false);
            if (panel == null)
                return;
            Rebuild(false);
            _searchField.schedule.Execute(() => _searchField.Focus());
        }

        public void CloseSearch()
        {
            if (!_searchMode)
                return;
            _searchMode = false;
            _pickMode = false;
            if (panel != null)
                Rebuild(false);
        }

        public void SetPickMode(bool pick)
        {
            if (_pickMode == pick)
                return;
            _pickMode = pick;
            if (panel != null)
                Rebuild(true);
        }

        private void SetSearchText(string text)
        {
            _searchText = text ?? string.Empty;
            _searchRebuild?.Pause();
            _searchRebuild = schedule.Execute(() => RebuildContent(false)).StartingIn(150);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape && (_searchMode || _pickMode))
            {
                if (_searchMode) CloseSearch();
                else SetPickMode(false);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.F && evt.actionKey)
            {
                OpenSearch(false);
                evt.StopPropagation();
            }
        }

        private void SetLocation(ADebugTool tool, DebugPage page)
        {
            var samePage = page != null && page == _page && _document == null && !_searchMode;
            _document = null;
            _searchMode = false;
            _pickMode = false;
            _tool = tool;
            _page = page;
            _lastLocation = tool != null ? tool.Name : page?.Path;
            SetDrawerOpen(false);
            if (panel != null)
                Rebuild(samePage);
        }

        private void NavigateBack()
        {
            if (_document != null)
            {
                _document = null;
                Rebuild(false);
                return;
            }

            var parent = _page?.Parent;
            while (parent != null && !parent.HasContent)
            {
                parent = parent.Parent;
            }
            if (parent != null)
                Navigate(parent);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            DebugMenu.Changed += ScheduleRebuild;
            if (_tool == null && _page == null)
                Navigate(_lastLocation);
            Rebuild(false);
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            DebugMenu.Changed -= ScheduleRebuild;
            // Detaches rows, so getters stop until shown again
            ClearContent();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            var width = evt.newRect.width;
            if (width <= 0f)
                return;

            var narrow = width < NARROW_WIDTH;
            if (narrow != _narrow)
                SetNarrow(narrow);

            var contentWidth = width - (narrow ? 0f : SIDEBAR_WIDTH) - CONTENT_PADDING;
            var columns = Mathf.Max(1, Mathf.FloorToInt((contentWidth + COLUMN_GAP) / (COLUMN_MIN_WIDTH + COLUMN_GAP)));
            if (columns != Columns)
            {
                Columns = columns;
                RebuildContent(true);
            }
        }

        private void SetNarrow(bool narrow)
        {
            _narrow = narrow;
            EnableInClassList(NARROW_CLASS, narrow);
            // After the search box and the spacer
            if (narrow) _subHeader.Add(_headerExtra);
            else _header.Insert(_header.IndexOf(_searchBox) + 2, _headerExtra);
            UpdateSubHeader();
            // Drawer has to be drawn over the main area
            if (narrow) _sidebar.BringToFront();
            else _sidebar.SendToBack();
            SetDrawerOpen(false);
        }

        private void SetDrawerOpen(bool open)
        {
            EnableInClassList(DRAWER_OPEN_CLASS, open && _narrow);
        }

        private void ScheduleRebuild()
        {
            if (_rebuildScheduled || panel == null)
                return;
            _rebuildScheduled = true;
            schedule.Execute(() =>
            {
                _rebuildScheduled = false;
                if (panel != null)
                    Rebuild(true);
            });
        }

        private void Rebuild(bool keepScroll)
        {
            ValidateLocation();
            RebuildSidebar();
            RebuildHeader();
            RebuildPages();
            RebuildContent(keepScroll);
        }

        // Falls back when the shown page lost its controls
        private void ValidateLocation()
        {
            if (_tool != null && DebugMenu.Tools.Contains(_tool))
                return;

            if (_page != null)
            {
                var page = _page;
                while (page != null && !page.HasContent)
                {
                    page = page.Parent;
                }

                if (page != null)
                {
                    _page = page;
                    _tool = null;
                    return;
                }
            }

            _page = VisiblePanels().FirstOrDefault();
            _tool = _page == null ? SortedTools().FirstOrDefault() : null;
        }

        private void RebuildSidebar()
        {
            _toolList.Clear();
            foreach (var tool in SortedTools())
            {
                var item = CreateNavItem(tool.Icon, tool.Name, () => Navigate(tool));
                var badge = tool.CreateBadge();
                if (badge != null)
                    item.Add(badge);
                item.EnableInClassList(SELECTED_CLASS, tool == _tool);
                _toolList.Add(item);
            }

            _panelList.Clear();
            var currentPanel = GetPanel(_page);
            foreach (var panel in VisiblePanels())
            {
                var item = CreateNavItem(panel.IconName ?? "tune", panel.Title, () => Navigate(panel));
                item.EnableInClassList(SELECTED_CLASS, panel == currentPanel && _tool == null);
                _panelList.Add(item);
            }

            _panelsCaption.style.display = _panelList.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RebuildHeader()
        {
            _crumbs.Clear();
            var canGoBack = !_searchMode && (_document != null || (_tool == null && _page is {IsPanel: false}));
            _backButton.style.display = canGoBack ? DisplayStyle.Flex : DisplayStyle.None;
            SetShown(_crumbs, !_searchMode);
            SetShown(_searchBox, _searchMode);
            SetShown(_headerSpacer, !_searchMode);
            SetShown(_searchButton, !_searchMode);
            SetShown(_pickButton, !_searchMode && !_pickMode && _document == null && _tool == null && _page != null);
            SetShown(_doneButton, _pickMode);

            if (_searchMode)
                return;

            if (_document != null)
            {
                var returnTitle = _tool?.Name ?? _page?.Title;
                if (returnTitle != null)
                {
                    var crumb = new Button(NavigateBack) {text = returnTitle};
                    crumb.AddToClassList("dm-crumb");
                    AddCrumb(crumb);
                }
                _crumbs.Add(CreateTitle(_document.Title));
                return;
            }

            if (_tool != null)
            {
                _crumbs.Add(CreateTitle(_tool.Name));
                return;
            }

            if (_page == null)
                return;

            var ancestors = new List<DebugPage>();
            for (var p = _page.Parent; p != null; p = p.Parent)
            {
                ancestors.Insert(0, p);
            }

            for (var i = 0; i < ancestors.Count; i++)
            {
                // Long paths keep the panel and the direct parent
                if (ancestors.Count > MAX_CRUMBS && i > 0 && i < ancestors.Count - 1)
                {
                    if (i == 1)
                        AddCrumb(new Label("…"));
                    continue;
                }

                var ancestor = ancestors[i];
                var crumb = new Button(() => Navigate(ancestor)) {text = ancestor.Title};
                crumb.AddToClassList("dm-crumb");
                AddCrumb(crumb);
            }

            _crumbs.Add(CreateTitle(_page.Title));
        }

        private static void SetShown(VisualElement element, bool shown) => element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;

        private void AddCrumb(VisualElement crumb)
        {
            if (crumb is Label label)
                label.AddToClassList("dm-crumb");
            _crumbs.Add(crumb);
            _crumbs.Add(DebugIcons.Create("chevron_right", "dm-crumb__separator"));
        }

        private void RebuildPages()
        {
            _pages.Clear();
            var children = _tool == null && _document == null && !_searchMode ? _page?.VisibleChildren.ToList() : null;
            if (children == null || children.Count == 0)
            {
                _pages.style.display = DisplayStyle.None;
                return;
            }

            _pages.style.display = DisplayStyle.Flex;
            foreach (var child in children)
            {
                _pages.Add(CreatePageChip(child, child.Title));
            }
        }

        private void RebuildContent(bool keepScroll)
        {
            var oldPage = _contentElement as DebugPageContent;
            var scroll = keepScroll && oldPage != null && oldPage.Page == _page ? oldPage.ScrollOffset : 0f;
            ClearContent();

            if (panel == null)
                return;

            if (_searchMode)
            {
                _contentElement = CreateSearchContent();
            }
            else if (_document != null)
            {
                _contentElement = new DebugTextPreview(_document);
            }
            else if (_tool != null)
            {
                _contentElement = _tool.CreateContent(this);
            }
            else if (_page != null)
            {
                var pageContent = new DebugPageContent(_page, Columns, _pickMode);
                if (scroll > 0f)
                    pageContent.ScrollOffset = scroll;
                _contentElement = pageContent;
            }
            else
            {
                var empty = new Label("Nothing registered yet.");
                empty.AddToClassList("dm-empty");
                _contentElement = empty;
            }

            if (_contentElement != null)
                _content.Add(_contentElement);

            if (_contentElement is IDebugHeaderContent {HeaderContent: { } headerContent})
                _headerExtra.Add(headerContent);
            UpdateSubHeader();
        }

        private void UpdateSubHeader() => SetShown(_subHeader, _pages.style.display != DisplayStyle.None || (_narrow && _headerExtra.childCount > 0));

        private void ClearContent()
        {
            _content.Clear();
            _headerExtra.Clear();
            _contentElement = null;
        }

        private VisualElement CreateSearchContent()
        {
            var query = _searchText.Trim();
            if (query.Length == 0)
            {
                _searchInfo.text = string.Empty;
                return new DebugGroupsContent(Array.Empty<DebugControlGroup>(), Columns, _pickMode, Navigate, emptyText: "Type to search labels, sections and pages.");
            }

            var controls = DebugMenu.AllControls()
                .Where(c => !c.IsOwnerDestroyed && (Contains(c.SearchText, query) || Contains(c.Handle?.Name, query)))
                .Take(MAX_SEARCH_RESULTS)
                .ToList();
            var pages = DebugMenu.AllPages()
                .Where(p => p.HasContent && Contains(p.Title, query))
                .ToList();
            _searchInfo.text = controls.Count == 1 ? "1 result" : $"{controls.Count} results";

            VisualElement top = null;
            if (pages.Count > 0)
            {
                top = new VisualElement();
                top.AddToClassList("dm-search__pages");
                foreach (var page in pages)
                {
                    top.Add(CreatePageChip(page, GetPathTitle(page)));
                }
            }

            return new DebugGroupsContent(DebugControlGroup.FromControls(controls), Columns, _pickMode, Navigate, top, "Nothing found.");
        }

        private static bool Contains(string text, string query) => text != null && text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

        private static string GetPathTitle(DebugPage page)
        {
            var parts = new List<string>();
            for (var p = page; p != null; p = p.Parent)
            {
                parts.Insert(0, p.Title);
            }
            return string.Join("  ›  ", parts);
        }

        private Button CreatePageChip(DebugPage page, string title)
        {
            var chip = new Button(() => Navigate(page));
            chip.AddToClassList("dm-chip");
            if (!string.IsNullOrEmpty(page.IconName))
                chip.Add(DebugIcons.Create(page.IconName, "dm-chip__icon"));
            var label = new Label(title);
            label.AddToClassList("dm-chip__label");
            chip.Add(label);
            chip.Add(DebugIcons.Create("chevron_right", "dm-chip__chevron"));
            return chip;
        }

        private static IEnumerable<DebugPage> VisiblePanels() => DebugMenu.Panels
            .Where(p => p.HasContent)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Sequence);

        private static IEnumerable<ADebugTool> SortedTools() => DebugMenu.Tools.OrderBy(t => t.Order);

        private static DebugPage GetPanel(DebugPage page)
        {
            while (page?.Parent != null)
            {
                page = page.Parent;
            }
            return page;
        }

        private static Label CreateTitle(string text)
        {
            var title = new Label(text);
            title.AddToClassList("dm-title");
            return title;
        }

        private static Button CreateNavItem(string icon, string text, Action clicked)
        {
            var item = new Button(clicked);
            item.AddToClassList("dm-nav-item");
            item.Add(DebugIcons.Create(icon, "dm-nav-item__icon"));
            var label = new Label(text);
            label.AddToClassList("dm-nav-item__label");
            item.Add(label);
            return item;
        }

        internal static Button CreateIconButton(string icon, Action clicked, string className = null)
        {
            var button = new Button(clicked);
            button.AddToClassList("dm-icon-button");
            if (className != null)
                button.AddToClassList(className);
            button.Add(DebugIcons.Create(icon));
            return button;
        }
    }
}
