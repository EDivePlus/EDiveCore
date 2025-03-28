using System;
using System.Collections.Generic;
using DG.Tweening;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
#endif

namespace EDIVE.Tweening
{
    public class TweenAnimationController : MonoBehaviour, ITweenAnimationPlayer, ITweenTargetProvider, ITweenReferencesHolder
    {
        [UsedImplicitly]
        [SerializeField]
        private string _Description;
        
        [Tooltip("Set to -1 for infinite loops.")]
        [EnhancedFoldoutGroup("Settings", true, SpaceBefore = 4)]
        [SerializeField]
        private int _Loops = -1;

        [EnhancedFoldoutGroup("Settings")]
        [ShowIf(nameof(IsLoopSequence))]
        [SerializeField]
        private LoopType _LoopType;

        [EnhancedFoldoutGroup("Settings")]
        [SerializeField]
        private float _Offset;

        [EnhancedFoldoutGroup("Settings")]
        [SerializeField]
        private float _StartDelay;

        [EnhancedFoldoutGroup("Settings")]
        [SerializeField]
        private float _TimeScale = 1;

        [EnhancedFoldoutGroup("Settings")]
        [Tooltip("If true, this Sequence will ignore the current Time Scale of the application (Time.timeScale).")]
        [SerializeField]
        private bool _IgnoreUnityTimeScale;

        [EnhancedFoldoutGroup("Settings")]
        [Tooltip("DOTween's update type for THIS Sequence.\n\nNormal: updates on Update() calls.\nFixed: updates on FixedUpdate() calls.\nLate: updates on LateUpdate() calls.")]
        [SerializeField]
        private UpdateType _UpdateTime = UpdateType.Normal;

        [EnhancedFoldoutGroup("Settings")]
        [ShowIf(nameof(_Loops), -1)]
        [SerializeField]
        private bool _SyncWithGlobalTime;
        
        [EnhancedFoldoutGroup("Callbacks", SpaceAfter = 4)]
        [SerializeField]
        private PlayAction _OnAwake;
        
        [EnhancedFoldoutGroup("Callbacks")]
        [SerializeField]
        private PlayAction _OnStart;

        [EnhancedFoldoutGroup("Callbacks")]
        [SerializeField]
        private PlayAction _OnEnable = PlayAction.PlayOrRestart;
        
        [EnhancedFoldoutGroup("Callbacks")]
        [SerializeField]
        private StopAction _OnDisable = StopAction.RewindAndPause;

        [PropertySpace]
        [PropertyOrder(20)]
        [SerializeField]
        private TweenObjectReferenceCollection _References;

        [PropertyOrder(20)]
        [SerializeField]
        private TweenSequence _Sequence = new ();

        private bool IsLoopSequence => _Loops != 0;
        public bool IsPlaying => _tweener != null && _tweener.IsPlaying();
        public bool IsPaused => _tweener != null && _tweener.IsInitialized() && !_tweener.IsPlaying() && !_tweener.IsComplete();
        public bool IsInitialized => _tweener != null && _tweener.IsInitialized();

        private Sequence _tweener;

        public virtual Sequence CreateSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.SetUpdate(_UpdateTime, _IgnoreUnityTimeScale);
            _References.AssignTempReferences();
            _Sequence?.PopulateSequence(sequence);
            _References.ClearTempReferences();
            sequence.SetLoops(_Loops, _LoopType);
            sequence.timeScale = _TimeScale;
            if (_Offset > 0)
            {
                sequence.Goto(_Offset);
            }

            if (_StartDelay > 0)
            {
                sequence.SetDelay(_StartDelay, false);
            }
            return sequence;
        }

        public Tween Play()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return EditorPreview();
            }
#endif
            return PlayInternal();
        }

        public void Complete()
        {
            _tweener?.Complete(true);
        }

        public void Kill(bool complete = false)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                KillEditorPreview(complete);
                return;
            }
#endif
            KillInternal(complete);
        }

        private Tween PlayInternal()
        {
            Kill();
            _tweener = CreateSequence();
            _tweener.Play();
            
            SyncWithGlobalTime();
            
            return _tweener;
        }

        private void KillInternal(bool complete = false)
        {
            // Complete called manually to invoke callbacks!
            if (complete) _tweener?.Complete(true);
            _tweener?.Kill();
            _tweener = null;
        }

        public void Resume()
        {
            _tweener?.Play();
            SyncWithGlobalTime();
        }

        public void Pause()
        {
            _tweener?.Pause();
        }

        public void Rewind()
        {
            _tweener?.Rewind();
        }
        
        private void SyncWithGlobalTime()
        {
            if (!_SyncWithGlobalTime) return;
            var time = Time.time;
            var timeOffset = time % _tweener.Duration();
            _tweener.Goto(timeOffset, true);
        }
        
        private void Awake()
        {
            ExecutePlayAction(_OnAwake);
        }
        
        private void Start()
        {
            ExecutePlayAction(_OnStart);
        }

        private void OnEnable()
        {
            ExecutePlayAction(_OnEnable);
        }

        private void OnDisable()
        {
            ExecuteStopAction(_OnDisable);
        }
        
        private void OnDestroy()
        {
            Kill();
        }

        private void ExecutePlayAction(PlayAction action)
        {
            switch (action)
            {
                case PlayAction.None:
                    break;
                
                case PlayAction.PlayOrResume:
                    if(_tweener == null) Play();
                    else Resume();
                    break;
                
                case PlayAction.PlayOrRestart:
                    Play();
                    break;
                
                default: 
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void ExecuteStopAction(StopAction action)
        {
            switch (action)
            {
                case StopAction.None: 
                    break;
                
                case StopAction.Rewind: 
                    Rewind();
                    break;
                
                case StopAction.Pause: 
                    Pause();
                    break;
                
                case StopAction.RewindAndPause:
                    Rewind();
                    Pause();
                    break;
                
                case StopAction.Kill: 
                    Kill();
                    break;
                
                default: 
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        public IDictionary<TweenObjectReference, Object> GetReferencesDictionary()
        {
            return _References?.GetReferencesDictionary();
        }

#if UNITY_EDITOR
        public void PopulateReferences(HashSet<TweenObjectReference> references)
        {
            _Sequence.PopulateReferences(references);
        }

        public void PopulateTargets(TweenTargetCollection targets)
        {
            _Sequence.PopulateTargets(targets);
        }

        [OnInspectorGUI]
        private void Controls()
        {
            TweenEditorUtils.DrawControls(this);
        }

        public Tween CreatePreview() => CreateSequence();

        private Tween EditorPreview()
        {
            Rewind();
            KillEditorPreview();
            var tweener = PlayInternal();
            DOTweenEditorPreview.PrepareTweenForPreview(_tweener);
            DOTweenEditorPreview.Start();
            return tweener;
        }

        private void KillEditorPreview(bool complete = false)
        {
            // Complete called manually to invoke callbacks!
            if (complete) _tweener?.Complete(true);
            _tweener?.Kill();
            DOTweenEditorPreview.RemoveTweenFromPreview(_tweener);
            _tweener = null;
        }
#endif
        
        private enum PlayAction
        {
            None            = 0,
            PlayOrResume    = 1,
            PlayOrRestart   = 2,
        }
        
        private enum StopAction
        {
            None            = 0,
            Rewind          = 1,
            Pause           = 2,
            RewindAndPause  = 3,
            Kill            = 4,
        }
    }
}
