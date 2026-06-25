// Author: Radim Holub
// Created: 10.10.2025

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using PurrNet;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.Networking.Utils
{
    public class VisibilityObserverUpdater : NetworkBehaviour
    {
        [ReadOnly]
        [ShowInInspector]
        private List<IObserverUpdaterRule> _rules;

        private PlayerID _trackedLocalPlayer;
        private bool _isLocallyVisible = true;
        private bool _eventsSubscribed;

        private void Awake()
        {
            _rules = GetRuleTemplates().Select(r => r.GetCopy()).ToList();
            var rootGameObjects = gameObject.scene.GetRootGameObjects();
            _rules.ForEach(r => r.Initialize(rootGameObjects));
        }
        
        protected override void OnSpawned()
        {
            if (!isHost) return;
            if (!localPlayer.HasValue) return;

            _trackedLocalPlayer = localPlayer.Value;

            var nowVisible = IsObserver(_trackedLocalPlayer);
            if (nowVisible != _isLocallyVisible)
            {
                ApplyVisibility(_isLocallyVisible, nowVisible);
                _isLocallyVisible = nowVisible;
            }

            onObserverAdded += HandleObserverAdded;
            onObserverRemoved += HandleObserverRemoved;
            _eventsSubscribed = true;
        }

        protected override void OnDespawned()
        {
            if (!_eventsSubscribed) return;
            onObserverAdded -= HandleObserverAdded;
            onObserverRemoved -= HandleObserverRemoved;
            _eventsSubscribed = false;
        }

        private void HandleObserverAdded(PlayerID player)
        {
            if (player != _trackedLocalPlayer) return;
            if (_isLocallyVisible) return;
            ApplyVisibility(false, true);
            _isLocallyVisible = true;
        }

        private void HandleObserverRemoved(PlayerID player)
        {
            if (player != _trackedLocalPlayer) return;
            if (!_isLocallyVisible) return;
            ApplyVisibility(true, false);
            _isLocallyVisible = false;
        }

        private void ApplyVisibility(bool prevVisible, bool nextVisible)
        {
            if (_rules == null) return;
            foreach (var rule in _rules)
                rule?.UpdateVisibility(prevVisible, nextVisible);
        }
        
        private static List<IObserverUpdaterRule> _ruleTemplates;
        private static List<IObserverUpdaterRule> GetRuleTemplates()
        {
            if(_ruleTemplates != null)
                return _ruleTemplates;
            
            _ruleTemplates = ReflectionExtensions.GetAssignableClassesOfType<IObserverUpdaterRule>().ToList();
            return _ruleTemplates;
        }
    }
    
    public interface IObserverUpdaterRule
    {
        void Initialize(GameObject[] targetGameObjects);
        void UpdateVisibility(bool prevVisible, bool nextVisible);
        IObserverUpdaterRule GetCopy();
    }
    
    [Serializable]
    public abstract class AObserverUpdaterRule<TSelf> : IObserverUpdaterRule
        where TSelf : IObserverUpdaterRule, new()
    {
        protected abstract string Label { get; }
        public abstract void Initialize(GameObject[] targetGameObjects);
        public abstract void UpdateVisibility(bool prevVisible, bool nextVisible);
        public virtual IObserverUpdaterRule GetCopy() => new TSelf();

#if UNITY_EDITOR
        [PropertyOrder(-100)]
        [OnInspectorGUI]
        private void DrawLabel() => GUILayout.Label(GUIHelper.TempContent(Label), SirenixGUIStyles.BoldLabel);
#endif
    }
    
    [Serializable]
    public abstract class AObjectObserverUpdaterRule<TSelf, T> : AObserverUpdaterRule<TSelf> 
        where TSelf : IObserverUpdaterRule, new()
        where T : Object
    {
        [SerializeField]
        protected List<T> _Targets;
            
        public override void UpdateVisibility(bool prevVisible, bool nextVisible)
        {
            _Targets?.RemoveAll(t => t == null);
            _Targets?.ForEach(t => UpdateTarget(t, prevVisible, nextVisible));
        }
        protected abstract void UpdateTarget(T target, bool prevVisible, bool nextVisible);
    }
    
    [Serializable]
    public abstract class AComponentObserverUpdaterRule<TSelf, T> : AObjectObserverUpdaterRule<TSelf, T> 
        where TSelf : IObserverUpdaterRule, new()
        where T : Component
    {
        public override void Initialize(GameObject[] targetGameObjects)
        {
            _Targets = new List<T>();
            foreach (var root in targetGameObjects)
            {
                _Targets.AddRange(root.GetComponentsInChildren<T>(true)); 
            }
        }
    }
    
    [Serializable]
    public abstract class ABehaviourEnableObserverUpdaterRule<TSelf, T> : AComponentObserverUpdaterRule<TSelf, T> 
        where TSelf : IObserverUpdaterRule, new()
        where T : Behaviour
    {
        protected override void UpdateTarget(T target, bool prevVisible, bool nextVisible)
        {
            target.enabled = nextVisible;
        }
    }
    
    [Serializable, Preserve]
    public class LightObserverUpdaterRule : ABehaviourEnableObserverUpdaterRule<LightObserverUpdaterRule, Light>
    {
        protected override string Label => "Lights";
    }

    [Serializable, Preserve]
    public class AudioSourceObserverUpdaterRule : ABehaviourEnableObserverUpdaterRule<AudioSourceObserverUpdaterRule, AudioSource>
    {
        protected override string Label => "Audio Sources";
    }
    
    [Serializable, Preserve]
    public class CanvasObserverUpdaterRule : ABehaviourEnableObserverUpdaterRule<CanvasObserverUpdaterRule, Canvas>
    {
        protected override string Label => "Canvases";
    }
    
    [Serializable, Preserve]
    public class RendererObserverUpdaterRule : AComponentObserverUpdaterRule<RendererObserverUpdaterRule, Renderer>
    {
        protected override string Label => "Renderers";
        protected override void UpdateTarget(Renderer target, bool prevVisible, bool nextVisible) => target.enabled = nextVisible;
    }
    
        
    [Serializable, Preserve]
    public class TerrainObserverUpdaterRule : AComponentObserverUpdaterRule<TerrainObserverUpdaterRule, Terrain>
    {
        protected override string Label => "Terrains";
        protected override void UpdateTarget(Terrain target, bool prevVisible, bool nextVisible) => target.enabled = nextVisible;
    }
}
