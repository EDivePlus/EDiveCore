#if UNITY_EDITOR
using System;
using System.Linq;
using EDIVE.External.DomainReloadHelper;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace EDIVE.External.SelectionHistory
{
    public static class SelectionHistoryManager
    {
        private static GlobalPersistentContext<bool> EnableSelectionHistoryContext => 
            PersistentContext.Get("SelectionHistoryManager.EnableHistory", true);
        
        public static bool EnableSelectionHistory
        {
            get => EnableSelectionHistoryContext.Value;
            set
            {
                EnableSelectionHistoryContext.Value = value;
                if (EnableSelectionHistory) Enable();
                else Disable();
            }
        }

        private static bool _ignoreSelectionChange = false;

        public static HistoryBuffer<SelectionSnapshot> History { get; private set; } = new HistoryBuffer<SelectionSnapshot>(50);
        public static SelectionSnapshot Current => History.Current();
        
        public static bool SelectionIsEmpty => Selection.activeObject == null;
        public static int Size => History?.Size ?? 0;
        public static Action HistoryChanged;

        [InitializeOnLoadMethod]
        [ExecuteOnReload]
        public static void Initialize()
        {
            if (EnableSelectionHistory)
            {
                Enable();
            }
        }

        private static void Enable()
        {
            Disable();
            HistoryButtonsHandler.AddListener(OnHistoryButtonPressed);
            Selection.selectionChanged += OnSelectionChanged;
        }
        
        private static void Disable()
        {
            HistoryButtonsHandler.RemoveListener(OnHistoryButtonPressed);
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private static void OnHistoryButtonPressed(HistoryButtonType buttonType)
        {
            switch (buttonType)
            {
                case HistoryButtonType.Forward: Forward(); break;
                case HistoryButtonType.Backward: Back(); break;
            }
        }

        private static void OnSelectionChanged()
        {
            if (_ignoreSelectionChange)
            {
                _ignoreSelectionChange = false;
                return;
            }

            if (SelectionIsEmpty) return;
            History.Push(TakeSnapshot());
            HistoryChanged?.Invoke();
        }

        public static SelectionSnapshot TakeSnapshot()
        {
            return new SelectionSnapshot(Selection.activeObject, Selection.objects, Selection.activeContext);
        }

        public static void Select(SelectionSnapshot snapshot)
        {
            Selection.SetActiveObjectWithContext(snapshot.ActiveObject, snapshot.Context);
            Selection.objects = snapshot.Objects;
        }

        public static void Select(int index)
        {
            History.SetCurrent(index);
            Select(History.Current());
            HistoryChanged?.Invoke();
        }

        [Shortcut("History/Back", null, KeyCode.Home, ShortcutModifiers.Alt)]
        public static void Back()
        {
            if (History.Size <= 0) return;

            var prev = SelectionIsEmpty?History.Current():History.Previous();

            if (prev.IsEmpty)
            {
                History.Next();
                ClearEmptyEntries();
                Back();
            }

            _ignoreSelectionChange = true;
            Select(prev);
            HistoryChanged?.Invoke();
        }

        [Shortcut("History/Forward", null, KeyCode.End, ShortcutModifiers.Alt)]
        public static void Forward()
        {
            if (History.Size <= 0) return;
            
            var next = SelectionIsEmpty?History.Current():History.Next();

            if (next.IsEmpty)
            {
                History.Previous();
                ClearEmptyEntries();
                Forward();
            }

            _ignoreSelectionChange = true;
            Select(next);
            HistoryChanged?.Invoke();
        }

        [Shortcut("History/Clear", null)]
        public static void Clear()
        {
            History.Clear();
            HistoryChanged?.Invoke();
        }

        /// <summary>
        /// Makes selection history ignore the next selection
        /// </summary>
        public static void HideSelectionFromHistory()
        {
            _ignoreSelectionChange = true;
        }

        /// <summary>
        /// Removes deleted objects from history buffer
        /// </summary>
        private static void ClearEmptyEntries()
        {
            var array = History.ToArray().ToList();
            var current = History.GetCurrentArrayIndex();

            for (var i = array.Count - 1; i >= 0; i--)
            {
                if (array[i].IsEmpty)
                {
                    array.RemoveAt(i);
                    if (current >= i) current--;
                }
            }

            History = HistoryBuffer<SelectionSnapshot>.FromArray(array.ToArray(), current, 50);
        }
    }
}
#endif
