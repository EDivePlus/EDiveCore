using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Http.Editor
{
    public class NetworkDebugWindow : OdinEditorWindow
    {
        private readonly List<RequestEntry> _entries = new();
        private RequestEntry _selected;
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private string _filterText = "";
        private bool _requestHeadersFoldout;
        private bool _responseHeadersFoldout;

        [MenuItem("Window/EDive/Network Monitor")]
        public static void Open()
        {
            var window = GetWindow<NetworkDebugWindow>("Network Monitor");
            window.Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            wantsMouseMove = true;
            
            // Populate from existing logs
            foreach (var log in NetworkRequestLogger.Logs)
            {
                _entries.Add(new RequestEntry(log));
            }

            NetworkRequestLogger.OnLogAdded += OnLogAdded;
            NetworkRequestLogger.OnLogUpdated += OnLogUpdated;
            NetworkRequestLogger.OnLogsCleared += OnLogsCleared;
        }

        protected override void OnDestroy()
        {
            NetworkRequestLogger.OnLogAdded -= OnLogAdded;
            NetworkRequestLogger.OnLogUpdated -= OnLogUpdated;
            NetworkRequestLogger.OnLogsCleared -= OnLogsCleared;
            base.OnDestroy();
        }

        private void OnLogAdded(NetworkRequestLog log)
        {
            _entries.Add(new RequestEntry(log));
            Repaint();
        }

        private void OnLogUpdated(NetworkRequestLog log)
        {
            Repaint();
        }

        private void OnLogsCleared()
        {
            _entries.Clear();
            _selected = null;
            Repaint();
        }

        protected override void OnImGUI()
        {
            DrawToolbar();

            var toolbarHeight = EditorStyles.toolbar.fixedHeight;
            var fullRect = new Rect(0, toolbarHeight, position.width, position.height - toolbarHeight);

            var leftWidth = Mathf.Floor(fullRect.width * 0.45f);
            var separatorWidth = 1f;
            var rightWidth = fullRect.width - leftWidth - separatorWidth;

            // Left panel - request list
            var leftRect = new Rect(fullRect.x, fullRect.y, leftWidth, fullRect.height);
            EditorGUI.DrawRect(leftRect, new Color(0.22f, 0.22f, 0.22f));
            GUILayout.BeginArea(leftRect);
            DrawRequestList();
            GUILayout.EndArea();

            // Separator
            var sepRect = new Rect(leftRect.xMax, fullRect.y, separatorWidth, fullRect.height);
            EditorGUI.DrawRect(sepRect, new Color(0.15f, 0.15f, 0.15f));

            // Right panel - detail
            var rightRect = new Rect(sepRect.xMax, fullRect.y, rightWidth, fullRect.height);
            // Clear the background to prevent ghost artifacts when scrolling
            EditorGUI.DrawRect(rightRect, new Color(0.22f, 0.22f, 0.22f));
            GUILayout.BeginArea(rightRect);
            DrawDetailPanel();
            GUILayout.EndArea();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                NetworkRequestLogger.Clear();
            }

            GUILayout.Space(6);
            _filterText = EditorGUILayout.TextField(_filterText, EditorStyles.toolbarSearchField, GUILayout.MinWidth(150));

            GUILayout.FlexibleSpace();

            var pendingCount = _entries.Count(e => e.Log.Status == RequestStatus.Pending);
            if (pendingCount > 0)
            {
                EditorGUILayout.LabelField($"⏳ {pendingCount} pending", EditorStyles.miniLabel, GUILayout.Width(80));
            }

            EditorGUILayout.LabelField($"{_entries.Count} requests", EditorStyles.miniLabel, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRequestList()
        {
            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Method", EditorStyles.miniBoldLabel, GUILayout.Width(52));
            EditorGUILayout.LabelField("Status", EditorStyles.miniBoldLabel, GUILayout.Width(44));
            EditorGUILayout.LabelField("URL", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Time", EditorStyles.miniBoldLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            var filtered = string.IsNullOrEmpty(_filterText)
                ? _entries
                : _entries.Where(e => e.Log.Url.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0 
                                      || e.Log.Method.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            for (int i = filtered.Count - 1; i >= 0; i--)
            {
                var entry = filtered[i];
                var log = entry.Log;
                var isSelected = _selected == entry;

                var bgColor = isSelected ? new Color(0.24f, 0.37f, 0.59f) : (i % 2 == 0 ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.25f, 0.25f, 0.25f));
                
                var rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
                EditorGUI.DrawRect(rowRect, bgColor);

                // Method with color
                var methodStyle = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold };
                methodStyle.normal.textColor = GetMethodColor(log.Method);
                EditorGUILayout.LabelField(log.Method, methodStyle, GUILayout.Width(52));

                // Status
                var statusStyle = new GUIStyle(EditorStyles.miniLabel);
                statusStyle.normal.textColor = GetStatusColor(log);
                var statusText = log.Status == RequestStatus.Pending ? "..." : log.StatusCode.ToString();
                EditorGUILayout.LabelField(statusText, statusStyle, GUILayout.Width(44));

                // URL (shortened)
                var shortUrl = ShortenUrl(log.Url);
                EditorGUILayout.LabelField(shortUrl, EditorStyles.miniLabel);

                // Duration
                var duration = log.Status == RequestStatus.Pending ? "..." : $"{log.DurationMs:F0}ms";
                EditorGUILayout.LabelField(duration, EditorStyles.miniLabel, GUILayout.Width(60));

                EditorGUILayout.EndHorizontal();

                if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                {
                    _selected = entry;
                    Event.current.Use();
                    Repaint();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDetailPanel()
        {
            if (_selected == null)
            {
                EditorGUILayout.HelpBox("Select a request to view details.", MessageType.Info);
                return;
            }

            var log = _selected.Log;
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

            // General
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            DrawReadOnlyField("Method", log.Method);
            DrawReadOnlyField("URL", log.Url);
            DrawReadOnlyField("Status", $"{log.StatusCode} ({log.Status})");
            DrawReadOnlyField("Started", log.StartTime.ToString("HH:mm:ss.fff"));
            DrawReadOnlyField("Duration", log.Status == RequestStatus.Pending ? "Pending..." : $"{log.DurationMs:F0}ms");
            EditorGUILayout.Space(10);

            // Request box
            SirenixEditorGUI.BeginBox("Request");
            
            _requestHeadersFoldout = EditorGUILayout.Foldout(_requestHeadersFoldout, $"Headers ({log.RequestHeaders.Count})", true, EditorStyles.foldoutHeader);
            if (_requestHeadersFoldout)
            {
                EditorGUI.indentLevel++;
                DrawHeaders(log.RequestHeaders);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField("Payload", EditorStyles.boldLabel);
            if (string.IsNullOrEmpty(log.RequestPayload))
            {
                EditorGUILayout.LabelField("(none)", EditorStyles.miniLabel);
            }
            else
            {
                var formatted = TryFormatJson(log.RequestPayload);
                DrawSyntaxHighlightedJson(formatted);
            }

            SirenixEditorGUI.EndBox();
            EditorGUILayout.Space(10);

            // Response box
            SirenixEditorGUI.BeginBox("Response");

            if (log.Status == RequestStatus.Pending)
            {
                EditorGUILayout.LabelField("⏳ Waiting for response...", EditorStyles.miniLabel);
            }
            else
            {
                _responseHeadersFoldout = EditorGUILayout.Foldout(_responseHeadersFoldout, $"Headers ({log.ResponseHeaders.Count})", true, EditorStyles.foldoutHeader);
                if (_responseHeadersFoldout)
                {
                    EditorGUI.indentLevel++;
                    DrawHeaders(log.ResponseHeaders);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space(6);

                if (!string.IsNullOrEmpty(log.ErrorMessage))
                {
                    EditorGUILayout.HelpBox(log.ErrorMessage, MessageType.Error);
                }

                if (!string.IsNullOrEmpty(log.ResponsePayload))
                {
                    var formattedResp = TryFormatJson(log.ResponsePayload);
                    DrawSyntaxHighlightedJson(formattedResp);
                }
            }

            SirenixEditorGUI.EndBox();

            EditorGUILayout.EndScrollView();
        }

        private static void DrawReadOnlyField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(70));
            EditorGUILayout.SelectableLabel(value, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawHeaders(Dictionary<string, string> headers)
        {
            if (headers == null || headers.Count == 0)
            {
                EditorGUILayout.LabelField("(none)", EditorStyles.miniLabel);
                return;
            }

            foreach (var kvp in headers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(kvp.Key, EditorStyles.miniBoldLabel, GUILayout.Width(160));
                EditorGUILayout.SelectableLabel(kvp.Value, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.EndHorizontal();
            }
        }

        private static string TryFormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;
            try
            {
                var obj = Newtonsoft.Json.Linq.JToken.Parse(json);
                return obj.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        private static readonly Color JSON_KEY_COLOR = new(0.58f, 0.82f, 0.95f);
        private static readonly Color JSON_STRING_COLOR = new(0.81f, 0.65f, 0.47f);
        private static readonly Color JSON_NUMBER_COLOR = new(0.71f, 0.84f, 0.66f);
        private static readonly Color JSON_BOOL_NULL_COLOR = new(0.78f, 0.57f, 0.82f);
        private static readonly Color JSON_BRACKET_COLOR = new(0.86f, 0.86f, 0.67f);
        private static readonly Color JSON_PUNCTUATION_COLOR = new(0.6f, 0.6f, 0.6f);

        private static readonly Regex JSON_TOKEN_REGEX = new(
            @"(?<indent>^\s+)|" +
            @"(?<key>""(?:[^""\\]|\\.)*"")(?=\s*:)|" +
            @"(?<string>""(?:[^""\\]|\\.)*"")|" +
            @"(?<number>-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)|" +
            @"(?<bool>true|false)|" +
            @"(?<null>null)|" +
            @"(?<bracket>[\[\]{}])|" +
            @"(?<punct>[,:])",
            RegexOptions.Compiled);

        private void DrawSyntaxHighlightedJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                EditorGUILayout.LabelField(json, EditorStyles.wordWrappedLabel);
                return;
            }

            var style = new GUIStyle(EditorStyles.textArea)
            {
                richText = true,
                wordWrap = true,
                font = EditorStyles.miniLabel.font,
                fontSize = EditorStyles.miniLabel.fontSize,
                padding = new RectOffset(6, 6, 6, 6)
            };

            var lines = json.Split('\n');
            var sb = new System.Text.StringBuilder(json.Length * 2);

            foreach (var line in lines)
            {
                var lastIdx = 0;
                var matches = JSON_TOKEN_REGEX.Matches(line);
                foreach (Match m in matches)
                {
                    if (m.Index > lastIdx)
                        sb.Append(EscapeRichText(line.Substring(lastIdx, m.Index - lastIdx)));

                    Color c;
                    if (m.Groups["indent"].Success) { sb.Append(m.Value); lastIdx = m.Index + m.Length; continue; }
                    else if (m.Groups["key"].Success) c = JSON_KEY_COLOR;
                    else if (m.Groups["string"].Success) c = JSON_STRING_COLOR;
                    else if (m.Groups["number"].Success) c = JSON_NUMBER_COLOR;
                    else if (m.Groups["bool"].Success || m.Groups["null"].Success) c = JSON_BOOL_NULL_COLOR;
                    else if (m.Groups["bracket"].Success) c = JSON_BRACKET_COLOR;
                    else c = JSON_PUNCTUATION_COLOR;

                    sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{EscapeRichText(m.Value)}</color>");
                    lastIdx = m.Index + m.Length;
                }
                if (lastIdx < line.Length)
                    sb.Append(EscapeRichText(line.Substring(lastIdx)));
                sb.Append('\n');
            }

            var content = new GUIContent(sb.ToString());
            var height = style.CalcHeight(content, EditorGUIUtility.currentViewWidth - 40);
            EditorGUILayout.SelectableLabel(sb.ToString(), style, GUILayout.Height(height + 10));
        }

        private static string EscapeRichText(string text)
        {
            // Unity rich text only needs < escaped
            return text.Replace("<", "<\u200B");
        }

        private static string ShortenUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            try
            {
                var uri = new Uri(url);
                return uri.PathAndQuery;
            }
            catch
            {
                return url;
            }
        }

        private static Color GetMethodColor(string method)
        {
            return method switch
            {
                "GET" => new Color(0.4f, 0.8f, 0.4f),
                "POST" => new Color(0.4f, 0.6f, 1f),
                "PUT" => new Color(1f, 0.8f, 0.3f),
                "PATCH" => new Color(0.9f, 0.6f, 0.2f),
                "DELETE" => new Color(1f, 0.4f, 0.4f),
                _ => Color.white
            };
        }

        private static Color GetStatusColor(NetworkRequestLog log)
        {
            return log.Status switch
            {
                RequestStatus.Pending => new Color(1f, 0.85f, 0.3f),
                RequestStatus.Success => new Color(0.4f, 0.8f, 0.4f),
                RequestStatus.Cancelled => new Color(0.7f, 0.7f, 0.7f),
                _ => new Color(1f, 0.4f, 0.4f)
            };
        }

        private class RequestEntry
        {
            public NetworkRequestLog Log;
            public RequestEntry(NetworkRequestLog log) => Log = log;
        }
    }
}

