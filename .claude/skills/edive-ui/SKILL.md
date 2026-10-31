---
name: edive-ui
description: Runtime UI in EDIVE apps (uGUI and UI Toolkit). Use when building or changing app windows, dialogs, tablet/world-space UI, the UI library prefabs, SDF shapes, font-symbol icons, the view tree, tweens and visual presets, debug menu UI, keyboards, minimap or any in-app visual style decision.
---

# EDIVE runtime UI

## Source of truth
- The "EDive Design" artifact (claude.ai design canvas) holds all visual data: `UiFoundations` (tokens), `UiButtons`/`UiInputs`/`UiSelection`/`UiContainers`/`UiLibraryPrefabs` (library), `Win2_*`/`Win3_*` (app windows), `Hub_*` (ServiceHub web). Read the relevant board before designing. Some boards can be outdated; check against Unity and the newest boards.

## Style
The current style guide: defaults and best practice, not fixed rules. The design artifact wins when it differs.
- Tailwind Slate, Noto Sans, Material Symbols Rounded, units = Unity UI units, no outlines. Core 3D props (tablet, keyboard, generic interactables) follow the same palette with flat faces and small radii; environment props have their own look.
- Colours, type scale, radii and spacing: the `UiFoundations` board. In Unity colours come from the style preset (`UILibrary/Style/UIStyle_Slate`) through the `UI*ColorID` visual IDs; use the IDs, not hex values.
- Widget/app icons: pastel-300 backgrounds with slate-800 glyphs.
- Icons: Material Symbols Rounded **Filled** through `FontSymbolTMPTextUI`. Don't put icon glyphs in plain TMP texts relying on font fallback. Prefer font symbols and SDF shapes over sprites; sprites for logos, photos and other real images.
- Shapes: `SDFGraphic` for panels, frames, circles and thin dividers (an `Image` snaps sub-pixel and lines look uneven). Canvases need `additionalShaderChannels` for SDF.
- Chips, search fields, buttons and filter chips are rounded squares (radius ~8). Full radius only for switches and avatars. Avatars: pastel background, dark initials.
- Cards inside a window use `UISurfaceRaised` (`UISurface` is the window colour). Fields always have a background, inset Surface colour inside raised panels.
- Semi-transparent world-space backgrounds look greyer than expected (linear blending); mind that when using them.
- Contrast between parts stays subtle. "Sharp" means a crisp edge with a small radius, not no radius.

## Copy and states
- Short labels and pictograms; descriptions go to tooltips. Keep localization keys few and universal ("service is disabled", "accept") instead of one per feature. Say "app"/"device", not "headset".
- The app runs on headset, desktop and mobile (landscape). Touch has no hover: irreversible or exposing actions need visible text, not tooltip-only icons.
- One universal toggle visual: same icon, slashed when off.
- Prefer disabling controls over hiding them. Status badges only while they apply.
- Empty lists: short label ("No servers found"), no spinner. Loading: skeletons or the library `Spinner`. Count chips show "–" while loading.
- Long text: single line with ellipsis or `TextMarquee`. Touch has no hover, so anything hover-triggered needs another trigger on mobile. Long IDs: middle ellipsis plus tooltip.
- Confirmation popups for destructive actions. Close X always top right. Popups are small anchored dialogs rather than full-screen overlays.
- Service/network requirements: the shared `LockedOverlay` prefab with modes rather than hiding the window. Order modes so the thing the user can't fix shows first. Windows that still partly work (LAN join) get an inline note instead.
- Errors: inline message plus red outline under the field; dialog popups for full server, version mismatch, could not connect.

## Building UI
- Build from the UI library prefabs in `_Shared/Entities/UILibrary` (instances or variants, strongly preferred over unpacked copies, which lose library updates; no prefabs inside EDIVE). The library is hand-made prefabs; new reusable prefabs go into `UILibraryDemoScene` (paged demo, no labels).
- New windows start from existing window/dialog prefabs and the framework standards (`AWindow`, `WindowDefinition`, `ScreenRoot`, `ViewSwitch`). Stateless preloaded apps use Close On Minimize.
- View tree: groups link inactive children on Awake; spawned content uses `ViewChildrenLinker` or `instance.LinkViewTree()` instead of rescanning later. Pooled windows reset on close through `ResetView()`/`OnViewReset`, not hard-coded component searches. Overlays, backdrops and dimming go through the view system.
- Use EDIVE state building blocks (`AToggleState`, `AMultiState`, VisualPresets) instead of raw GameObject toggling. Drive them from code when code knows the state; no `ICondition` plumbing for that. Default states apply immediately on Awake. Live style changes must recolour everything (`RefreshVisuals`).
- Library `Text_*` prefabs carry a `VisualPresetApplier` colour: change the colour ID, not the text colour.
- `FontSymbolTMPTextUI` sizes from its rect: inside layout groups give it a fixed `LayoutElement`.
- Dynamic fallback fonts: Clear Dynamic Data On Build on, atlas 512–1024 with multi-atlas, so glyphs added while testing don't ship or dirty the asset in git. Keep the padding to sampling size ratio (~10%) the same across a font and its fallbacks, or glyph weight differs within a line.
- Fonts created from script: add the atlas textures and material as sub-assets (`AddObjectToAsset`) and check with `LoadAllAssetsAtPath`; `fontAsset.material != null` is true even when the material wasn't saved.
- uGUI keeps plain `Button.onClick`; `IActivation` is for world-space or script triggers.
- Toasts/dialogs: extend the shared prefab when the change fits everyone; give a feature its own variant when it would bloat the shared one.
- Reuse the existing transition presets (Button/Toggle/Slider/Tab). Tweens must die with their object (`SetLink` or kill in `OnDestroy`).
- After converting `Image` to SDF, re-point every tween/reference slot (e.g. `SDFArc` fill) and bake instance-override colours into the base prefab first.

## World-space and XR UI
- World canvas inside a 3D frame: the opening matches the canvas size exactly, the canvas sits slightly in front of the screen. No screen material where an opaque canvas covers the opening.
- Grabbable panels (tablet, keyboard, TV) get four edge colliders so the canvas stays clickable.
- World-space UI Toolkit (debug menu on the tablet): active `PanelInputConfiguration`, same plane as the tablet canvases with higher sorting order, non-trigger collider. UI Toolkit dropdown popups attach to the panel root, style them at panel level.
- Depth problems: first check missing materials and back-face culling. Try render queues and depth first; stencil/RenderObjects/layer tricks only when those can't solve it. Check existing patterns first (hands in `ControlsManager` use DepthOnly + transparent materials).
- VR is stereo: no screen-space tricks, anything drawn must work per eye (single-pass instanced). Spatial indicators are small world canvases (fine if they rarely rebuild and have no raycaster).
- Reuse the project's own prefabs and animations (LoadingIndicator with `LoadingCircleTweenPreset`, IconCircle, visual preset IDs) before building new ones.

## Mockups and checking
- Mock in the shared design canvas artifact before building when the design is open; ask for its link if you don't have it, and make a new artifact if you can't access it. Mock only what the code can draw; read the real prefab layout before drawing.
- Render new UI prefabs and show them (preview scene, ScreenSpaceCamera canvas into a RenderTexture; world canvases switched to camera space; force a layout pass). Check the capture camera culling mask. Confirm suspicious render artifacts in the Game view.
