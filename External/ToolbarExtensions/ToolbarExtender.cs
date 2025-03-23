#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace EDIVE.External.ToolbarExtensions
{
    public static class ToolbarExtender
    {
        public static VisualElement ToolbarVisualElement { get; private set; }
        public static event Action Refreshed;

        private static Type _toolbarType;
        private static Type ToolbarType => _toolbarType ??= typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");

        private static FieldInfo _rootField;
        private static FieldInfo RootField => _rootField ??= ToolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);

        private static MethodInfo _repaintMethod;
        private static MethodInfo RepaintMethod => _repaintMethod ??= ToolbarType.GetMethod("Repaint", BindingFlags.Instance | BindingFlags.Public);

        private static int _lastRebuildHash;
        private static int _newRebuildHash;

        private static ScriptableObject _currentToolbar;

        private static List<PriorityAction> _leftPlayButtonActions = new();
        private static List<PriorityAction> _leftToolbarActions = new();
        private static List<PriorityAction> _rightToolbarActions = new();

        public static void RepaintToolbar()
        {
            if (_currentToolbar != null)
                RepaintMethod?.Invoke(_currentToolbar, null);
        }

        public static void AddToRightToolbar(Action action, int priority) { AddToToolbar(_rightToolbarActions, action, priority); }

        public static void AddToLeftToolbar(Action action, int priority) { AddToToolbar(_leftToolbarActions, action, priority); }

        public static void AddToLeftPlayButtons(Action action, int priority) { AddToToolbar(_leftPlayButtonActions, action, priority); }

        private static void AddToToolbar(List<PriorityAction> toolbarList, Action action, int priority)
        {
            toolbarList.Add(new PriorityAction(action, priority));
            toolbarList.Sort();
        }

        [DidReloadScripts]
        [InitializeOnLoadMethod]
        public static void Reload()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            
            AddToLeftToolbar(() => GUILayout.Space(4), -10000);
            AddToLeftToolbar(GUILayout.FlexibleSpace, 0);
            AddToRightToolbar(GUILayout.FlexibleSpace, 0);
            AddToLeftToolbar(() => GUILayout.Space(4), 10000);
        }
        
        private static void OnUpdate()
        {
            if (_currentToolbar != null)
                return;

            var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
            _currentToolbar = toolbars.Length > 0 ? (ScriptableObject) toolbars[0] : null;
            if (_currentToolbar == null)
                return;

            if (RootField == null)
                return;

            ToolbarVisualElement = RootField.GetValue(_currentToolbar) as VisualElement;
            RegisterCallback(ToolbarVisualElement, "ToolbarZoneLeftAlign", DrawLeftGUI);
            RegisterCallback(ToolbarVisualElement, "ToolbarZoneRightAlign", DrawRightGUI);
            RegisterCallback(ToolbarVisualElement, "ToolbarZonePlayMode", DrawLeftPlayButtonsGUI, 0);
            Refreshed?.Invoke();
        }

        private static void RegisterCallback(VisualElement root, string rootElement, Action callback)
        {
            RegisterCallback(root, rootElement, callback, (toolbar, container) => { toolbar.Add(container); });
        }

        private static void RegisterCallback(VisualElement root, string rootElement, Action callback, int index)
        {
            RegisterCallback(root, rootElement, callback, (toolbar, container) => { toolbar.Insert(index, container); });
        }

        private static void RegisterCallback(VisualElement root, string rootElement, Action callback, Action<VisualElement, VisualElement> registerAction)
        {
            var toolbarZone = root.Q(rootElement);
            var parent = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row
                }
            };
            var container = new IMGUIContainer
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row
                }
            };
            container.onGUIHandler += () => { callback?.Invoke(); };
            parent.Add(container);
            registerAction?.Invoke(toolbarZone, parent);
        }

        private static void DrawLeftGUI() => DrawGUI(_leftToolbarActions);
        private static void DrawRightGUI() => DrawGUI(_rightToolbarActions);
        private static void DrawLeftPlayButtonsGUI() => DrawGUI(_leftPlayButtonActions);

        private static void DrawGUI(List<PriorityAction> actions)
        {
#if UNITY_6000_0_OR_NEWER
            const int height = 20;
#else
            // Standard height is 20, but unity is using 18, morons
            const int height = 18;
#endif
            GUILayout.Space(1);
            GUILayout.BeginHorizontal(GUILayout.Height(height));
            foreach (var priorityAction in actions)
            {
                try
                {
                    priorityAction.Action?.Invoke();
                }
                catch (ExitGUIException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            GUILayout.EndHorizontal();
        }

        private readonly struct PriorityAction : IComparable<PriorityAction>, IComparable
        {
            public readonly Action Action;
            private readonly int _priority;

            public PriorityAction(Action action, int priority)
            {
                _priority = priority;
                Action = action;
            }

            public int CompareTo(PriorityAction other) { return _priority.CompareTo(other._priority); }

            public int CompareTo(object obj)
            {
                if (ReferenceEquals(null, obj)) return 1;
                return obj is PriorityAction other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(PriorityAction)}");
            }
        }
    }
}
#endif
