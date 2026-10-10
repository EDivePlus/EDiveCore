# EDIVE map

Core Unity library shared by EDIVE projects. Unity 6, URP, Odin Inspector, PurrNet, XRI 3, DOTween, UniTask, R3, Newtonsoft.
Check here first before writing a helper. If EDIVE has it, use it.

## Code style
- File header: `// Author: <name>` + `// Created: dd.MM.yyyy`. UTF-8 BOM, CRLF.
- Namespace = folder, `EDIVE.<Module>...`. Abstract classes prefixed `A` (`AToggleState`).
- Fields: serialized `[SerializeField] private T _PascalCase;`, private `_camelCase`, consts and static readonly `UPPER_CASE`.
- Attributes on own lines above field. Target-typed `new()`, `var`.
- Comments and tooltips short and plain.
- Polymorphic config: `[SerializeReference]` + `[EnhancedTypeSelector]` (already on `IActivation`, `IAction`, `ICondition`).
- Renamed SerializeReference types: `[FormerlySerializedType]` + Tools/Serialized Type Migration, not `[MovedFrom]`.
- Odin drawers live in same file under `#if UNITY_EDITOR`. Native Unity subclasses get inspector via `NativeWrapperOdinEditor`.
- Static state reset with `[ClearOnReload]` / `[ClearOnAppRestart]`.

## App startup and services
- `AppCore` (Core): root singleton in RootScene. `AppCore.Services` = service locator (`Get/TryGet/AwaitRegistered/WhenRegistered<T>`). Services implement `IService`.
- Service bases: `AServiceBehaviour<T>` (register OnEnable), `ALoadableServiceBehaviour<T>` (async `LoadRoutine`, `PopulateDependencies`), `ANetworkServiceBehaviour<T>` (register on spawn).
- AppLoading: `AppLoaderController` in RootScene runs `LoadSetupDefinition` -> `LoadGroupDefinition`s (parallel) -> `LoadItemDefinition`s (prefab / addressable / serialized `ILoadable` sources, typed dependencies, `ICondition`) -> `ILoadFinalizer` (e.g. load scene).
- New manager: `ALoadableServiceBehaviour<T>` prefab -> `LoadItemDefinition` -> "Refresh Resolved Data" -> add to a group.
- Toolbar "EDive/Play Root Scene" plays from RootScene.
- `AUniqueDefinition` (AssetTranslation): SO with stable `UniqueID` from file name; sent over network / JSON as ID via `ADefinitionTranslator<T>`.

## Building blocks (Utils, Conditions, DataStructures, ScriptableArchitecture)
- `IActivation`: click/trigger source (`ButtonActivation`, `ColliderActivation`, `InputActionActivation`, `XRInteractable*Activation`, `CompoundActivation`).
- `IAction`: async `Execute()`.
- `ICondition`: `Evaluate` + `StateChanged`. Bases `ABoolCondition`, `AComparisonCondition<T>`, `CompositeCondition`. `ConditionalToggleTrigger` drives an `AToggleState` from a condition.
- `VariableField<T>`, `ToggleableField<T>`, `SerializedInterface<T>`, `SceneField`, `PlatformSpecificValue<T>`, `UGuid`, `UType`, `SerializableDictionary`, `UDateTime`/`UTimeSpan`.
- ScriptableArchitecture: `AScriptableVariable<T>` SOs (Value, ValueChanged), `ScriptableVariableField<T>`, `AScriptableList<T>`, assigner components.
- NativeUtils extensions: `Color.WithA`, `Vector3.WithY/XZ`, `Transform.DestroyChildren/GetPath`, `RectTransform.SetToFillParent`, `GetOrAddComponent`, `InvokeNextFrame/InvokeAfterTime`, `IsNullOrEmpty`, `Remap`, `PositiveModulo`, `RichText`.
- Utils: `RandomUtility` (RandomItem, Shuffle, weighted), `JsonUtils` (JsonCopy deep clone), `ColorPalette` SO, `ActiveDefinesRegistry`, `IdentifierUtility`.
- Utils components: `TwoBoneHingeIK`, `LockParentRotation`, `SmoothRestrictedRotateTowards`, `FPSDisplay`, `BoxColliderRendererFitter`.
- OdinExtensions attributes: `EnhancedBoxGroup/FoldoutGroup` + `ShowInGroupHeader`, `EnhancedValidate`, `RequiredIf`, `EnhancedTypeSelector`, `EnhancedInlineEditor`, `EnhancedValueDropdown`, `EnhancedInfoBox`, `EnhancedAssetList/Selector`, `EnhancedTableList`, `CompactList`, `InlineIconButton`, `IconButton`, `IconEnumToggleButtons`, `LayerField`, `TagSelector`, `SceneReference`, `ShowCreateNew`, `LogRange`, `KeepRefreshing`. Icons: `FontAwesomeEditorIcons`.
- EditorUtils: `EditorAssetUtils.FindAllAssetsOfType`, `DefinesUtility`, `SubAssetUtility`, `MainToolbarUtility`.

## UI
- Shapes: `SDFGraphic` (ProceduralUI). Use for nice shapes only (rounded rect, circle, pill, outline, shadow, arc), plain `Image` otherwise. `CornerRoundness.Circle/Uniform`, `GradientFill`, `FillMode.Filled/NoFill`. `Graphic.color` is the fill color, its alpha also fades effects with Use Graphic Alpha. Button tint and CanvasGroup tint/fade the whole graphic. Effects are sibling components drawn in the same mesh: `SDFShadow` (Outer/Inner), `SDFOutline`. `SDFArc` component cuts the shape to a sector. Canvas needs only TexCoord1+2.
- Icons: `FontSymbolTMPTextUI` + `FontSymbol(definition, char)`. Material Symbols in `Assets/_Shared/Entities/FontSymbols/` (`MaterialSymbols_Round_Filled`, `_Standard`). Dynamic TMP font.
- Selectables: `EnhancedButton`, `EnhancedToggle` (drives an `AToggleState` from `isOn`), `EnhancedKnob`. `SelectableAdditionalData` -> `TweenSelectableTransition` + preset for animated states.
- Other: `TabGroupController`/`TabHandler`, `AProgressBar`, `RadialLayout`/`RadialLayoutElement`, `RecyclableScroller`, `TooltipTrigger`/`TooltipManager`, `ColorPickerController`, `RoundedRectRaycastTarget`.
- MenuScreen: `MenuScreenController` service, `WidgetDefinition`, `WidgetView`, `MenuScreenFrame`.
- Localization: `SafeLocalizedString`, `EnhancedLocalizeStringEvent`, `LocalizeFont` + `LocalizedFontDefinition` (TMP texts need it, validator checks).

## State, tweening, visual presets
- `AToggleState` (bool): `ToggleState` (per-target enabled/disabled `AStateValuePreset` lists, Apply/Capture in inspector), `ToggleStateObjects` (SetActive on/off lists), `TweenToggleState` (tween in/out).
- `AMultiState` (string ID): `MultiState`, `MultiStateObjects`.
- `AStateValuePreset<TTarget,TValue>`: Graphic color/alpha, GameObject active, CanvasGroup, RectTransform, Transform, TMP text, Selectable interactable, Renderer material, `FontSymbolTMPTextUISymbolPreset`... New one: inherit, auto-discovered.
- Tweening: `TweenAnimationController` (component), `TweenAnimationField` (embed in own component), `TweenSequence` of `ITweenSegment`s, `ATweenObjectAction<T>`, `TweenAnimationPreset` SO with `TweenObjectReference` slots.
- VisualPresets: `VisualPreset` (data: ID -> value) applied by `VisualSwitcher` (view: ID -> target). IDs are `ABaseVisualID` SOs (Color, Sprite, String, Material, Prefab, MultiState, FontSymbol, UVDecal).

## Networking (PurrNet)
- `MasterNetworkManager` (Start Host/Server/Client), `NetworkServerManager` (server list, join), `NetworkSceneManager`, `NetworkPlayerManager` + `NetworkPlayerController`.
- Patterns:
  - Simple prop: one `NetworkBehaviour` with `SyncVar`s, server seeds in `OnSpawned(asServer)`, clients call `[ServerRpc(requireOwnership: false)]`, apply in `onChanged`.
  - Companion: local controller + `Network*` sibling (`NetworkFormController`, `NetworkStagePlayController`, `NetworkToggleState`). Unhook local events while applying remote state, no echo.
  - Owner state: `SyncVar<T>(ownerAuth: true)`.
- `NetworkToggleState` syncs any `AToggleState`.
- XR grab: `NetworkXRInteractable` (ownership to grabber, kinematic restore) + PurrNet `NetworkTransform`.
- Conditions: `NetworkOwnerCondition`, `NetworkConnectionStateCondition`, `NetworkRuntimeModeCondition`.

## XR and input
- `ControlsManager` service picks one `AControls` rig (`XRControls` or desktop/touch `UniversalControls`). Keep XR only features out of `ControlsManager`/`AControls`.
- `InteractionManagerProvider` service + `InteractionManagerAssigner` on interactables outside the rig.
- `ControllerInputActionManager`: interactor switching, `RequestThumbstickControl(requester)`.
- Grab transformers: `XRRotationGrabTransformer`, `ConstrainedGrab/XRConstrainedGrabTransformer` + `APoseConstraint` (`BoundsPlaneConstraint`, `TwoBoneHingeIKConstraint`).
- World UI: Canvas + `GraphicRaycaster` + `TrackedDeviceGraphicRaycaster` (or `FilteredTrackedDeviceGraphicRaycaster` + `UIInteractionLayer`), layer UI.
- ActionWheel: `ActionWheelController`, wedges `SingleActionWheelWedge`/`ToggleActionWheelWedge` (prefabs in `_Shared/Entities/ActionWheel`).
- Also: HandGestures, Tablet, Vignette (`VignetteHandle`), DeviceSimulator, `VirtualKeyboardManager`, touch `VirtualJoystick`.

## Features
- Avatars: `AvatarDefinition`, `AvatarController`, `ARigFollow` (`VRBodySolverRigFollow`), `NetworkAvatarPlayerController`.
- Audio: `AudioManager` (volume, mic, UniVoice over PurrNet broadcasts), `VoiceRecordingManager`.
- Replay: `ReplayController`, `ReplayAgentHandler`, record data via `AFrameSequenceComponent<TTarget,TData>`.
- Forms (questionnaires), StagePlay (teleprompter), EyeTracking (`EyeTrackingManager`).
- Environment: `SceneSetupManager` + `SceneSetupDefinition`, `ASceneSpawnPlace`.
- ServiceHub: backend (auth, `SaveDataService` with `ASaveDataObject`, lobby, remote content). RemoteContent: `RemoteContentManager`, `ARemoteContentHandler`.
- Http: `RestUtils`, `JwtUtils`. CredentialStore: OS secrets, `[CredentialField]`.
- AddressableAssets: `AssetAddressReference<T>`, `SceneAssetReference`.
- Rendering (URP): mirrors, UV decals, LitLayered, TriPlanar, grids, vignette shaders.
- Procedural: `MeshSliceScaler`, `SplineMeshBender`. GeoToolkit: geo coords, `MapController`, map services.
- BuildTool: Tools/Build Tool (Ctrl+G), `BuildPreset` = user + app + platform config, custom steps inherit `ABuildAction`.
- Configuration (`LocalConfigLoader`), Console (headless, `SPECTRE_CONSOLE`), View (`SafeAreaController`).
- External: third party code, don't edit.
