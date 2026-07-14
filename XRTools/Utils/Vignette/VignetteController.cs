// Author: František Holubec
// Created: 08.07.2026

using System.Collections.Generic;
using DG.Tweening;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.XRTools.Utils.Vignette
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class VignetteController : MonoBehaviour
    {
        private const string DEFAULT_SHADER = "VR/TunnelingVignette";

        [SerializeField]
        [EnhancedInlineProperty]
        private VignetteSettings _DefaultSettings = VignetteSettings.Default;

        [SerializeField]
        [EnhancedInlineProperty]
        private VignetteTransition _DefaultTransition = VignetteTransition.Default;

        [SerializeField]
        private VignetteTiebreak _Tiebreak = VignetteTiebreak.StrongestEffect;

        [SerializeReference]
        private List<AVignetteProvider> _Providers = new();
        
        [SerializeField]
        private bool _PreviewInEditor;

        [SerializeField]
        [EnhancedInlineProperty]
        [ShowIf(nameof(_PreviewInEditor))]
        private VignetteSettings _PreviewSettings = VignetteSettings.Default;

        private const int RAMP_WIDTH = 256;

        private static readonly int APERTURE_SIZE_ID = Shader.PropertyToID("_ApertureSize");
        private static readonly int FEATHERING_EFFECT_ID = Shader.PropertyToID("_FeatheringEffect");
        private static readonly int ALPHA_ID = Shader.PropertyToID("_Alpha");
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");
        private static readonly int GRADIENT_ID = Shader.PropertyToID("_Gradient");
        private static readonly int VERTICAL_OFFSET_ID = Shader.PropertyToID("_VerticalOffset");

        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private MaterialPropertyBlock _propertyBlock;
        private Texture2D _rampTexture;
        private Gradient _rampSource;

        private readonly List<VignetteHandle> _requests = new();
        private VignetteHandle _winner;
        private VignetteHandle _target;
        private readonly VignetteSettings _displayed = new();
        private readonly VignetteSettings _from = new();
        private readonly VignetteSettings _none = new();
        private float _progress;
        private bool _transitioning;
        private Tween _tween;
        private int _orderCounter;
        private bool _setupWarned;

        public IReadOnlyList<VignetteHandle> Requests => _requests;

        public VignetteHandle Request(int priority = 0) => Request(null, priority, _DefaultTransition);

        public VignetteHandle Request(VignetteSettings settings, int priority = 0) => Request(settings, priority, _DefaultTransition);

        public VignetteHandle Request(VignetteSettings settings, int priority, VignetteTransition transition)
        {
            var handle = new VignetteHandle(this, settings, priority, transition, _orderCounter++);
            _requests.Add(handle);
            return handle;
        }

        internal void Release(VignetteHandle handle) => _requests.Remove(handle);

        private void OnEnable()
        {
            foreach (var provider in _Providers)
                provider?.Initialize(this);

            _winner = SelectWinner();
            _displayed.CopyFrom(_winner == null ? _none : ResolveSettings(_winner));
            ApplyToMaterial(_displayed);
        }

        private void OnDisable()
        {
            foreach (var provider in _Providers)
                provider?.Deinitialize();

            _tween?.Kill();
            _tween = null;
            _target = null;
            _transitioning = false;
            DestroyRamp();
        }

        private void Update()
        {
            foreach (var provider in _Providers)
                provider?.Tick();

            var winner = SelectWinner();
            if (winner != _winner)
            {
                BeginTransition(winner);
                _winner = winner;
            }
            
            if (!_transitioning && _winner != null)
            {
                _displayed.CopyFrom(ResolveSettings(_winner));
                ApplyToMaterial(_displayed);
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying || !gameObject.activeInHierarchy)
                return;

            if (_PreviewInEditor)
                ApplyToMaterial(_PreviewSettings);
            else
                ReleasePropertyBlock();
        }
        
        private void ReleasePropertyBlock()
        {
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer != null)
                _meshRenderer.SetPropertyBlock(null);
        }

        private VignetteHandle SelectWinner()
        {
            VignetteHandle best = null;
            foreach (var request in _requests)
            {
                if (best == null || IsBetter(request, best))
                    best = request;
            }
            return best;
        }

        private bool IsBetter(VignetteHandle a, VignetteHandle b)
        {
            if (a.Priority != b.Priority)
                return a.Priority > b.Priority;

            return _Tiebreak switch
            {
                VignetteTiebreak.StrongestEffect => ResolveSettings(a).ApertureSize < ResolveSettings(b).ApertureSize,
                VignetteTiebreak.MostRecent => a.Order > b.Order,
                VignetteTiebreak.Oldest => a.Order < b.Order,
                _ => false,
            };
        }

        private void BeginTransition(VignetteHandle newWinner)
        {
            _tween?.Kill();
            _transitioning = true;
            var sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

            if (newWinner != null)
            {
                var transition = newWinner.Transition;
                AppendLeg(sequence, newWinner, transition.ShowDuration, transition.ShowEase);
            }
            else
            {
                var transition = _winner?.Transition ?? _DefaultTransition;

                if (transition.CompleteShowBeforeHide && _winner != null && !Settled(_winner))
                    AppendLeg(sequence, _winner, transition.ShowDuration * RemainingShowFraction(_winner), transition.ShowEase);

                if (transition.HideDelay > 0f)
                    sequence.AppendInterval(transition.HideDelay);

                AppendLeg(sequence, null, transition.HideDuration, transition.HideEase);
            }

            sequence.OnUpdate(ApplyTransitionFrame);
            sequence.OnComplete(() =>
            {
                _transitioning = false;
                ApplyTransitionFrame();
            });
            _tween = sequence;
        }
        
        private void AppendLeg(Sequence sequence, VignetteHandle target, float duration, Ease ease)
        {
            sequence.AppendCallback(() =>
            {
                _from.CopyFrom(_displayed);
                _target = target;
                _progress = 0f;
            });

            if (duration > 0f)
                sequence.Append(DOTween.To(() => _progress, x => _progress = x, 1f, duration).SetEase(ease));
            else
                sequence.AppendCallback(() => _progress = 1f);
        }

        private void ApplyTransitionFrame()
        {
            var target = _target == null ? _none : ResolveSettings(_target);
            VignetteSettings.Lerp(_from, target, _progress, _displayed);
            ApplyToMaterial(_displayed);
        }

        private VignetteSettings ResolveSettings(VignetteHandle handle) => handle.Settings ?? _DefaultSettings;

        private bool Settled(VignetteHandle handle) =>
            Mathf.Approximately(_displayed.ApertureSize, ResolveSettings(handle).ApertureSize);

        private float RemainingShowFraction(VignetteHandle handle)
        {
            var apertureSize = ResolveSettings(handle).ApertureSize;
            var full = 1f - apertureSize;
            if (full <= 0.0001f)
                return 0f;
            return Mathf.Clamp01((_displayed.ApertureSize - apertureSize) / full);
        }

        private void ApplyToMaterial(VignetteSettings settings)
        {
            if (!TrySetUpMaterial())
                return;

            _propertyBlock ??= new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(APERTURE_SIZE_ID, settings.ApertureSize);
            _propertyBlock.SetFloat(FEATHERING_EFFECT_ID, settings.Feathering);
            _propertyBlock.SetFloat(ALPHA_ID, settings.Alpha);
            _propertyBlock.SetColor(COLOR_ID, settings.Color);
            _propertyBlock.SetFloat(VERTICAL_OFFSET_ID, settings.VerticalPosition);
            _propertyBlock.SetTexture(GRADIENT_ID, GetRampTexture(settings.Gradient));
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        private bool TrySetUpMaterial()
        {
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();

            if (_meshRenderer == null || _meshFilter == null)
                return false;

            if (_meshFilter.sharedMesh == null)
            {
                WarnOnce("Mesh is not set");
                return false;
            }

            if (_meshRenderer.sharedMaterial == null)
            {
                if (!Application.isPlaying)
                {
                    WarnOnce("Material is not set");
                    return false;
                }

                var shader = Shader.Find(DEFAULT_SHADER);
                if (shader == null)
                {
                    WarnOnce($"Material is not set and the shader {DEFAULT_SHADER} cannot be found");
                    return false;
                }

                _meshRenderer.sharedMaterial = new Material(shader)
                {
                    name = "TunnelingVignette"
                };
            }

            return true;
        }

        private Texture2D GetRampTexture(Gradient gradient)
        {
            // A single-color gradient only needs one texel; a varying one gets the full ramp.
            var width = IsConstant(gradient) ? 1 : RAMP_WIDTH;

            if (_rampTexture != null && _rampTexture.width != width)
                DestroyRamp();

            if (_rampTexture == null)
            {
                _rampTexture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
                {
                    name = "VignetteRamp",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                _rampSource = null;
            }

            // Rebake when the source gradient changes, and always in edit mode (in-place edits keep the same reference).
            if (!ReferenceEquals(_rampSource, gradient) || !Application.isPlaying)
            {
                for (var x = 0; x < width; x++)
                    _rampTexture.SetPixel(x, 0, gradient.Evaluate(width == 1 ? 0f : x / (width - 1f)));
                _rampTexture.Apply(false);
                _rampSource = gradient;
            }

            return _rampTexture;
        }

        private static bool IsConstant(Gradient gradient)
        {
            var color = gradient.Evaluate(0f);
            return Approximately(gradient.Evaluate(0.5f), color) && Approximately(gradient.Evaluate(1f), color);
        }

        private static bool Approximately(Color a, Color b) =>
            Mathf.Approximately(a.r, b.r) && Mathf.Approximately(a.g, b.g) &&
            Mathf.Approximately(a.b, b.b) && Mathf.Approximately(a.a, b.a);

        private void DestroyRamp()
        {
            if (_rampTexture == null)
                return;

            if (Application.isPlaying)
                Destroy(_rampTexture);
            else
                DestroyImmediate(_rampTexture);
            _rampTexture = null;
            _rampSource = null;
        }

        private void WarnOnce(string message)
        {
            if (_setupWarned)
                return;
            _setupWarned = true;
            Debug.LogWarning(message, this);
        }
    }
}
