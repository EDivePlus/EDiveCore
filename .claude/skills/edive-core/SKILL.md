---
name: edive-core
description: How to use and extend the EDIVE core library well. Use when adding features, managers, components or assets in EDIVE or an EDIVE project (app loading, services, state building blocks, localization, ObjectScaler, shaders, tablet, XR rig, ServiceHub client, asset placement, code style, multi-project setup).
---

# Using EDIVE

The map of modules is in `EDIVE/CLAUDE.md`. This skill is how to work with them. These are defaults and preferences, not hard rules: when a case needs something else, say why.

## Design rules
- Suggest and discuss before changing EDIVE architecture or adding reusable systems. Building for future use is welcome; say what it's for.
- Single responsibility: new behaviour goes in its own component rather than growing an unrelated one. A small feature usually fits an existing manager or the component that owns it; add a new manager or service when it really is a separate system.
- Avoid statics, lazy binding and adding components at runtime (e.g. a `PhysicsRaycaster` from code); prefer setup in the scene/prefab. Use them when the case calls for it (procedural mesh caches use a static registry).
- Anything that depends on services, networking or other late-initialized parts: wire it in the loading pipeline (`LoadRoutine`/`PopulateDependencies`) or after spawn, not in `Awake`. `Awake` is fine for local setup.
- Several projects use EDIVE: don't break them. Migrations are fine when old data keeps working or gets migrated; otherwise leave old types in place marked `[Obsolete]`.
- Name systems by what they do (`ObjectScaler`, not "slicer"). Don't brand generic assets with "EDIVE".
- Return error results from calls, not "last error" properties.
- Polymorphic config: `[SerializeReference]` lists of small types when options carry data or logic (e.g. `ALoginProvider` subclasses); a plain enum is fine for a small fixed set.
- A ScriptableObject for a single value is fine; check first whether EDIVE has a better fit (`ScriptableVariable`, `VariableField`, `VisualPreset`). Once a variant is chosen, delete the switching code.

## Assets and folders
- EDIVE holds code and shaders only. Materials, textures and prefabs go to the project (`_Shared` or the project folder): projects can run on different Unity versions and assets in EDIVE would keep reserializing.
- Shaders: prefer existing shaders or extending them; new ones when needed. Prefer HLSL, since Shader Graph files are hard to read and edit as text. Keep URP Lit's CBUFFER layout for SRP Batcher. Keep shaders generic (texture x colour); palettes live in materials/UVs.
- Don't overwrite source art (fonts, source textures) in place; make a derived asset or ask.
- Keep generator scripts that will be rerun or iterated; delete true one-offs once their output exists.
- Small components go into an existing assembly (e.g. `EDIVE.Utils`) when one fits; a new asmdef for a module with its own dependencies.

## Systems
- App loading: new manager = `ALoadableServiceBehaviour<T>` + `LoadItemDefinition`. A dependency declared in `PopulateDependencies` only applies after "Refresh Resolved Data" on the asset. After loader changes exercise the validators, cycle detection and a full RootScene play.
- Services: `OnDestroy` overrides must call base, or unregistration is skipped. Never add `AppCore` to temp objects.
- New feature depending on ServiceHub goes under the ServiceHub module as one assembly and an optional module (`IServiceHubModule`), so projects without it still work.
- New animatable values: make the tween action and the state preset together (`ATweenObjectAction` + `AStateValuePreset`). Derived values (size from a synced texture) need no network sibling; each client computes them.
- `ObjectScaler` (`EDIVE.Procedural.ObjectScaling`) for sliced objects that resize (frames, tablet, keyboard, photos). Size them via material instances/`MaterialPropertyBlock`, not transform scale. Sliced shader: `DisableBatching`, no Contribute GI. After frame model changes verify photos/remote images (mesh cache must rebuild submesh topology).
- Renamed `[SerializeReference]` types: `[FormerlySerializedType]` + migration scanner, rename files with `git mv`, clean dangling prefab overrides.
- Never create Unity objects in static initializers of `[Serializable]` classes.
- Odin attributes on runtime fields must not use `nameof()` of an editor-only method (breaks player builds).
- UniTask: shared in-flight work uses `AsyncLazy`. `CancelAfter` fires on a thread pool thread, use `CancelAfterSlim(DelayType.Realtime)` when touching Unity API.
- DOTween: "Max Tweens reached" means the pools grew; set `SetTweensCapacity` at startup to the peak usage. Make sure tweens die with their object (`SetLink` or kill in `OnDestroy`). Discard `TweenAnimationField.Play()` with `_ =`.
- Domain reload is off: statics survive play sessions; `[ClearOnReload]` resets them.
- Logs are used in builds on purpose; never log tokens.
- The XR rig can't be swapped at runtime (restart needed). Tablet logic stays in `TabletController` (follows teleports while open and in view).

## Localization
- Locales are in the Localization settings; read them there. UI labels come from string events; dynamic text through `LocalizedString` (with variables) in code.
- `LocalizeFont` is a separate component, required only on string-event texts (validator). Keep a dynamic fallback font behind each static TMP font.
- Table edits from code: mark the table, the collection and `SharedData` dirty, call `LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(sender, collection)`, then `AssetDatabase.SaveAssets()`. Otherwise editor windows show stale data and unsaved entries are lost on close.
- An empty value in any locale shows the package's "No translation found" text in game. Before calling localization work done, check every touched key has a value in every table; report gaps instead of filling them with English.
- After assigning a `LocalizedString` from code, call `RefreshString()` on the string event. Listeners added with `UnityEventTools.AddPersistentListener` default to Runtime Only; set `EditorAndRuntime` so they preview in the editor (target a method like `set_text`, not a lambda).
- Fonts referenced from asset tables (per-locale fonts) must be Addressable and outside `Resources/`. A recreated font asset gets a new GUID: re-point the table and Addressables entries.
- A localization pass covers text set from code too: search for `.text =`, `.text +=` and `SetText("` with string literals.
- Unused-key checks must also search key IDs in prefab overrides. After removing keys locally, delete the rows in the Google sheet. Sheet sync: pull, check columns, push, pull again.

## Code style
- Follow the EDIVE code style (`EDIVE/CLAUDE.md`): fields plus properties, Odin attributes, extracted methods, extend `NativeUtils` instead of local helpers. UniTask, R3 and ZLinq are welcome.
- Self-explanatory field names, no tooltips on obvious fields; rename with `[FormerlySerializedAs]`. When keeping an odd workaround, add a short comment why.

## Multi-project
- Projects share code through EDIVE and a common project template; shared changes go back to where they are shared from. Pain points are per-project additions to shared lists (input actions, network prefabs, addressable groups, localization tables, load items); aim for single ownership with extension points.

## Learning more
- UniTask: https://github.com/Cysharp/UniTask · R3: https://github.com/Cysharp/R3 · ZLinq: https://github.com/Cysharp/ZLinq
- DOTween: https://dotween.demigiant.com/documentation.php
- Unity Localization: https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html
