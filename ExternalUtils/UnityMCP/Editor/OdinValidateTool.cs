// Author: František Holubec
// Created: 31.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.OdinValidator.Editor;

namespace EDIVE.UnityMCP.Editor
{
    [McpForUnityTool("odin_validate",
        Description =
            "Runs Odin Validator on assets, folders or scenes (Paths) and returns errors and warnings with asset, hierarchy and property paths. " +
            "Large folders take minutes and block Unity meanwhile; prefer specific folders. " +
            "Without Paths returns the current results of the main validation session (background validation).")]
    public static class OdinValidateTool
    {
        public class Parameters
        {
            [ToolParameter("Comma separated asset, folder or scene paths to validate. Empty = current main session results.", Required = false)]
            public string Paths { get; set; }

            [ToolParameter("Comma separated types: error, warning. Default both.", Required = false)]
            public string Types { get; set; }

            [ToolParameter("Only results whose message or path contains this text.", Required = false)]
            public string Contains { get; set; }

            [ToolParameter("Maximum results to return.", Required = false, DefaultValue = "50")]
            public int Count { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            var paths = (ToolParams.GetString(@params, "Paths") ?? "").Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
            var types = (ToolParams.GetString(@params, "Types") ?? "error,warning").Split(',').Select(t => t.Trim().ToLowerInvariant()).ToHashSet();
            var contains = ToolParams.GetString(@params, "Contains");
            var count = Math.Max(1, ToolParams.GetInt(@params, "Count", 50));

            try
            {
                if (paths.Count == 0)
                {
                    var profile = ValidationProfile.MainValidationProfile;
                    if (profile == null)
                        return new ErrorResponse("No main validation profile, pass Paths.");
                    using var handle = profile.ClaimSessionHandle();
                    return Report(handle.Session.Name, handle.Session.GetCurrentResults(), types, contains, count);
                }

                var items = paths.Select(p => p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ? ValidationItem.FromScenePath(p) : ValidationItem.FromAssetPath(p)).ToArray();
                using var session = new ValidationSession("MCP Validation", items);
                // Unloaded scenes would be opened one by one, only explicitly listed scenes are validated that way
                var openScenes = paths.Any(p => p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase));
                session.PopulateQueue(true, openScenes, queueGlobalValidators: false);
                session.ValidateQueuedUpWorkNow(false);
                return Report(session.Name, session.GetCurrentResults(), types, contains, count);
            }
            catch (Exception e)
            {
                return new ErrorResponse($"Validation failed: {e.Message}");
            }
        }

        private static object Report(string name, IEnumerable<PersistentValidationResult> results, HashSet<string> types, string contains, int count)
        {
            var issues = results
                .Where(r => r.ResultType is ValidationResultType.Error or ValidationResultType.Warning)
                .Select(r => new
                {
                    type = r.ResultType == ValidationResultType.Error ? "error" : "warning",
                    message = r.Message,
                    asset = r.DynamicObjectAddress.LatestAddress.AssetPath,
                    hierarchy = r.DynamicObjectAddress.LatestAddress.Hierarchy.ToString(),
                    property = r.Path,
                    validator = r.ValidatorType?.Name
                })
                .Where(r => types.Contains(r.type))
                .Where(r => string.IsNullOrEmpty(contains) || $"{r.message} {r.asset} {r.hierarchy} {r.property}".IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            return new SuccessResponse($"{name}: {issues.Count(i => i.type == "error")} errors, {issues.Count(i => i.type == "warning")} warnings",
                new {total = issues.Count, issues = issues.Take(count)});
        }
    }
}
