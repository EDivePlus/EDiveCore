// Author: František Holubec
// Created: 23.06.2025

using System.Collections.Generic;
using EnhancedUI.EnhancedScroller;
using UnityEngine;

namespace EDIVE.StagePlay.Script.UI
{
    public class ScriptScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
    {
        [SerializeField]
        private StagePlayConfig _Config;

        [SerializeField]
        private StagePlaySceneDefinition _CurrentStagePlayScene;

        [SerializeField]
        private EnhancedScroller _Scroller;

        [SerializeField]
        private LineScriptSegmentDisplay _LineDisplayPrefab;

        [SerializeField]
        private DirectionScriptSegmentDisplay _DirectionDisplayPrefab;

        private List<AScriptSegment> _currentSegments;

        private void Start()
        {
            _Scroller.Delegate = this;
            RefreshScroller();
        }

        public void SetPlayScene(StagePlaySceneDefinition scene)
        {
            _CurrentStagePlayScene = scene;
            RefreshScroller();
        }

        private void RefreshScroller()
        {
            if (_CurrentStagePlayScene == null)
            {
                _currentSegments = null;
                _Scroller.ReloadData();
                return;
            }

            _currentSegments = _CurrentStagePlayScene.ScriptSegments;
            _Scroller.ReloadData(_Scroller.NormalizedScrollPosition);
        }

        public int GetNumberOfCells(EnhancedScroller scroller) => _currentSegments?.Count ?? 0;

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            var rect = _currentSegments[dataIndex] switch
            {
                LineScriptSegment => (RectTransform) _LineDisplayPrefab?.transform,
                DirectionScriptSegment => (RectTransform) _DirectionDisplayPrefab?.transform,
                _ => null
            };

            if (rect != null) return rect.rect.height;
            return 0;
        }

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cellView = _currentSegments[dataIndex] switch
            {
                LineScriptSegment _ => _LineDisplayPrefab ? _Scroller.GetCellView(_LineDisplayPrefab) as AScriptSegmentDisplay : null,
                DirectionScriptSegment _ => _DirectionDisplayPrefab ? _Scroller.GetCellView(_DirectionDisplayPrefab) as AScriptSegmentDisplay : null,
                _ => null
            };

            if (!cellView)
                return null;

            cellView.SetData(_currentSegments[dataIndex], _Config);
            return cellView;
        }

    }
}
