#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EDIVE.EditorUtils;
using EDIVE.OdinExtensions;
using EDIVE.Tweening.Segments;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening
{
    public static class TweenEditorUtils
    {
        private static GUIStyle _buttonStyle;
        private static GUIStyle ButtonStyle => _buttonStyle ??= new GUIStyle(SirenixGUIStyles.Button)
        {
            padding = new RectOffset(4, 4, 4, 4)
        };

        public static bool DetectCycle(this IParentTweenSegment node)
        {
            return DetectCycle(node, out _);
        }

        public static bool DetectCycle(this IParentTweenSegment node, out IEnumerable<IParentTweenSegment> path)
        {
            var visited = new HashSet<IParentTweenSegment>();
            var result = DetectCycle(node, node, visited);
            path = ((IEnumerable<IParentTweenSegment>) result)?.Reverse();
            return result != null;
        }

        private static List<IParentTweenSegment> DetectCycle(IParentTweenSegment node, IParentTweenSegment root, HashSet<IParentTweenSegment> visited)
        {
            visited.Add(node);
            foreach (var child in node.GetChildSegments().OfType<IParentTweenSegment>())
            {
                if (!visited.Contains(child))
                {
                    var cycle = DetectCycle(child, root, visited);
                    if (cycle != null)
                    {
                        cycle.Add(node);
                        return cycle;
                    }
                }
                else if (child == root)
                {
                    return new List<IParentTweenSegment>{node};
                }
            }
            return null;
        }

        public static List<ITweenSegment> ConvertToDirectSegments(this IEnumerable<ITweenSegment> segments, IDictionary<TweenObjectReference, Object> targets)
        {
            var result = new List<ITweenSegment>();
            foreach (var segment in segments)
            {
                if (segment is IPresetTweenSegment presetSegment && presetSegment.TryConvertToDirectSegment(out var directSegment, targets))
                {
                    result.Add(directSegment);
                }
            }
            return result;
        }

        public static List<ITweenSegment> ConvertToPresetSegments(this IEnumerable<ITweenSegment> segments, IDictionary<Object, TweenObjectReference> references)
        {
            var result = new List<ITweenSegment>();
            foreach (var segment in segments)
            {
                if (segment is IDirectTweenSegment directSegment && directSegment.TryConvertToPresetSegment(out var presetSegment, references))
                {
                    result.Add(presetSegment);
                }
            }
            return result;
        }

        public static IEnumerable<ValueDropdownItem<ITweenSegment>> GetAllSequenceSegments(InspectorProperty property)
        {
            if (property.HasParentObject<ITweenPreset>())
            {
                return TypeCacheUtils.GetDerivedClassesOfType<IPresetTweenSegment>()
                    .Select(t => new ValueDropdownItem<ITweenSegment>(t.LabelName, t));
            }

            return TypeCacheUtils.GetDerivedClassesOfType<IDirectTweenSegment>()
                .Select(t => new ValueDropdownItem<ITweenSegment>(t.LabelName, t));
        }

        public static void DrawSavePresetToolbarButton(List<ITweenSegment> segments, InspectorProperty property)
        {
            if (SirenixEditorGUI.ToolbarButton(FontAwesomeEditorIcons.FloppyDiskSolid))
            {
                if (property.HasParentObject<ITweenPreset>())
                {
                    EditorHelper.ExecuteNextFrame(() =>
                    {
                        var result = OdinExtensionUtils.CreateNewInstanceOfType<TweenAnimationPreset>();
                        if (result == null) return;
                        result.Initialize(segments);
                        EditorUtility.SetDirty(result);
                        EditorGUIUtility.PingObject(result);
                    });
                }
                else
                {
                    var window = EditorWindow.GetWindow<TweenPresetCreatorWindow>(false, "Tween Preset Creator");
                    window.Initialize(segments);
                }
            }
        }
          
        public static void DrawControls(ITweenAnimationPlayer player)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (ToolbarButton(FontAwesomeEditorIcons.ForwardSolid, "Play on Repeat"))
            {
                PlayRepeat();
                void PlayRepeat()
                {
                    player.Rewind();
                    var tween = player.Play();
                    tween.SetLoops(0);
                    tween?.OnComplete(PlayRepeat);
                }
            }

            if (ToolbarButton(FontAwesomeEditorIcons.PlaySolid, "Play"))
            {
                player.Play();
            }
            
            GUIHelper.PushGUIEnabled(player.IsPlaying || player.IsPaused);
            if (ToolbarButton(FontAwesomeEditorIcons.PlayPauseSolid, "Pause/Resume"))
            {
                if (player.IsPlaying)
                {
                    player.Pause();
                }
                else
                {
                    player.Resume();
                }
            }
            GUIHelper.PopGUIEnabled();

            if (ToolbarButton(FontAwesomeEditorIcons.StopSolid, "Kill and Rewind"))
            {
                player.Rewind();
                player.Kill();
            }

            if (ToolbarButton(FontAwesomeEditorIcons.SquareXmarkSolid, "Kill"))
            {
                player.Kill();
            }

            GUIHelper.PushGUIEnabled(player.IsInitialized);
            if (ToolbarButton(FontAwesomeEditorIcons.BackwardFastSolid, "Rewind"))
            {
                player.Rewind();
            }
            if (ToolbarButton(FontAwesomeEditorIcons.ForwardFastSolid, "Complete"))
            {
                player.Complete();
            }
            GUIHelper.PopGUIEnabled();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static bool ToolbarButton(EditorIcon icon, string tooltip)
        {
            return GUILayout.Button(GUIHelper.TempContent(icon.Highlighted, tooltip), ButtonStyle, GUILayout.Width(45), GUILayout.Height(22));
        }
    }
}
#endif
