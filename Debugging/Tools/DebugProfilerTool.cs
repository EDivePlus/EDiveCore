// Author: František Holubec
// Created: 15.10.2026

using System;
using System.Globalization;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Frame time graph, frame stats and memory. Sampled only while the tab is shown.
    public sealed class DebugProfilerTool : ADebugTool
    {
        public override string Name => "Profiler";
        public override string Icon => "monitoring";
        public override int Order => -95;

        internal bool Paused;

        public override VisualElement CreateContent(DebugMenuView view) => new DebugProfilerView(this);
    }

    internal sealed class DebugProfilerView : VisualElement, IDebugHeaderContent
    {
        private const int SAMPLES = 240;
        private const float MB = 1024f * 1024f;
        private const long TEXT_INTERVAL_MS = 250;

        private static readonly CustomStyleProperty<Color> ACCENT_PROPERTY = new("--dm-accent");
        private static readonly CustomStyleProperty<Color> WARNING_PROPERTY = new("--dm-warning");

        private readonly DebugProfilerTool _tool;
        private readonly float[] _frames = new float[SAMPLES];
        private readonly VisualElement _graph;
        private readonly VisualElement _targetLine;
        private readonly Label _targetLabel;
        private readonly Button _pauseButton;
        private readonly Button _fpsButton;
        private readonly Tile _fps;
        private readonly Tile _frame;
        private readonly Tile _cpu;
        private readonly Tile _gc;
        private readonly Tile _batches;
        private readonly MemoryRow _reserved;
        private readonly MemoryRow _allocated;
        private readonly MemoryRow _managed;
        private readonly MemoryRow _graphics;

        private int _count;
        private int _head;
        private float _maxFrame;
        private Color _accent = new(0.22f, 0.74f, 0.97f);
        private Color _warning = new(0.98f, 0.75f, 0.14f);
        private ProfilerRecorder _mainThread;
        private ProfilerRecorder _gcAlloc;
        private ProfilerRecorder _batchCount;
        private ProfilerRecorder _drawCalls;
        private IVisualElementScheduledItem _sampleTick;
        private IVisualElementScheduledItem _textTick;

        public VisualElement HeaderContent { get; }

        public DebugProfilerView(DebugProfilerTool tool)
        {
            _tool = tool;
            AddToClassList("dm-profiler");

            var header = new VisualElement();
            header.AddToClassList("dm-header__group");
            // FPS counter over the app while the menu is closed
            _fpsButton = CreateToggle("speed", "FPS", () => DebugFpsCounter.Enabled = !DebugFpsCounter.Enabled);
            header.Add(_fpsButton);
            _pauseButton = CreateToggle("pause", "Pause", TogglePause);
            header.Add(_pauseButton);
            HeaderContent = header;

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("dm-profiler__scroll");
            Add(scroll);

            var tiles = new VisualElement();
            tiles.AddToClassList("dm-profiler__tiles");
            _fps = new Tile(tiles, "FPS", string.Empty);
            _frame = new Tile(tiles, "Frame", "ms");
            _cpu = new Tile(tiles, "CPU main", "ms");
            _gc = new Tile(tiles, "GC alloc", "KB/frame");
            _batches = new Tile(tiles, "Batches", string.Empty);
            scroll.Add(tiles);

            var graphCard = CreateCard("Frame time");
            _graph = new VisualElement();
            _graph.AddToClassList("dm-profiler__graph");
            _graph.generateVisualContent += DrawGraph;
            _targetLine = new VisualElement {pickingMode = PickingMode.Ignore};
            _targetLine.AddToClassList("dm-profiler__target");
            _graph.Add(_targetLine);
            _targetLabel = new Label {pickingMode = PickingMode.Ignore};
            _targetLabel.AddToClassList("dm-profiler__target-label");
            _graph.Add(_targetLabel);
            graphCard.Add(_graph);
            scroll.Add(graphCard);

            var bottom = new VisualElement();
            bottom.AddToClassList("dm-profiler__bottom");
            scroll.Add(bottom);

            var memory = CreateCard("Memory");
            memory.AddToClassList("dm-profiler__memory");
            _reserved = new MemoryRow(memory, "Reserved");
            _allocated = new MemoryRow(memory, "Allocated");
            _managed = new MemoryRow(memory, "Managed");
            _graphics = new MemoryRow(memory, "Graphics");
            bottom.Add(memory);

            var actions = CreateCard("Actions");
            actions.AddToClassList("dm-profiler__actions");
            actions.Add(CreateAction("recycling", "Collect GC", () => GC.Collect()));
            actions.Add(CreateAction("cleaning_services", "Unload unused assets", () => Resources.UnloadUnusedAssets()));
            actions.Add(CreateAction("restart_alt", "Reset max", ResetFrames));
            bottom.Add(actions);

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyle);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
            UpdatePauseButton();
            UpdateFpsButton();
        }

        private static Button CreateToggle(string icon, string text, Action clicked)
        {
            var button = new Button(clicked);
            button.AddToClassList("dm-toggle");
            button.Add(DebugIcons.Create(icon, "dm-toggle__icon"));
            var label = new Label(text);
            label.AddToClassList("dm-toggle__label");
            button.Add(label);
            return button;
        }

        private void UpdateFpsButton() => _fpsButton.EnableInClassList("dm-toggle--on", DebugFpsCounter.Enabled);

        private void OnAttach(AttachToPanelEvent evt)
        {
            _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            _gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _batchCount = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _sampleTick = schedule.Execute(Sample).Every(0);
            _textTick = schedule.Execute(UpdateTexts).Every(TEXT_INTERVAL_MS);
            DebugFpsCounter.EnabledChanged += UpdateFpsButton;
            UpdateTexts();
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            _sampleTick?.Pause();
            _textTick?.Pause();
            DebugFpsCounter.EnabledChanged -= UpdateFpsButton;
            _mainThread.Dispose();
            _gcAlloc.Dispose();
            _batchCount.Dispose();
            _drawCalls.Dispose();
        }

        private void OnCustomStyle(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(ACCENT_PROPERTY, out var accent))
                _accent = accent;
            if (evt.customStyle.TryGetValue(WARNING_PROPERTY, out var warning))
                _warning = warning;
        }

        private void TogglePause()
        {
            _tool.Paused = !_tool.Paused;
            UpdatePauseButton();
        }

        private void UpdatePauseButton() => _pauseButton.EnableInClassList("dm-toggle--on", _tool.Paused);

        private void ResetFrames()
        {
            _count = 0;
            _head = 0;
            _maxFrame = 0f;
            _graph.MarkDirtyRepaint();
        }

        private void Sample()
        {
            if (_tool.Paused)
                return;

            var ms = Time.unscaledDeltaTime * 1000f;
            _frames[_head] = ms;
            _head = (_head + 1) % SAMPLES;
            _count = Mathf.Min(_count + 1, SAMPLES);
            _maxFrame = Mathf.Max(_maxFrame, ms);
            _graph.MarkDirtyRepaint();
        }

        private float GetFrame(int age) => _frames[(_head - 1 - age + SAMPLES) % SAMPLES];

        private void UpdateTexts()
        {
            if (_tool.Paused)
                return;

            var recent = Mathf.Min(_count, 30);
            var sum = 0f;
            for (var i = 0; i < recent; i++)
            {
                sum += GetFrame(i);
            }
            var avg = recent > 0 ? sum / recent : 0f;

            _fps.Set(avg > 0f ? (1000f / avg).ToString("0", CultureInfo.InvariantCulture) : "-", $"target {TargetFps:0}");
            _fps.SetWarning(avg > TargetMs * 1.1f);
            _frame.Set(F1(avg), $"max {F1(_maxFrame)}");
            _cpu.Set(_mainThread.Valid && _mainThread.Count > 0 ? F1(_mainThread.LastValue / 1e6f) : "-", _mainThread.Valid ? string.Empty : "dev builds only");
            _gc.Set(_gcAlloc.Valid ? F1(_gcAlloc.LastValue / 1024f) : "-", $"{GC.CollectionCount(0)} collections");
            _batches.Set(_batchCount.Valid ? _batchCount.LastValue.ToString(CultureInfo.InvariantCulture) : "-",
                _drawCalls.Valid ? $"{_drawCalls.LastValue} draw calls" : "dev builds only");

            var systemBytes = SystemInfo.systemMemorySize * MB;
            var reserved = Profiler.GetTotalReservedMemoryLong();
            _reserved.Set(reserved, systemBytes);
            _allocated.Set(Profiler.GetTotalAllocatedMemoryLong(), reserved);
            _managed.Set(GC.GetTotalMemory(false), Profiler.GetMonoHeapSizeLong());
            // VRAM, shared memory on mobile and standalone XR
            var graphicsBytes = SystemInfo.graphicsMemorySize > 0 ? SystemInfo.graphicsMemorySize * MB : systemBytes;
            _graphics.Set(Profiler.GetAllocatedMemoryForGraphicsDriver(), graphicsBytes);

            UpdateTarget();
        }

        private static float TargetFps => DebugFpsCounter.TargetFps;

        private static float TargetMs => 1000f / TargetFps;

        // Scale fits twice the target frame time, or the slowest frame
        private float GraphScale => Mathf.Max(TargetMs * 2f, _maxFrame * 1.1f);

        private void UpdateTarget()
        {
            var height = _graph.contentRect.height;
            if (height <= 0f)
                return;
            var y = height - TargetMs / GraphScale * height;
            _targetLine.style.top = y;
            _targetLabel.style.top = Mathf.Max(0f, y - 20f);
            _targetLabel.text = $"{F1(TargetMs)} ms · {TargetFps:0} FPS";
        }

        private void DrawGraph(MeshGenerationContext context)
        {
            var rect = _graph.contentRect;
            if (_count == 0 || rect.width <= 0f || rect.height <= 0f)
                return;

            var painter = context.painter2D;
            var barWidth = rect.width / SAMPLES;
            var scale = GraphScale;
            var limit = TargetMs * 1.1f;

            DrawBars(painter, rect, barWidth, scale, f => f <= limit, _accent);
            DrawBars(painter, rect, barWidth, scale, f => f > limit, _warning);
        }

        // Newest frame on the right
        private void DrawBars(Painter2D painter, Rect rect, float barWidth, float scale, Func<float, bool> filter, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            var any = false;
            for (var age = 0; age < _count; age++)
            {
                var frame = GetFrame(age);
                if (!filter(frame))
                    continue;

                var x = rect.width - (age + 1) * barWidth;
                var top = rect.height - Mathf.Min(1f, frame / scale) * rect.height;
                painter.MoveTo(new Vector2(x, rect.height));
                painter.LineTo(new Vector2(x, top));
                painter.LineTo(new Vector2(x + barWidth * 0.8f, top));
                painter.LineTo(new Vector2(x + barWidth * 0.8f, rect.height));
                painter.ClosePath();
                any = true;
            }
            if (any)
                painter.Fill();
        }

        private static string F1(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);

        private static VisualElement CreateCard(string title)
        {
            var card = new VisualElement();
            card.AddToClassList("dm-card");
            var header = new VisualElement();
            header.AddToClassList("dm-card__header");
            var label = new Label(title);
            label.AddToClassList("dm-card__title");
            header.Add(label);
            card.Add(header);
            return card;
        }

        private static Button CreateAction(string icon, string text, Action clicked)
        {
            var button = new Button(clicked);
            button.AddToClassList("dm-button");
            button.AddToClassList("dm-profiler__action");
            button.Add(DebugIcons.Create(icon, "dm-profiler__action-icon"));
            var label = new Label(text);
            button.Add(label);
            return button;
        }

        private sealed class Tile
        {
            private readonly VisualElement _root;
            private readonly Label _value;
            private readonly Label _sub;

            public Tile(VisualElement parent, string title, string unit)
            {
                _root = new VisualElement();
                _root.AddToClassList("dm-tile");
                var label = new Label(title);
                label.AddToClassList("dm-tile__label");
                _root.Add(label);

                var line = new VisualElement();
                line.AddToClassList("dm-tile__line");
                _value = new Label("-");
                _value.AddToClassList("dm-tile__value");
                line.Add(_value);
                if (!string.IsNullOrEmpty(unit))
                {
                    var unitLabel = new Label(unit);
                    unitLabel.AddToClassList("dm-tile__unit");
                    line.Add(unitLabel);
                }
                _root.Add(line);

                _sub = new Label();
                _sub.AddToClassList("dm-tile__sub");
                _root.Add(_sub);
                parent.Add(_root);
            }

            public void Set(string value, string sub)
            {
                _value.text = value;
                _sub.text = sub;
            }

            public void SetWarning(bool warning) => _root.EnableInClassList("dm-tile--warning", warning);
        }

        private sealed class MemoryRow
        {
            private readonly Label _value;
            private readonly VisualElement _fill;

            public MemoryRow(VisualElement parent, string title)
            {
                var row = new VisualElement();
                row.AddToClassList("dm-memory");
                var label = new Label(title);
                label.AddToClassList("dm-memory__label");
                row.Add(label);
                var bar = new VisualElement();
                bar.AddToClassList("dm-memory__bar");
                _fill = new VisualElement();
                _fill.AddToClassList("dm-memory__fill");
                bar.Add(_fill);
                row.Add(bar);
                _value = new Label();
                _value.AddToClassList("dm-memory__value");
                row.Add(_value);
                parent.Add(row);
            }

            public void Set(long bytes, float totalBytes)
            {
                _value.text = bytes > 0 ? $"{Format(bytes)} / {Format((long) totalBytes)}" : "-";
                _fill.style.width = Length.Percent(totalBytes > 0f ? Mathf.Clamp01(bytes / totalBytes) * 100f : 0f);
            }

            private static string Format(long bytes) => bytes >= 1024 * MB
                ? (bytes / (1024f * MB)).ToString("0.00 GB", CultureInfo.InvariantCulture)
                : (bytes / MB).ToString("0 MB", CultureInfo.InvariantCulture);
        }
    }
}
