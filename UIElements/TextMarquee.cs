// Author: František Holubec
// Created: 15.10.2026

using TMPro;
using UnityEngine;

namespace EDIVE.UIElements
{
    // Scrolls a text that does not fit sideways inside its own rect, pauses at the end and jumps back.
    // Glyphs fade out only on a side where the text continues.
    [RequireComponent(typeof(TMP_Text))]
    public class TextMarquee : MonoBehaviour
    {
        [SerializeField]
        [Min(0)]
        private float _Delay = 0.8f;

        // Pixels per second
        [SerializeField]
        [Min(1)]
        private float _Speed = 40f;

        [SerializeField]
        [Min(0)]
        private float _EndPause = 1f;

        // Soft edge where the scrolling text fades out, in pixels
        [SerializeField]
        [Min(0)]
        private float _EdgeFade = 6f;

        private TMP_Text _text;
        private bool _active;
        private bool _scrolling;
        private float _time;
        private float _offset;
        private float _overflow;
        private TextOverflowModes _overflowMode;
        private Vector4 _margin;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable() => SetActive(true);
        private void OnDisable() => SetActive(false);

        public void SetActive(bool active)
        {
            if (_active == active)
                return;
            _active = active;
            _time = 0;
            if (!active)
                StopScrolling();
        }

        private void Update()
        {
            if (!_active)
                return;

            _time += Time.unscaledDeltaTime;
            var overflow = _text.preferredWidth - _text.rectTransform.rect.width;
            if (overflow <= 0 || _time < _Delay)
                return;

            if (!_scrolling)
                StartScrolling();

            var elapsed = _time - _Delay;
            if (elapsed > overflow / _Speed + _EndPause)
            {
                _time = 0;
                StopScrolling();
                return;
            }

            _overflow = overflow;
            _offset = Mathf.Min(elapsed * _Speed, overflow);
            _text.margin = new Vector4(_margin.x - _offset, _margin.y, _margin.z, _margin.w);
            _text.ForceMeshUpdate();
            ClipToRect();
        }

        // Vertex alpha hides what lies outside the rect, the fade softens the cut
        private void ClipToRect()
        {
            var rect = _text.rectTransform.rect;
            var info = _text.textInfo;
            for (var i = 0; i < info.characterCount; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible)
                    continue;
                var mesh = info.meshInfo[character.materialReferenceIndex];
                for (var v = 0; v < 4; v++)
                {
                    var index = character.vertexIndex + v;
                    var x = mesh.vertices[index].x;
                    var visible = Mathf.Min(EdgeVisibility(x - rect.xMin, _offset), EdgeVisibility(rect.xMax - x, _overflow - _offset));
                    mesh.colors32[index].a = (byte) (character.color.a * visible);
                }
            }
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        // Hidden past the edge, a fade inside it while text continues beyond, the fade grows in over the first pixels of travel
        private float EdgeVisibility(float distance, float hiddenBeyond)
        {
            if (distance < 0)
                return 0;
            var fade = Mathf.Min(_EdgeFade, hiddenBeyond);
            return fade > 0 ? Mathf.Clamp01(distance / fade) : 1;
        }

        // The whole line shows while it scrolls, clipped to the text rect
        private void StartScrolling()
        {
            _scrolling = true;
            _overflowMode = _text.overflowMode;
            _margin = _text.margin;
            _text.overflowMode = TextOverflowModes.Overflow;
        }

        private void StopScrolling()
        {
            if (!_scrolling)
                return;
            _scrolling = false;
            _text.overflowMode = _overflowMode;
            _text.margin = _margin;
            _text.ForceMeshUpdate();
        }
    }
}
