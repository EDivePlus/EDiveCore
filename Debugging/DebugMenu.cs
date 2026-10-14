// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.External.DomainReloadHelper;

namespace EDIVE.Debugging
{
    // Entry point for debug menu registrations. The assembly only exists with DEBUG_MENU, wrap call sites in #if DEBUG_MENU.
    //   DebugMenu.For(this).Section("Network", "Session").Toggle("Voice", () => _voice, v => _voice = v);
    // Page paths use '/', the first part is the panel: "Network/Players/History".
    public static class DebugMenu
    {
        public const char PAGE_SEPARATOR = '/';

        [ClearOnReload(NewInstance = true)]
        private static readonly List<DebugPage> PANELS = new();

        [ClearOnReload(NewInstance = true)]
        private static readonly List<ADebugTool> TOOLS = new();

        [ClearOnReload(NewInstance = true)]
        private static readonly Dictionary<object, DebugScope> SCOPES = new();

        [ClearOnReload(NewInstance = true)]
        private static readonly List<DebugMenuHost> HOSTS = new();

        [ClearOnReload]
        private static DebugScope _globalScope;

        [ClearOnReload]
        private static int _sequence;

        // Raised on any structure change: controls added or removed, order, icons, tools.
        [ClearOnReload]
        public static event Action Changed;

        public static IReadOnlyList<DebugPage> Panels => PANELS;
        public static IReadOnlyList<ADebugTool> Tools => TOOLS;
        public static IReadOnlyList<DebugMenuHost> Hosts => HOSTS;
        public static bool IsOpen => HOSTS.Any(h => h.IsOpen);

        private static DebugScope GlobalScope => _globalScope ??= new DebugScope(null);

        // Registrations owned by an object. A Unity object owner removes them when destroyed, others call Dispose.
        public static DebugScope For(object owner)
        {
            if (owner == null)
                return GlobalScope;

            if (!SCOPES.TryGetValue(owner, out var scope))
            {
                scope = new DebugScope(owner);
                SCOPES.Add(owner, scope);
            }
            return scope;
        }

        // Registrations without owner, they live until the returned handle is disposed.
        public static DebugSection Section(string page, string section = null) => GlobalScope.Section(page, section);

        public static DebugPage Panel(string name)
        {
            if (name != null && name.IndexOf(PAGE_SEPARATOR) >= 0)
                throw new ArgumentException($"Panel name can't contain '{PAGE_SEPARATOR}': {name}", nameof(name));
            return Page(name);
        }

        public static DebugPage Page(string path)
        {
            var parts = SplitPath(path);
            if (parts.Length == 0)
                throw new ArgumentException("Debug page path is empty.", nameof(path));

            var page = PANELS.FirstOrDefault(p => p.Name == parts[0]);
            if (page == null)
            {
                page = new DebugPage(parts[0], null, GlobalScope);
                PANELS.Add(page);
            }

            for (var i = 1; i < parts.Length; i++)
            {
                page = page.GetOrCreateChild(parts[i]);
            }
            return page;
        }

        public static bool TryFindPage(string path, out DebugPage page)
        {
            page = null;
            var parts = SplitPath(path);
            if (parts.Length == 0)
                return false;

            page = PANELS.FirstOrDefault(p => p.Name == parts[0]);
            for (var i = 1; i < parts.Length && page != null; i++)
            {
                page = page.FindChild(parts[i]);
            }
            return page != null;
        }

        public static ADebugTool FindTool(string name) => TOOLS.FirstOrDefault(t => t.Name == name);

        // Re-reads getters of shown controls. Null refreshes every panel.
        public static void Refresh(string path = null)
        {
            if (path == null)
            {
                foreach (var panel in PANELS)
                {
                    panel.Refresh();
                }
                return;
            }

            if (TryFindPage(path, out var page))
                page.Refresh();
        }

        // Opens the menu on every host. Path is a page path or a tool name, null keeps the last location.
        public static void Open(string path = null)
        {
            foreach (var host in HOSTS)
            {
                host.Open(path);
            }
        }

        // Opens a text page with copy, share, wrap and right alignment. Back returns to the previous location.
        public static void ShowText(string title, string text)
        {
            var document = new DebugTextDocument(title, text);
            foreach (var host in HOSTS)
            {
                host.ShowText(document);
            }
        }

        public static void Close()
        {
            foreach (var host in HOSTS)
            {
                host.Close();
            }
        }

        public static void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public static void AddTool(ADebugTool tool)
        {
            if (tool == null || TOOLS.Contains(tool))
                return;
            TOOLS.Add(tool);
            NotifyChanged();
        }

        public static void RemoveTool(ADebugTool tool)
        {
            if (TOOLS.Remove(tool))
                NotifyChanged();
        }

        internal static void RegisterHost(DebugMenuHost host)
        {
            if (!HOSTS.Contains(host))
                HOSTS.Add(host);
        }

        internal static void UnregisterHost(DebugMenuHost host) => HOSTS.Remove(host);

        internal static void RemoveScope(DebugScope scope)
        {
            if (scope.Owner != null)
                SCOPES.Remove(scope.Owner);
        }

        internal static void CleanupDestroyedOwners()
        {
            List<DebugScope> destroyed = null;
            foreach (var scope in SCOPES.Values)
            {
                if (scope.IsOwnerDestroyed)
                    (destroyed ??= new List<DebugScope>()).Add(scope);
            }

            if (destroyed == null)
                return;

            foreach (var scope in destroyed)
            {
                scope.Dispose();
            }
        }

        internal static int NextSequence() => ++_sequence;

        internal static void NotifyChanged() => Changed?.Invoke();

        internal static string[] SplitPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Array.Empty<string>();

            return path.Split(PAGE_SEPARATOR)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToArray();
        }
    }
}
