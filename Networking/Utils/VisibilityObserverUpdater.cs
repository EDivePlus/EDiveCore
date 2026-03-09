// Author: Radim Holub
// Created: 10.10.2025

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using FishNet.Object;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        
        private void Awake()
        {
            _rules = GetRuleTemplates().Select(r => r.GetCopy()).ToList();
            _rules.ForEach(r => r.Initialize(gameObject.scene));
        }
        
        public override void OnStartNetwork()
        {
            NetworkObject.OnHostVisibilityUpdated += OnHostVisibilityUpdated;
        }

        public override void OnStopNetwork()
        {
            NetworkObject.OnHostVisibilityUpdated -= OnHostVisibilityUpdated;
        }

        private void OnHostVisibilityUpdated(bool prevVisible, bool nextVisible)
        {
            _rules.ForEach(r => r?.UpdateVisibility(prevVisible, nextVisible));
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
        void Initialize(Scene scene);
        void UpdateVisibility(bool prevVisible, bool nextVisible);
        IObserverUpdaterRule GetCopy();
    }
    
    [Serializable]
    public abstract class AObserverUpdaterRule<TSelf> : IObserverUpdaterRule
        where TSelf : IObserverUpdaterRule, new()
    {
        protected abstract string Label { get; }
        public abstract void Initialize(Scene scene);
        public abstract void UpdateVisibility(bool prevVisible, bool nextVisible);
        public IObserverUpdaterRule GetCopy() => new TSelf();

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
        public override void Initialize(Scene scene)
        {
            _Targets = GetAllComponentsInScene<T>(scene, true);
        }
        
        private static List<TObj> GetAllComponentsInScene<TObj>(Scene scene, bool includeInactive)
            where TObj : Component
        {
            var results = new List<TObj>();
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                results.AddRange(root.GetComponentsInChildren<TObj>(includeInactive)); 
            }
            return results;
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
}
