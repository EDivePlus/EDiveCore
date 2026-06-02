// Author: František Holubec
// Created: 02.06.2026

using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EDIVE.UIElements.RecyclableScroller
{
    /// <summary>
    /// A windowed list virtualizer for Unity's ScrollRect: only the items overlapping the
    /// viewport (plus an optional look-ahead margin) are instantiated and recycled; everything
    /// outside is collapsed into two spacers so the content
    /// keeps its full scrollable size. This is the standard recycling approach shared by list
    /// virtualizers such as Android's RecyclerView and iOS's UICollectionView.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class RecyclableScroller : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
    {
        public enum ScrollDirectionEnum
        {
            Vertical,
            Horizontal
        }

        public enum TweenInterruptModeEnum
        {
            Never,
            OnDrag,
            OnPointerDown
        }
        
        public class JumpOptions
        {
            public float ScrollerOffset { get; set; } // viewport anchor (0..1) the target lands on
            public float ItemOffset { get; set; } // point within the item (0..1) aligned to that anchor
            public Ease Ease { get; set; } = Ease.Linear;
            public float TweenTime { get; set; } // seconds; 0 = jump instantly
            public float? StartingVelocity { get; set; } // null = use the current scroll velocity
            public Action OnCompleted { get; set; }
        }

        [FormerlySerializedAs("scrollDirection")]
        [SerializeField]
        private ScrollDirectionEnum _ScrollDirection;

        [Tooltip("Pixels between items, starting after the first one.")]
        [FormerlySerializedAs("spacing")]
        [SerializeField]
        private float _Spacing;

        [Tooltip("Padding inside the scroller: top, bottom, left, right.")]
        [FormerlySerializedAs("padding")]
        [SerializeField]
        private RectOffset _Padding;

        [Tooltip("Maximum scroll speed. Zero means no cap.")]
        [FormerlySerializedAs("maxVelocity")]
        [SerializeField]
        private float _MaxVelocity;

        [Tooltip("When user input should interrupt a running snap/jump tween.")]
        [SerializeField]
        private TweenInterruptModeEnum _InterruptTweenMode;

        [Tooltip("Whether snapping is turned on.")]
        [FormerlySerializedAs("snapping")]
        [SerializeField]
        private bool _Snapping;

        [Tooltip("Scroll speed (pixels/second) the fling must decay to before a snap is initiated. 0 disables mid-fling snapping.")]
        [FormerlySerializedAs("snapVelocityThreshold")]
        [ShowIfGroup("Snap", Condition = nameof(_Snapping))]
        [SerializeField]
        private float _SnapVelocityThreshold = 50f;

        [Tooltip("Anchor within the viewport (0..1) that items snap to. 0 = start, 0.5 = center, 1 = end.")]
        [FormerlySerializedAs("_SnapJumpToOffset")]
        [FormerlySerializedAs("snapJumpToOffset")]
        [ShowIfGroup("Snap")]
        [SerializeField]
        private float _SnapAlignment;

        [Tooltip("Easing used to interpolate to the snap location.")]
        [FormerlySerializedAs("snapTweenType")]
        [ShowIfGroup("Snap")]
        [SerializeField]
        private Ease _SnapTweenType = Ease.InOutQuad;

        [Tooltip("Time to interpolate to the snap location. Zero snaps immediately.")]
        [FormerlySerializedAs("snapTweenTime")]
        [ShowIfGroup("Snap")]
        [SerializeField]
        private float _SnapTweenTime = 0.2f;

        [Tooltip("Keep snapping while the scroller is dragged.")]
        [FormerlySerializedAs("snapWhileDragging")]
        [ShowIfGroup("Snap")]
        [SerializeField]
        private bool _SnapWhileDragging;

        public ScrollDirectionEnum ScrollDirection { get => _ScrollDirection; set => _ScrollDirection = value; }
        public float Spacing { get => _Spacing; set => _Spacing = value; }
        public RectOffset Padding { get => _Padding; set => _Padding = value; }
        public float MaxVelocity { get => _MaxVelocity; set => _MaxVelocity = value; }
        public bool Snapping { get => _Snapping; set => _Snapping = value; }
        public float SnapVelocityThreshold { get => _SnapVelocityThreshold; set => _SnapVelocityThreshold = value; }
        public float SnapAlignment { get => _SnapAlignment; set => _SnapAlignment = value; }
        public Ease SnapTweenType { get => _SnapTweenType; set => _SnapTweenType = value; }
        public float SnapTweenTime { get => _SnapTweenTime; set => _SnapTweenTime = value; }
        public bool SnapWhileDragging { get => _SnapWhileDragging; set => _SnapWhileDragging = value; }
        public TweenInterruptModeEnum InterruptTweenMode { get => _InterruptTweenMode; set => _InterruptTweenMode = value; }

        public bool TweenPaused
        {
            get => _tweenPaused;
            set
            {
                _tweenPaused = value;
                if (_scrollTween == null) return;
                if (value)
                    _scrollTween.Pause();
                else
                    _scrollTween.Play();
            }
        }

        public RecyclableScrollerSource Source
        {
            get => _source;
            set
            {
                _source = value;
                _reloadData = true;
            }
        }

        public float ScrollPosition
        {
            get => _scrollPosition;
            set
            {
                value = Mathf.Clamp(value, 0, ScrollableLength);

                if (!Mathf.Approximately(_scrollPosition, value))
                {
                    _scrollPosition = value;
                    var length = ScrollableLength;
                    SetNormalizedAxisPosition(length > 0 ? _scrollPosition / length : 0f);
                }
            }
        }

        public float ScrollableLength => Mathf.Max(AxisSize(Container.rect) - AxisSize(_scrollRectTransform.rect), 0);

        public float NormalizedScrollPosition
        {
            get
            {
                var length = ScrollableLength;
                return _scrollPosition <= 0 || length <= 0 ? 0 : _scrollPosition / length;
            }
        }

        public Vector2 Velocity
        {
            get => ScrollRect.velocity;
            set => ScrollRect.velocity = value;
        }

        public float AxisVelocity
        {
            get => AxisOf(ScrollRect.velocity);
            set => ScrollRect.velocity = AxisVector(value);
        }

        public bool IsScrolling { get; private set; }
        public bool IsTweening { get; private set; }
        public bool IsDragging { get; private set; }

        public int StartItemIndex { get; private set; }
        public int EndItemIndex { get; private set; }
        public int StartDataIndex => StartItemIndex;
        public int EndDataIndex => EndItemIndex;
        public int ActiveItemCount => _activeItemViews.Count;
        public int ItemCount => _source?.GetItemCount?.Invoke() ?? 0;
        
        private bool IsVertical => _ScrollDirection == ScrollDirectionEnum.Vertical;
        private float LeadingPadding => IsVertical ? _Padding.top : _Padding.left;
        private float TrailingPadding => IsVertical ? _Padding.bottom : _Padding.right;
        
        public ScrollRect ScrollRect { get; private set; }

        public float ViewportLength => AxisSize(_scrollRectTransform.rect);

        public LayoutElement LeadingSpacer { get; private set; }
        public LayoutElement TrailingSpacer { get; private set; }
        public RectTransform Container { get; private set; }

        public float LookAheadBefore
        {
            get => _lookAheadBefore;
            set => _lookAheadBefore = Mathf.Abs(value);
        }

        public float LookAheadAfter
        {
            get => _lookAheadAfter;
            set => _lookAheadAfter = Mathf.Abs(value);
        }

        public event Action<RecyclableScrollerItemView, bool> ItemVisibilityChanged;
        public event Action<RecyclableScrollerItemView> ItemWillRecycle;
        public event Action<RecyclableScroller, RecyclableScrollerItemView, bool> ItemBound; 
        public event Action<RecyclableScroller, Vector2, float> ScrollerScrolled;
        public event Action<RecyclableScroller, int, int, RecyclableScrollerItemView> ScrollerSnapped;
        public event Action<RecyclableScroller, bool> ScrollerScrollingChanged;
        public event Action<RecyclableScroller, bool> ScrollerTweeningChanged;

        public event Action<int, int> DataIndexRangeChanged;
        
        private RectTransform _scrollRectTransform;
        private HorizontalOrVerticalLayoutGroup _layoutGroup;
        private ScrollRect.MovementType _scrollRectMovementType;

        private RecyclableScrollerSource _source;

        private bool _initialized;
        private bool _reloadData;
        private bool _updateSpacing;

        private float _scrollPosition;
        private float _lookAheadBefore;
        private float _lookAheadAfter;
        
        private readonly List<RecyclableScrollerItemView> _recycledItemViews = new();
        private readonly List<RecyclableScrollerItemView> _activeItemViews = new();
        private readonly List<float> _itemSizeArray = new();
        private readonly List<float> _itemOffsetArray = new();

        private int _snapItemIndex;
        private int _snapDataIndex;
        private bool _snapJumping;
        
        private int _dragFingerCount;
        private bool _snapBeforeDrag;
        
        private Tween _scrollTween;
        private bool _tweenPaused;
        private bool _drivenScroll;
        private bool _savedInertia;

        private enum ItemEdge
        {
            Leading,
            Trailing
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;

            ScrollRect = GetComponent<ScrollRect>();
            _scrollRectTransform = ScrollRect.GetComponent<RectTransform>();
            _scrollRectMovementType = ScrollRect.movementType;
            
            Container = ScrollRect.content;
            if (Container == null || Container == _scrollRectTransform)
            {
                var container = new GameObject("Content", typeof(RectTransform));
                container.transform.SetParent(_scrollRectTransform, false);
                Container = (RectTransform) container.transform;
            }
            
            if (ScrollRect.viewport == null || ScrollRect.viewport == Container)
                ScrollRect.viewport = _scrollRectTransform;

            ScrollRect.content = Container;
            
            _layoutGroup = Container.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (_layoutGroup == null)
                _layoutGroup = IsVertical
                    ? Container.gameObject.AddComponent<VerticalLayoutGroup>()
                    : Container.gameObject.AddComponent<HorizontalLayoutGroup>();
            _layoutGroup.spacing = _Spacing;
            _layoutGroup.padding = _Padding;
            _layoutGroup.childAlignment = TextAnchor.UpperLeft;
            _layoutGroup.childControlWidth = true;
            _layoutGroup.childControlHeight = true;
            _layoutGroup.childForceExpandWidth = true;
            _layoutGroup.childForceExpandHeight = true;
            
            if (IsVertical)
            {
                Container.anchorMin = new Vector2(0, 1);
                Container.anchorMax = Vector2.one;
                Container.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                Container.anchorMin = Vector2.zero;
                Container.anchorMax = new Vector2(0, 1f);
                Container.pivot = new Vector2(0, 0.5f);
            }
            
            Container.localPosition = Vector3.zero;
            Container.localRotation = Quaternion.identity;
            Container.localScale = Vector3.one;
            Container.offsetMax = Vector2.zero;
            Container.offsetMin = Vector2.zero;
            
            ScrollRect.horizontal = !IsVertical;
            ScrollRect.vertical = IsVertical;

            // drop any design-time children left under the content; the scroller owns this
            // hierarchy (spacers + pooled items) and stray objects break the layout and the
            // spacer sibling-index math
            for (var i = Container.childCount - 1; i >= 0; i--)
                DestroyInternal(Container.GetChild(i).gameObject);

            LeadingSpacer = CreateSpacer("Leading Spacer");
            TrailingSpacer = CreateSpacer("Trailing Spacer");

            _initialized = true;
        }

        private LayoutElement CreateSpacer(string spacerName)
        {
            var go = new GameObject(spacerName, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(Container, false);
            return go.GetComponent<LayoutElement>();
        }

        private void OnEnable()
        {
            ScrollRect.onValueChanged.AddListener(ScrollRect_OnValueChanged);
        }

        private void OnDisable()
        {
            ScrollRect.onValueChanged.RemoveListener(ScrollRect_OnValueChanged);
            
            _scrollTween?.Kill();
            _scrollTween = null;

            _snapJumping = false;
            if (IsTweening)
            {
                IsTweening = false;
                ScrollerTweeningChanged?.Invoke(this, false);
            }
        }

        private void Update()
        {
            if (_updateSpacing)
            {
                UpdateSpacing(_Spacing);
                _reloadData = false;
            }

            if (_reloadData)
                ReloadData();

            // raise the scrolling changed event when scrolling starts or stops
            if (!Mathf.Approximately(AxisVelocity, 0f) && !IsScrolling)
            {
                IsScrolling = true;
                ScrollerScrollingChanged?.Invoke(this, true);
            }
            else if (Mathf.Approximately(AxisVelocity, 0f) && IsScrolling)
            {
                IsScrolling = false;
                ScrollerScrollingChanged?.Invoke(this, false);
            }
        }

        private void LateUpdate()
        {
            // cap the velocity if a maximum is set (but not while a tween drives the position)
            if (_MaxVelocity > 0 && !_drivenScroll)
            {
                var speed = AxisVelocity;
                AxisVelocity = Mathf.Clamp(Mathf.Abs(speed), 0, _MaxVelocity) * Mathf.Sign(speed);
            }
        }

        private void OnValidate()
        {
            // spacing can't be changed during OnValidate, so defer it to Update
            if (_initialized && !Mathf.Approximately(_Spacing, _layoutGroup.spacing))
                _updateSpacing = true;
        }

        public T GetItemView<T>(T itemPrefab) where T : RecyclableScrollerItemView
        {
            var itemView = GetRecycledItemView(itemPrefab);
            var wasInstantiated = itemView == null;
            if (wasInstantiated)
            {
                // no recyclable item found, create a new one and attach it to the container
                var go = Instantiate(itemPrefab.gameObject);
                itemView = go.GetComponent<RecyclableScrollerItemView>();
                itemView.SourcePrefab = itemPrefab;
                itemView.transform.SetParent(Container);
                itemView.transform.localPosition = Vector3.zero;
                itemView.transform.localRotation = Quaternion.identity;
            }
            else
            {
                itemView.gameObject.SetActive(true);
            }

            ItemBound?.Invoke(this, itemView, wasInstantiated);

            return (T) itemView;
        }

        public void ReloadData(float scrollPositionFactor = 0)
        {
            _reloadData = false;

            // make sure we are set up even if this is called before Awake
            Initialize();

            RecycleAllItems();

            if (_source != null)
                Resize(false);

            if (ScrollRect == null || _scrollRectTransform == null || Container == null)
            {
                _scrollPosition = 0f;
                return;
            }

            _scrollPosition = Mathf.Clamp(scrollPositionFactor * ScrollableLength, 0, ScrollableLength);
            SetNormalizedAxisPosition(scrollPositionFactor);

            RefreshActive();
        }

        // Reloads the data, starting at an absolute scroll position in pixels instead of a 0..1 factor.
        public void ReloadDataWithScrollPosition(float scrollPosition = 0)
        {
            Initialize();
            var factor = ScrollableLength > 0 ? Mathf.InverseLerp(0, ScrollableLength, scrollPosition) : 0f;
            ReloadData(factor);
        }

        public void RefreshActiveItems()
        {
            foreach (var activeItemView in _activeItemViews)
                activeItemView.RefreshView();
        }

        public void ClearAll()
        {
            ClearActive();
            ClearRecycled();
        }

        public void ClearActive()
        {
            foreach (var activeItemView in _activeItemViews)
                DestroyInternal(activeItemView.gameObject);

            _activeItemViews.Clear();
        }

        public void ClearRecycled()
        {
            foreach (var recycledItemView in _recycledItemViews)
                DestroyInternal(recycledItemView.gameObject);

            _recycledItemViews.Clear();
        }

        public void SetScrollPositionImmediately(float scrollPosition)
        {
            ScrollPosition = scrollPosition;
            RefreshActive();
        }

        public Tween JumpToDataIndex(int dataIndex, JumpOptions options = null)
        {
            options ??= new JumpOptions();

            var itemOffsetPosition = 0f;

            if (!Mathf.Approximately(options.ItemOffset, 0f))
            {
                // treat the item's footprint as including its surrounding spacing: one gap on the
                // leading side, and another on the trailing side unless this is a boundary item
                var itemSize = _source?.GetItemSize?.Invoke(dataIndex) ?? 0;
                itemSize += _Spacing;
                if (dataIndex > 0 && dataIndex < ItemCount - 1) itemSize += _Spacing;

                itemOffsetPosition = itemSize * options.ItemOffset;
            }

            if (Mathf.Approximately(options.ScrollerOffset, 1f))
                itemOffsetPosition += TrailingPadding;

            var offset = -(options.ScrollerOffset * ViewportLength) + itemOffsetPosition;

            var newScrollPosition = GetScrollPositionForDataIndex(dataIndex) + offset;
            newScrollPosition = Mathf.Clamp(newScrollPosition - _Spacing, 0, ScrollableLength);

            // ignore the jump if the scroll position hasn't changed
            if (Mathf.Approximately(newScrollPosition, _scrollPosition))
            {
                options.OnCompleted?.Invoke();
                return null;
            }

            // clamp to the real content bounds so we never tween past the ends
            var maxScrollPosition = Mathf.Max(0, AxisSize(ScrollRect.content.rect) - AxisSize(ScrollRect.viewport.rect));
            newScrollPosition = Mathf.Clamp(newScrollPosition, 0, maxScrollPosition);

            // build a velocity-continuous ease so the tween flows out of the current scroll speed
            var velocity = options.StartingVelocity ?? AxisVelocity;
            AnimationCurve velocityCurve = null;
            if (options.TweenTime > 0 && !Mathf.Approximately(velocity, 0f))
            {
                var distance = _scrollPosition + offset - newScrollPosition;
                if (!Mathf.Approximately(distance, 0f))
                {
                    var startTangent = velocity / options.TweenTime / distance;
                    velocityCurve = new AnimationCurve(
                        new Keyframe(0f, 0f, -startTangent, startTangent),
                        new Keyframe(1f, 1f, 0f, 0f));
                }
            }

            return TweenPosition(options.Ease, velocityCurve, options.TweenTime, ScrollPosition, newScrollPosition, options.OnCompleted);
        }

        public void Snap()
        {
            if (ItemCount == 0) return;

            // capture the current speed (so the snap can flow out of it) before stopping;
            // the tween then takes over via BeginDrivenScroll
            _snapJumping = true;
            var previousVelocity = AxisVelocity;
            AxisVelocity = 0;

            // pick the item sitting under the alignment anchor, then land that same point on it
            var alignment = Mathf.Clamp01(_SnapAlignment);
            var snapPosition = ScrollPosition + ViewportLength * alignment;
            _snapItemIndex = GetItemIndexAtPosition(snapPosition);
            _snapDataIndex = _snapItemIndex;

            JumpToDataIndex(_snapDataIndex, new JumpOptions
            {
                ScrollerOffset = alignment,
                ItemOffset = alignment,
                Ease = _SnapTweenType,
                TweenTime = _SnapTweenTime,
                StartingVelocity = previousVelocity,
                OnCompleted = SnapJumpComplete,
            });
        }

        public float GetScrollPositionForItemIndex(int itemIndex)
        {
            if (ItemCount == 0) return 0;
            if (itemIndex < 0) itemIndex = 0;

            if (itemIndex == 0)
                return LeadingPadding;

            // previous item's offset plus the spacing between items
            return _itemOffsetArray[itemIndex - 1] + _Spacing + LeadingPadding;
        }

        public float GetScrollPositionForDataIndex(int dataIndex)
        {
            return GetScrollPositionForItemIndex(dataIndex);
        }

        public int GetItemIndexAtPosition(float position)
        {
            return FindItemIndexInRange(position, 0, _itemOffsetArray.Count - 1);
        }

        public RecyclableScrollerItemView GetItemViewAtDataIndex(int dataIndex)
        {
            foreach (var activeItemView in _activeItemViews)
            {
                if (activeItemView.DataIndex == dataIndex)
                    return activeItemView;
            }

            return null;
        }

        public void ToggleTweenPaused(float newTweenTime = -1)
        {
            TweenPaused = !_tweenPaused;

            // when resuming, optionally re-time the remaining portion of the tween
            if (!_tweenPaused && _scrollTween != null && newTweenTime > 0)
            {
                var remaining = _scrollTween.Duration(false) - _scrollTween.Elapsed(false);
                if (remaining > 0)
                    _scrollTween.timeScale = remaining / newTweenTime;
            }
        }

        public void InterruptTween()
        {
            if (!IsTweening) return;

            // killing the tween runs its OnKill, which hands control back to the ScrollRect
            _scrollTween?.Kill();
            _scrollTween = null;

            _snapJumping = false;
            IsTweening = false;
            ScrollerTweeningChanged?.Invoke(this, false);
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (IsTweening && _InterruptTweenMode == TweenInterruptModeEnum.OnPointerDown)
                InterruptTween();
        }

        public void OnBeginDrag(PointerEventData data)
        {
            IsDragging = true;

            _dragFingerCount++;
            if (_dragFingerCount > 1) return;

            // disable snapping while dragging if desired
            _snapBeforeDrag = _Snapping;
            if (!_SnapWhileDragging)
                _Snapping = false;

            if (IsTweening && _InterruptTweenMode == TweenInterruptModeEnum.OnDrag)
                InterruptTween();
        }

        public void OnEndDrag(PointerEventData data)
        {
            IsDragging = false;

            _dragFingerCount--;
            if (_dragFingerCount < 0) _dragFingerCount = 0;

            // restore the snapping captured before the drag
            _Snapping = _snapBeforeDrag;

            // snap to the nearest item once the last finger lifts; the velocity-continuous
            // tween flows out of any remaining fling speed
            if (_Snapping && _dragFingerCount == 0 && !_snapJumping)
                Snap();
        }

        private void Resize(bool keepPosition)
        {
            var originalScrollPosition = _scrollPosition;

            RebuildItemMetrics();

            // size the container to fit all items and the leading + trailing padding
            var contentSize = _itemOffsetArray.Count > 0 ? _itemOffsetArray[^1] : 0f;
            contentSize += LeadingPadding + TrailingPadding;
            Container.sizeDelta = IsVertical
                ? new Vector2(Container.sizeDelta.x, contentSize)
                : new Vector2(contentSize, Container.sizeDelta.y);

            ResetVisibleItems();

            if (keepPosition)
                ScrollPosition = originalScrollPosition;
            else
                ScrollPosition = 0;
        }

        private void UpdateSpacing(float spacing)
        {
            _updateSpacing = false;
            _layoutGroup.spacing = spacing;
            ReloadData(NormalizedScrollPosition);
        }

        // Builds both metric arrays in one sweep: each item's "slot" is its own size plus the
        // one gap that precedes it (the first item has no preceding gap), and the offset array
        // holds the running cumulative end of every slot. Keeping the gap folded into the slot
        // lets the spacer/layout math recover either value with a single subtraction later.
        private void RebuildItemMetrics()
        {
            _itemSizeArray.Clear();
            _itemOffsetArray.Clear();

            var spacing = _layoutGroup.spacing;
            var runningEnd = 0f;
            var count = ItemCount;

            for (var i = 0; i < count; i++)
            {
                var slot = _source.GetItemSize(i);
                if (i > 0) slot += spacing;

                runningEnd += slot;
                _itemSizeArray.Add(slot);
                _itemOffsetArray.Add(runningEnd);
            }
        }

        private RecyclableScrollerItemView GetRecycledItemView(RecyclableScrollerItemView itemPrefab)
        {
            for (var i = 0; i < _recycledItemViews.Count; i++)
            {
                if (_recycledItemViews[i].SourcePrefab != itemPrefab)
                    continue;

                var itemView = _recycledItemViews[i];
                _recycledItemViews.RemoveAt(i);
                return itemView;
            }

            return null;
        }

        private void ResetVisibleItems()
        {
            CalculateCurrentActiveItemRange(out var startIndex, out var endIndex);

            // Release any active view that no longer touches the visible window. The active set
            // is always a contiguous run of indices, so the survivors stay contiguous too; we
            // only need the lowest and highest survivor to know which indices are still missing.
            // Walking backwards lets RecycleItem remove in place without disturbing the cursor.
            var keptLow = int.MaxValue;
            var keptHigh = int.MinValue;
            for (var i = _activeItemViews.Count - 1; i >= 0; i--)
            {
                var index = _activeItemViews[i].ItemIndex;
                if (index < startIndex || index > endIndex)
                {
                    RecycleItem(_activeItemViews[i]);
                }
                else
                {
                    if (index < keptLow) keptLow = index;
                    if (index > keptHigh) keptHigh = index;
                }
            }

            if (keptHigh < keptLow)
            {
                // No survivors (fresh data or a jump elsewhere): materialize the whole window.
                for (var index = startIndex; index <= endIndex; index++)
                    AddItemView(index, ItemEdge.Trailing);
            }
            else
            {
                // Fill the gap below the survivors, descending so each prepend lands ahead of
                // the previous one and the final order stays ascending...
                for (var index = keptLow - 1; index >= startIndex; index--)
                    AddItemView(index, ItemEdge.Leading);

                // ...then fill the gap above them by appending.
                for (var index = keptHigh + 1; index <= endIndex; index++)
                    AddItemView(index, ItemEdge.Trailing);
            }

            StartItemIndex = startIndex;
            EndItemIndex = endIndex;
            DataIndexRangeChanged?.Invoke(StartItemIndex, EndItemIndex);

            SetSpacers();
        }

        private void RecycleAllItems()
        {
            while (_activeItemViews.Count > 0) RecycleItem(_activeItemViews[0]);
            StartItemIndex = 0;
            EndItemIndex = 0;
        }

        private void RecycleItem(RecyclableScrollerItemView itemView)
        {
            ItemWillRecycle?.Invoke(itemView);

            _activeItemViews.Remove(itemView);
            _recycledItemViews.Add(itemView);

            // deactivating is cheaper than reparenting
            itemView.transform.gameObject.SetActive(false);

            itemView.DataIndex = 0;
            itemView.ItemIndex = 0;
            itemView.Active = false;

            ItemVisibilityChanged?.Invoke(itemView, false);
        }

        private void AddItemView(int itemIndex, ItemEdge edge)
        {
            if (ItemCount == 0) return;

            var itemView = _source.GetItemView(itemIndex, itemIndex);

            itemView.ItemIndex = itemIndex;
            itemView.DataIndex = itemIndex;
            itemView.Active = true;

            itemView.transform.SetParent(Container, false);
            itemView.transform.localScale = Vector3.one;

            // cache the layout element on first use so later shows skip the GetComponent
            if (itemView.LayoutElement == null)
            {
                if (!itemView.TryGetComponent(out LayoutElement layoutElement))
                    layoutElement = itemView.gameObject.AddComponent<LayoutElement>();
                itemView.LayoutElement = layoutElement;
            }

            // size the layout element, removing the spacing baked into the size for non-first items
            var itemSize = _itemSizeArray[itemIndex] - (itemIndex > 0 ? _layoutGroup.spacing : 0);
            if (IsVertical)
                itemView.LayoutElement.minHeight = itemSize;
            else
                itemView.LayoutElement.minWidth = itemSize;

            if (edge == ItemEdge.Leading)
                _activeItemViews.Insert(0, itemView);
            else
                _activeItemViews.Add(itemView);

            // keep the hierarchy order between the two spacers
            if (edge == ItemEdge.Trailing)
                itemView.transform.SetSiblingIndex(Container.childCount - 2);
            else
                itemView.transform.SetSiblingIndex(1);

            ItemVisibilityChanged?.Invoke(itemView, true);
        }

        private void SetSpacers()
        {
            if (ItemCount == 0) return;

            var firstSize = _itemOffsetArray[StartItemIndex] - _itemSizeArray[StartItemIndex];
            var lastSize = _itemOffsetArray[^1] - _itemOffsetArray[EndItemIndex];

            SetSpacerSize(LeadingSpacer, firstSize);
            SetSpacerSize(TrailingSpacer, lastSize);
        }

        private void RefreshActive()
        {
            CalculateCurrentActiveItemRange(out var startIndex, out var endIndex);

            // nothing to do if the visible range hasn't changed
            if (startIndex == StartItemIndex && endIndex == EndItemIndex) return;

            ResetVisibleItems();
        }

        private void CalculateCurrentActiveItemRange(out int startIndex, out int endIndex)
        {
            var startPosition = _scrollPosition - _lookAheadBefore;
            var endPosition = _scrollPosition + ViewportLength + _lookAheadAfter;

            startIndex = GetItemIndexAtPosition(startPosition);
            endIndex = GetItemIndexAtPosition(endPosition);
        }

        // Lower-bound search: returns the lowest index in [lo, hi] whose cumulative end offset
        // reaches the query position. Offsets are stored without the leading padding, so the
        // padding is added back per comparison; the tiny bias resolves an exact boundary hit in
        // favour of the earlier item (and is skipped when there is no padding to bias against).
        private int FindItemIndexInRange(float position, int lo, int hi)
        {
            var pad = LeadingPadding;
            var threshold = position + (pad != 0f ? 1.00001f : 0f);

            while (lo < hi)
            {
                var mid = lo + ((hi - lo) >> 1);
                if (_itemOffsetArray[mid] + pad >= threshold)
                    hi = mid;
                else
                    lo = mid + 1;
            }

            return lo;
        }

        private void ScrollRect_OnValueChanged(Vector2 val)
        {
            var factor = IsVertical ? 1f - val.y : val.x;
            _scrollPosition = Mathf.Clamp(factor * ScrollableLength, 0, ScrollableLength);

            ScrollerScrolled?.Invoke(this, val, _scrollPosition);

            // snap once the speed drops below the threshold
            if (_Snapping && !_snapJumping)
            {
                if (Mathf.Abs(AxisVelocity) <= _SnapVelocityThreshold && !Mathf.Approximately(AxisVelocity, 0f))
                    Snap();
            }

            RefreshActive();
        }

        private void SnapJumpComplete()
        {
            // the ScrollRect was already handed back by the tween's OnKill (EndDrivenScroll)
            _snapJumping = false;

            var itemView = GetItemViewAtDataIndex(_snapDataIndex);
            ScrollerSnapped?.Invoke(this, _snapItemIndex, _snapDataIndex, itemView);
        }

        private Tween TweenPosition(Ease ease, AnimationCurve easeCurve, float time, float start, float end, Action tweenComplete)
        {
            // kill any tween already in progress without completing it
            _scrollTween?.Kill();
            _scrollTween = null;

            if (time <= 0)
            {
                ScrollPosition = end;
                RefreshActive();
                tweenComplete?.Invoke();
                return null;
            }

            // take manual control of the ScrollRect so the tween can drive its position
            BeginDrivenScroll();
            ScrollPosition = start;

            IsTweening = true;
            ScrollerTweeningChanged?.Invoke(this, true);

            _scrollTween = DOTween.To(() => ScrollPosition, value => ScrollPosition = value, end, time)
                .SetUpdate(true) // drive on unscaled time so snaps run during pauses
                .OnComplete(() =>
                {
                    _scrollTween = null;

                    // land exactly on the end position
                    ScrollPosition = end;

                    tweenComplete?.Invoke();

                    IsTweening = false;
                    ScrollerTweeningChanged?.Invoke(this, false);
                })
                .OnKill(EndDrivenScroll);

            // use the velocity-continuous curve when supplied, otherwise the plain ease
            if (easeCurve != null)
                _scrollTween.SetEase(easeCurve);
            else
                _scrollTween.SetEase(ease);

            if (_tweenPaused)
                _scrollTween.Pause();

            return _scrollTween;
        }
        
        private void BeginDrivenScroll()
        {
            if (_drivenScroll) return;
            _drivenScroll = true;

            _savedInertia = ScrollRect.inertia;
            ScrollRect.inertia = false;
            ScrollRect.movementType = ScrollRect.MovementType.Unrestricted;
            ScrollRect.velocity = Vector2.zero;
        }
        
        private void EndDrivenScroll()
        {
            if (!_drivenScroll) return;
            _drivenScroll = false;

            ScrollRect.inertia = _savedInertia;
            ScrollRect.movementType = _scrollRectMovementType;
        }

        private float AxisOf(Vector2 v) => IsVertical ? v.y : v.x;
        private Vector2 AxisVector(float value) => IsVertical ? new Vector2(0, value) : new Vector2(value, 0);
        private float AxisSize(Rect rect) => IsVertical ? rect.height : rect.width;
        
        private void SetNormalizedAxisPosition(float factor)
        {
            if (IsVertical)
                ScrollRect.verticalNormalizedPosition = 1f - factor;
            else
                ScrollRect.horizontalNormalizedPosition = factor;
        }
        
        private void SetSpacerSize(LayoutElement spacer, float size)
        {
            if (IsVertical)
                spacer.minHeight = size;
            else
                spacer.minWidth = size;

            spacer.gameObject.SetActive(size > 0);
        }
        
        private static void DestroyInternal(UnityEngine.Object obj)
        {
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }
    }
}
