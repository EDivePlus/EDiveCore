---
name: unity-mcp-workflow
description: How to work in Unity through the Unity MCP and Rider MCP tools. Use for any Unity editor work: running code in the editor, compiling, play mode tests, prefab/scene edits, renders and captures, multiplayer tests with ParrelSync clones, package updates, crashes and relaunching Unity.
---

# Working in Unity through MCP

Setup: MCP for Unity (CoplayDev) package `com.coplaydev.unity-mcp` and the `mcpforunityserver` Python server on the same version; after updating one, update the other (server pin in the user's Claude config). `execute_code` compiles modern C# when Roslyn is installed per machine (`scripts/install-roslyn.ps1` into `%LOCALAPPDATA%\UnityRoslyn`, loaded by EDIVE `ExternalRoslynLoader` on the next domain reload); keep it out of the project. Without it it falls back to C# 6 (no local functions, no `default` literal).

## EDIVE tools (`EDIVE/ExternalUtils/UnityMCP`)
Parameters are in each tool's description; this is when to use them.
- `capture_editor_window`: look at any editor window, also while Unity is in the background.
- `console_latest`: instead of `read_console`, which returns the oldest entries.
- `odin_layout`: check inspector alignment by numbers instead of eyeballing captures. Compare rows with each other; the drawn field edge is ~3px right of the values.
- `odin_validate`: after changing validated data or validators. Big folders take minutes and block Unity.
- `edive_play`: play from RootScene, host and join. Host/join need `loaded: true`; right after `stop_network` they answer "still stopping", retry.
- New tools: a static class with `[McpForUnityTool("name", Description = ...)]`, `HandleCommand(JObject)`, a nested `Parameters` class with `[ToolParameter]` properties; read args through `ToolParams` (the server sends PascalCase names). They register live after a recompile.

## Blocking dialogs
- A modal window freezes Unity's main thread, so every MCP call times out (Windows still reports Unity as responding). On any timeout run `scripts/unity-dialogs.ps1` (PowerShell, Windows only).
- It reports `blocked=True` when the editor's main window is disabled, which happens for every modal (native dialogs and Unity's own modal windows), and lists the blocking windows with their buttons.
- Native dialogs: `-Title "<title>" -Button "<button>"`. Any window: `-Capture` saves a PNG to read it (Unity's own windows have no readable buttons), `-ClickAt x,y` clicks a point in that capture, `-Close` closes it. Unity paints its own modal windows only in the foreground, so `-Capture` brings the window to the front first (it takes focus on the user's desktop).
- Known ones: "Missing Signature" after OpenUPM package changes (a notice, close it), save-modified-scenes prompts, move/delete failures, "Recovering Scene Backups" at startup after a crash (native Yes/No: Yes copies the backups to `Assets/_Recovery/`; prevent it by deleting Temp before relaunching), the bug reporter after a crash (`UnityBugReporter` process, kill it). A "Hold On" progress window means Unity is busy: wait, don't close it. Ask before choosing a button that discards work.

## execute_code
- Needs `action: "execute"`. With Roslyn installed (see Setup) it takes modern C#; without it write C# 6. Check which one by trying a local function once. `Object` is ambiguous (System and UnityEngine are both imported): write `UnityEngine.Object`.
- Paste the full snippet every time; replaying history by index can run a different snippet. Keep objects between calls with `AppDomain.SetData/GetData`. Events like a locale change land a frame later, check them in a separate call.
- Bigger edits: `execute_code` with Roslyn or a custom tool. Without Roslyn write a temporary editor script, call it via reflection, delete it. The script runner blocks asset deletes; use `manage_asset` delete and confirm the files are gone on disk (it can report success and leave them).

## Compile and play mode
- Test edits in a prefab stage leave it dirty even after Undo; leaving the stage then shows a Save/Discard modal. Discard your own test edits, or test on a scene copy.
- After C# changes refresh, wait for compile, check `console_latest`. Clear-on-play is off, so old entries persist; clear the console before a check. For timing log to a file.
- A recompile ends play mode and Unity doesn't recompile during play. Never refresh/compile while the user is in play mode or watching a live test; do code work meanwhile and tell them to restart play.
- Stop play before prefab or scene edits. Don't edit prefabs with `LoadPrefabContents` during play (runtime material instances get baked in). Play-mode changes to ScriptableObject assets persist; tweaks through debug panels aren't marked dirty.
- Play from RootScene (`playModeStartScene`, EDive/Play Root Scene) to get the rig; restore the setting after tests. PlayMode tests need it cleared.
- Domain and scene reload are off: statics survive play sessions.
- XR features (tablet, keyboard, debug menu on the tablet) need XR simulator play mode. Test XR issues yourself with the XR Device Simulator before proposing fixes.
- EventSystem ignores input while the Game view is unfocused: focus it and queue real input states (`InputSystem.QueueStateEvent`).
- If a settings asset changed on disk while the editor runs: exit play, `ImportAsset(ForceUpdate)`, re-enter.
- The editor is throttled in the background: profiler/FPS numbers taken through MCP are meaningless.

## Checking visuals
- Render, look at the PNG, judge it against the reference before showing it.
- Editor windows: `capture_editor_window`. For an inspector: select the asset or GameObject, `ActiveEditorTracker.sharedTracker.ForceRebuild()`, collapse other components with `InternalEditorUtility.SetIsInspectorExpanded` (per component type, restore after), then capture. Runtime UI: preview scene + camera into a RenderTexture; world canvases switched to camera space; check the capture camera culling mask. Docs shots: temp additive scene, temp camera, dark background, clean up after.
- Confirm odd artifacts (doubled TMP glyphs) in the Game view before fixing them. When the user reports a visual issue, look at their Scene view or the imported data, not only your own render.
- For timing-sensitive checks run a probe that logs every 0.25–0.5 s and captures on state change.

## Scenes, prefabs, files
- Don't hand-edit .unity/.prefab/.meta. Saving a scene can reserialize unrelated objects: keep only your hunks, open content scenes additively. Removing overrides reserializes files; revert serialization-only changes. Use temp scenes for tests and generators, never overwrite an open scene with unsaved changes.
- Content scene not loaded: read object placement from the .unity YAML.
- Files: .cs = UTF-8 BOM + CRLF (match the file), shaders CRLF without BOM. Check changed code yourself: Rider `get_file_problems` on the files you touched and `console_latest`. Before a commit, or to catch files you didn't look at, run `scripts/check-changes.ps1` (Rider inspections on changed lines of all uncommitted .cs files plus Unity compile state; `-SinceMinutes` narrows it). Rider's own per-edit `quality-check` hook reformats whole files; if Rider adds it to a project's Claude settings, remove it. Write new code in the file's existing style (tabs vs spaces, brace layout).
- .cs created from bash may not be picked up; create files with the Write tool or Rider MCP (`create_new_file`) and refresh.

## Multiple editors and multiplayer
- Several Unity instances may be connected (other project, ParrelSync clone): `set_active_instance` or pass `unity_instance` on every call; it can vanish after a package reload. Never work in the clone by accident.
- Clones symlink Assets/Packages without file watching: after changes `AssetDatabase.Refresh(ForceUpdate)` + request compilation; after package updates force-reimport the package. Clones share PlayerPrefs (saved login): sign each in explicitly.
- Host/join with `edive_play`: `play`, poll `status` until loaded (log in first where needed), then `host` or `join_code`/`join_address`. Simulate host loss by pausing the host editor.
- After a package bump check the loaded DLL version, not only the manifest.
- Package Manager `Client.Add/Remove/List` finish on later editor ticks: start them in one call, check `packages-lock.json` in a later one. Waiting on `IsCompleted` inside a call deadlocks. Several packages at once: `Client.AddAndRemove` (one resolve).

## Crashes and hangs
- MCP timing out: run `scripts/unity-dialogs.ps1` first, then read Editor.log. The editor only ticks while something pokes it in the background, so `delayCall` work may not run until the next MCP call.
- Startup much longer than usual (normally under 5 minutes) with no Editor.log progress = stuck (a Library rebuild can take far longer, check the log). Kill Unity and its helpers (UnityCrashHandler64, UnityPackageManager, Unity.ILPP.Runner, UnityAutoQuitter, UnityBugReporter), delete the whole project Temp folder (incl. __Backupscenes) so no recovery dialog blocks, after package swaps also Library/ScriptAssemblies, relaunch yourself. Same for a ParrelSync clone's Temp. When you know what went wrong, fix it; ask the user only when unsure.
- Hanging again after "Begin MonoManager ReloadAssembly": look for assets created right before the crash (untracked in git status) and move them out. A half-created asset table or Addressables group from a crashed `execute_code` call does this; adding widely referenced assets (fonts) to localization asset tables from `execute_code` has crashed Unity.
- Stuck with 0% CPU after "Refreshing native plugins" (no progress, no dialog) = deadlock in editor init, usually an `[InitializeOnLoad]` / `AppDomain.AssemblyResolve` handler. Find it by disabling suspects and cold starting; warm domain reloads don't reproduce it. Resolve handlers should answer only their own assembly names.
- Wait for idle before heavy editor work; rebuilding during a compile can crash Unity.

## Learning more
- Unity MCP server (tool list, limits): https://github.com/CoplayDev/unity-mcp
- ParrelSync: https://github.com/VeriorPies/ParrelSync
