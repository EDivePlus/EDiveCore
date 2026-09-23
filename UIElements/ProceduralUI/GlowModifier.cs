// Author: Michal Petr
// Created: 22.09.2026

using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UIElements.ProceduralUI
{
    [ExecuteAlways]
    public class GlowModifier : MonoBehaviour, IShapeStyleProvider
    {
        private const string GLOW_OBJECT_NAME = "~Glow";

        [SerializeField]
        [HideInInspector]
        private GlowGraphic _GlowGraphic;

        [SerializeField]
        [HideInInspector]
        private Shader _GlowShader;

        [MinValue(0f)]
        [SerializeField]
        private float _Spread;

        [MinValue(0f)]
        [SerializeField]
        private float _Blur = 10f;

        [MinValue(0.01f)]
        [SerializeField]
        private float _Power = 1f;

        [SerializeField]
        private Vector2 _Offset;

        [SerializeField]
        private float _ExtraSize;

        [PropertySpace]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private GradientFill _Fill = GradientFill.Default;

        [PropertySpace]
        [ShowIf(nameof(HasOwnShape))]
        [SerializeField]
        private CornerRoundness _Roundness;

        [PropertySpace]
        [ShowIf(nameof(HasOwnShape))]
        [IconEnumToggleButtons]
        [SerializeField]
        private FillMode _FillMode;

        [ShowIf(nameof(HasOwnShape))]
        [IconEnumToggleButtons]
        [SerializeField]
        private ShapeStyle _ShapeStyle;

        [ShowIf(nameof(ShowFrameSettings))]
        [MinValue(0f)]
        [SerializeField]
        private float _FrameWidth = 5f;

        [ShowIf(nameof(ShowFrameSettings))]
        [SerializeField]
        private EdgePlacement _FramePlacement;

        private bool _dirty = true;
#if UNITY_EDITOR
        private bool _editorSyncQueued;
#endif

        public bool HasSource => GetComponent<SDFGraphic>();
        private bool HasOwnShape => !HasSource;
        private bool ShowFrameSettings => HasOwnShape && _FillMode == FillMode.NoFill;

        public Vector2 Offset
        {
            get => _Offset;
            set { if (_Offset == value) return; _Offset = value; SetDirty(); }
        }

        public float ExtraSize
        {
            get => _ExtraSize;
            set { if (Mathf.Approximately(_ExtraSize, value)) return; _ExtraSize = value; SetDirty(); }
        }

        public GradientFill Fill
        {
            get => _Fill;
            set { _Fill = value; SetDirty(); }
        }

        public float Spread
        {
            get => _Spread;
            set { value = Mathf.Max(0f, value); if (Mathf.Approximately(_Spread, value)) return; _Spread = value; SetDirty(); }
        }

        public float Blur
        {
            get => _Blur;
            set { value = Mathf.Max(0f, value); if (Mathf.Approximately(_Blur, value)) return; _Blur = value; SetDirty(); }
        }

        public float Power
        {
            get => _Power;
            set { value = Mathf.Max(0.01f, value); if (Mathf.Approximately(_Power, value)) return; _Power = value; SetDirty(); }
        }

        public FillMode FillMode
        {
            get => _FillMode;
            set { if (_FillMode == value) return; _FillMode = value; SetDirty(); }
        }

        public ShapeStyle ShapeStyle
        {
            get => _ShapeStyle;
            set { if (_ShapeStyle == value) return; _ShapeStyle = value; SetDirty(); }
        }

        public float FrameWidth
        {
            get => _FrameWidth;
            set { value = Mathf.Max(0f, value); if (Mathf.Approximately(_FrameWidth, value)) return; _FrameWidth = value; SetDirty(); }
        }

        public EdgePlacement FramePlacement
        {
            get => _FramePlacement;
            set { if (_FramePlacement == value) return; _FramePlacement = value; SetDirty(); }
        }

        public CornerRoundness Roundness
        {
            get => _Roundness;
            set { _Roundness = value; SetDirty(); }
        }

        private void OnEnable()
        {
            EnsureShader();
            Canvas.willRenderCanvases += OnWillRenderCanvases;
            EnsureGlowObject();
            SetDirty();
            SyncNow();
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;

            if (_GlowGraphic && OwnsGlowGraphic(_GlowGraphic))
                _GlowGraphic.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (!_GlowGraphic)
                return;

            if (!OwnsGlowGraphic(_GlowGraphic))
            {
                _GlowGraphic = null;
                return;
            }

            var glowObject = _GlowGraphic.gameObject;
            _GlowGraphic = null;

            if (!glowObject)
                return;

            if (Application.isPlaying)
            {
                Destroy(glowObject);
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () => { if (glowObject) DestroyImmediate(glowObject); };
#endif
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            EnsureShader();
        }

        private void OnValidate()
        {
            EnsureShader();
            SetDirty();
        }
#endif

        private void LateUpdate()
        {
            SyncNow();
        }

        private void OnWillRenderCanvases()
        {
            SyncNow();
        }

        private void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        private void OnTransformParentChanged()
        {
            SetDirty();
            SyncNow();
        }

        private void OnTransformChildrenChanged()
        {
            SetDirty();
        }

        private void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

        public void Refresh()
        {
            SetDirty();
            SyncNow();
        }

        private void EnsureShader()
        {
            if (!_GlowShader)
                _GlowShader = Shader.Find(GlowGraphic.SHADER_NAME);
        }

        private void SetDirty()
        {
            _dirty = true;
#if UNITY_EDITOR
            QueueEditorSync();
#endif
        }

#if UNITY_EDITOR
        private void QueueEditorSync()
        {
            if (Application.isPlaying || _editorSyncQueued)
                return;

            _editorSyncQueued = true;
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.EditorApplication.delayCall += () =>
            {
                _editorSyncQueued = false;
                if (!this)
                    return;

                if (!isActiveAndEnabled)
                {
                    if (_GlowGraphic && OwnsGlowGraphic(_GlowGraphic))
                        _GlowGraphic.gameObject.SetActive(false);
                    return;
                }

                SyncNow();
                UnityEditor.SceneView.RepaintAll();
            };
        }
#endif

        private void SyncNow()
        {
            if (!isActiveAndEnabled)
                return;

            EnsureGlowObject();
            SyncTransform();

            if (_dirty)
            {
                _dirty = false;
                SyncAll();
            }
        }

        private void EnsureGlowObject()
        {
            if (_GlowGraphic && _GlowGraphic.gameObject != gameObject)
            {
                if (OwnsGlowGraphic(_GlowGraphic))
                {
                    _GlowGraphic.Owner = this;
                    _GlowGraphic.gameObject.SetActive(true);
                    return;
                }

                // Shared with another modifier, e.g. after duplication
                _GlowGraphic = null;
            }

            if (_GlowGraphic)
                return;

            var glowObject = new GameObject(GLOW_OBJECT_NAME, typeof(RectTransform))
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave
            };
            glowObject.transform.SetParent(transform.parent, false);
            _GlowGraphic = glowObject.AddComponent<GlowGraphic>();
            _GlowGraphic.SetShader(_GlowShader);
            _GlowGraphic.raycastTarget = false;
            _GlowGraphic.Owner = this;

            SyncTransform();
        }

        private bool OwnsGlowGraphic(GlowGraphic glow)
        {
            if (!glow)
                return false;

            if (glow.Owner == this)
                return true;

            if (glow.Owner)
                return false;

            var rect = GetComponent<SDFGraphic>();
            if (rect && glow.Source == rect)
                return true;

            if (rect || glow.Source || glow.transform.parent != transform.parent)
                return false;

            return glow.transform.GetSiblingIndex() == Mathf.Max(0, transform.GetSiblingIndex() - 1);
        }

        private void SyncTransform()
        {
            if (!_GlowGraphic)
                return;

            var changed = false;
            var glowTransform = _GlowGraphic.transform;

            if (glowTransform.parent != transform.parent)
            {
                glowTransform.SetParent(transform.parent, false);
                changed = true;
            }

            // Glow renders right before this object, i.e. behind it
            var myIndex = transform.GetSiblingIndex();
            var glowIndex = glowTransform.GetSiblingIndex();
            var targetIndex = glowIndex < myIndex ? myIndex - 1 : myIndex;
            if (glowIndex != targetIndex)
            {
                glowTransform.SetSiblingIndex(targetIndex);
                changed = true;
            }

            var glowRect = _GlowGraphic.rectTransform;
            var myRect = transform as RectTransform;
            if (!glowRect || !myRect)
                return;

            var anchoredPosition = myRect.anchoredPosition + _Offset;

            if (glowRect.anchorMin != myRect.anchorMin) { glowRect.anchorMin = myRect.anchorMin; changed = true; }
            if (glowRect.anchorMax != myRect.anchorMax) { glowRect.anchorMax = myRect.anchorMax; changed = true; }
            if (glowRect.pivot != myRect.pivot) { glowRect.pivot = myRect.pivot; changed = true; }
            if (glowRect.sizeDelta != myRect.sizeDelta) { glowRect.sizeDelta = myRect.sizeDelta; changed = true; }
            if (glowRect.anchoredPosition != anchoredPosition) { glowRect.anchoredPosition = anchoredPosition; changed = true; }
            if (glowRect.localRotation != myRect.localRotation) { glowRect.localRotation = myRect.localRotation; changed = true; }
            if (glowRect.localScale != myRect.localScale) { glowRect.localScale = myRect.localScale; changed = true; }

            if (changed)
                _GlowGraphic.SetAllDirty();
        }

        private void SyncAll()
        {
            if (!_GlowGraphic)
                return;

            var rect = GetComponent<SDFGraphic>();
            _GlowGraphic.Source = rect;
            _GlowGraphic.ExtraSize = _ExtraSize;
            _GlowGraphic.Fill = _Fill;
            _GlowGraphic.Spread = _Spread;
            _GlowGraphic.Blur = _Blur;
            _GlowGraphic.Power = _Power;

            if (rect)
                return;

            _GlowGraphic.FillMode = _FillMode;
            _GlowGraphic.ShapeStyle = _ShapeStyle;
            _GlowGraphic.FrameWidth = _FrameWidth;
            _GlowGraphic.FramePlacement = _FramePlacement;
            _GlowGraphic.Roundness = _Roundness;
        }
    }
}
