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
        private const string DEFAULT_SHADER = "EDIVE/Vignette";

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
        private Material _generatedMaterial;
        private Texture2D _rampTexture;
        private Gradient _rampSource;

        private readonly List<VignetteHandle> _requests = new();
        private VignetteHandle _winner;
        private VignetteHandle _fadeTarget;
        private readonly VignetteSettings _displayed = new();
        private readonly VignetteSettings _from = new();
        private readonly VignetteSettings _off = new();
        private float _progress;
        private bool _transitioning;
        private bool _running;
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
            ReevaluateWinner();
            return handle;
        }

        internal void Release(VignetteHandle handle)
        {
            if (_requests.Remove(handle))
                ReevaluateWinner();
        }

        // Runs on every change that can pick a different winner: a request, a release, a priority or settings edit.
        internal void ReevaluateWinner()
        {
            if (!_running || IsUninterruptibleShowRunning())
                return;

            var winner = SelectWinner();
            if (winner == _winner)
                return;

            BeginTransition(winner);
            _winner = winner;
        }

        // Nothing may interrupt such a show, so the pending change waits for FinishTransition to reevaluate.
        private bool IsUninterruptibleShowRunning() =>
            _transitioning && _winner != null && _fadeTarget == _winner && _winner.Transition.UninterruptibleShow;

        private void OnEnable()
        {
            foreach (var provider in _Providers)
                provider?.Initialize(this);

            _winner = SelectWinner();
            _displayed.CopyFrom(_winner == null ? GetOffState(_DefaultSettings) : ResolveSettings(_winner));
            ApplyToMaterial(_displayed);
            _running = true;
        }

        private void OnDisable()
        {
            _running = false;
            foreach (var provider in _Providers)
                provider?.Deinitialize();

            _tween?.Kill();
            _tween = null;
            _fadeTarget = null;
            _transitioning = false;
            DestroyRamp();
        }

        private void OnDestroy()
        {
            if (_generatedMaterial == null)
                return;

            if (Application.isPlaying)
                Destroy(_generatedMaterial);
            else
                DestroyImmediate(_generatedMaterial);
            _generatedMaterial = null;
        }

        private void Update()
        {
            foreach (var provider in _Providers)
                provider?.Tick();

            if (_transitioning || _winner == null)
                return;

            var settings = ResolveSettings(_winner);
            if (_displayed.Matches(settings))
                return;

            _displayed.CopyFrom(settings);
            ApplyToMaterial(_displayed);
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
                // Marks the show as running before the fade callback does, so it cannot be interrupted within this frame.
                _fadeTarget = newWinner;
                AppendFadeTo(sequence, newWinner, transition.ShowDuration, transition.ShowEase);
            }
            else
            {
                var transition = _winner?.Transition ?? _DefaultTransition;

                if (transition.HideDelay > 0f)
                    sequence.AppendInterval(transition.HideDelay);

                AppendFadeTo(sequence, null, transition.HideDuration, transition.HideEase);
            }

            sequence.OnComplete(FinishTransition);
            _tween = sequence;
        }

        private void FinishTransition()
        {
            _transitioning = false;
            _tween = null;
            ReevaluateWinner();
        }
        
        private void AppendFadeTo(Sequence sequence, VignetteHandle target, float duration, Ease ease)
        {
            sequence.AppendCallback(() =>
            {
                _from.CopyFrom(_displayed);
                _fadeTarget = target;
                _progress = 0f;
            });

            if (duration > 0f)
                sequence.Append(DOTween.To(() => _progress, SetFadeProgress, 1f, duration).SetEase(ease));
            else
                sequence.AppendCallback(() => SetFadeProgress(1f));
        }

        private void SetFadeProgress(float progress)
        {
            _progress = progress;
            var target = _fadeTarget == null ? GetOffState(_from) : ResolveSettings(_fadeTarget);
            VignetteSettings.Lerp(_from, target, progress, _displayed);
            ApplyToMaterial(_displayed);
        }

        private VignetteSettings ResolveSettings(VignetteHandle handle) => handle.Settings ?? _DefaultSettings;

        // Fading out only opens the aperture and drops the alpha, so color, gradient and feathering never shift.
        private VignetteSettings GetOffState(VignetteSettings source)
        {
            _off.CopyFrom(source);
            _off.ApertureSize = 1f;
            _off.Alpha = 0f;
            return _off;
        }

        private void ApplyToMaterial(VignetteSettings settings)
        {
            if (!TrySetUpMaterial())
                return;

            _propertyBlock ??= new MaterialPropertyBlock();
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

                _generatedMaterial = new Material(shader)
                {
                    name = "Vignette"
                };
                _meshRenderer.sharedMaterial = _generatedMaterial;
            }

            // Warn again if the setup breaks after it once worked.
            _setupWarned = false;
            return true;
        }

        private Texture2D GetRampTexture(Gradient gradient)
        {
            // Rebake when the source gradient changes, and always in edit mode (in-place edits keep the same reference).
            if (_rampTexture != null && ReferenceEquals(_rampSource, gradient) && Application.isPlaying)
                return _rampTexture;

            // A single-color gradient only needs one texel; a varying one gets the full ramp.
            var width = IsConstant(gradient) ? 1 : RAMP_WIDTH;

            if (_rampTexture == null)
            {
                _rampTexture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
                {
                    name = "VignetteRamp",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }
            else if (_rampTexture.width != width)
            {
                // Resized in place, destroying it here would be called from OnValidate.
                _rampTexture.Reinitialize(width, 1);
            }

            for (var x = 0; x < width; x++)
                _rampTexture.SetPixel(x, 0, gradient.Evaluate(width == 1 ? 0f : x / (width - 1f)));
            _rampTexture.Apply(false);
            _rampSource = gradient;

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
