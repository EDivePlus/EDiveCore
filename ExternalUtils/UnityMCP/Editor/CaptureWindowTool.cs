// Author: František Holubec
// Created: 30.10.2026

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace EDIVE.UnityMCP.Editor
{
    [McpForUnityTool("capture_editor_window", Description = "Captures an editor window (Inspector by default) to a PNG, also while Unity is in the background. Returns the file path.")]
    public static class CaptureWindowTool
    {
        private const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public class Parameters
        {
            [ToolParameter("Window type name (InspectorWindow, SceneView, GameView, ...) or part of its title. Default InspectorWindow.", Required = false)]
            public string Window { get; set; }

            [ToolParameter("Output PNG path. Default Temp/McpCaptures/<window>.png in the project.", Required = false)]
            public string OutputPath { get; set; }

            [ToolParameter("Pixels to skip from the top.", Required = false, DefaultValue = "0")]
            public int FromY { get; set; }

            [ToolParameter("Maximum height of the capture in pixels. 0 = whole window.", Required = false, DefaultValue = "0")]
            public int MaxHeight { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            var windowName = ToolParams.GetString(@params, "Window");
            if (string.IsNullOrEmpty(windowName))
                windowName = "InspectorWindow";

            var window = FindWindow(windowName);
            if (window == null)
                return new ErrorResponse($"No open editor window matches '{windowName}'.");

            var outputPath = ToolParams.GetString(@params, "OutputPath");
            if (string.IsNullOrEmpty(outputPath))
                outputPath = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Temp", "McpCaptures", window.GetType().Name + ".png");

            var fromY = ToolParams.GetInt(@params, "FromY", 0);
            var maxHeight = ToolParams.GetInt(@params, "MaxHeight", 0);

            try
            {
                var size = Capture(window, outputPath, fromY, maxHeight);
                return new SuccessResponse($"Captured {window.titleContent.text}", new {path = outputPath, width = size.x, height = size.y});
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Capture failed: {e.Message}");
            }
        }

        public static EditorWindow FindWindow(string name)
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            return windows.FirstOrDefault(w => w.GetType().Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                   ?? windows.FirstOrDefault(w => w.titleContent.text.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static void RepaintImmediately(EditorWindow window, int times)
        {
            var host = typeof(EditorWindow).GetField("m_Parent", FLAGS)!.GetValue(window);
            var repaint = host.GetType().GetMethod("RepaintImmediately", FLAGS);
            for (var i = 0; i < times; i++)
                repaint?.Invoke(host, null);
        }

        // Grabs the window's own pixels, so overlapping windows and an unfocused editor don't matter
        private static Vector2Int Capture(EditorWindow window, string outputPath, int fromY, int maxHeight)
        {
            var host = typeof(EditorWindow).GetField("m_Parent", FLAGS)!.GetValue(window);
            var hostType = host.GetType();
            var rect = window.position;
            var width = (int) rect.width;
            var height = (int) rect.height;

            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false, true);
            try
            {
                RepaintImmediately(window, 8);
                hostType.GetMethod("GrabPixels", FLAGS)!.Invoke(host, new object[] {renderTexture, new Rect(0, 0, width, height)});

                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                RenderTexture.active = null;

                fromY = Mathf.Clamp(fromY, 0, height - 1);
                var cropHeight = maxHeight > 0 ? Mathf.Min(height - fromY, maxHeight) : height - fromY;
                var pixels = texture.GetPixels();
                var result = new Color[width * cropHeight];
                for (var y = 0; y < cropHeight; y++)
                {
                    // GrabPixels writes rows top-down, the output texture is bottom-up
                    var sourceRow = fromY + y;
                    Array.Copy(pixels, sourceRow * width, result, (cropHeight - 1 - y) * width, width);
                }

                var output = new Texture2D(width, cropHeight, TextureFormat.RGB24, false, true);
                output.SetPixels(result);
                output.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                File.WriteAllBytes(outputPath, output.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(output);
                return new Vector2Int(width, cropHeight);
            }
            finally
            {
                RenderTexture.active = null;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
