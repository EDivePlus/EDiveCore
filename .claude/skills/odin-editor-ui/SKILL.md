---
name: odin-editor-ui
description: Rules and know-how for Unity editor UI with Odin Inspector in EDIVE projects. Use whenever writing or changing inspectors, Odin drawers, attribute or property processors, resolvers, facades, OdinEditor/OdinEditorWindow, editor windows, validation attributes, or rendering an inspector to check it.
---

# Odin editor UI

Inspectors in EDIVE are Odin, in EDIVE style.

## Before writing code
- Don't guess Odin APIs. Check the Odin source if available: look for an `OdinSourceCode` project next to this one (`../OdinSourceCode`, path inside `Assets/Plugins/Sirenix/Odin Inspector/Source`). Not found: ask the user where it is, it may not exist on this machine. Without it, use the Odin DLLs via Rider symbol search and the Odin docs.
- The source project can be older than the installed Odin. If an API is missing there, check the project's DLLs before ruling it out, or ask the user to update the source project.
- Odin modules ship as source in this project: `Assets/LicensedPlugins/Sirenix/Odin Inspector/Modules/` (e.g. Unity.Localization).
- Look at how an existing EDIVE inspector or window does the same thing (BuildTool, AppLoading, Tweening) for inspiration.
- Check every inspector change visually before calling it done, compared with the original when replacing one (capturing an inspector: `unity-mcp-workflow` skill). To capture with an Odin property expanded, find the `OdinEditor` in `ActiveEditorTracker.sharedTracker.activeEditors` and set `State.Expanded` on the property.

## Pick the lightest tool
1. Attributes on your own type.
2. Type you don't own, small tweaks: `OdinAttributeProcessor<T>` adds attributes to its members.
3. Type you don't own, different shape: `OdinFacade<T>` (`OdinExtensions/Editor/OdinFacade.cs`). `[FacadeMember]` / `[FacadeMember("m_name")]` binds a facade member to a `Target` member (private or internal type ok), write a proxy property only when it needs logic. `Target` is never null. Covers subclasses of T; to share members make the base facade generic (`BaseFacade<T> : OdinFacade<T> where T : Base`). `[ShowInGroupHeader]` puts a member on the foldout row. For a whole asset: empty `[CustomEditor(typeof(T))] class : OdinEditor {}` + facade.
4. Drawer last. Keep it small: draw `Property.Children`, `CallNextDrawer`. Inside it prefer EDIVE helpers, then Sirenix (`SirenixEditorGUI`, `SirenixEditorFields`), and Unity's `EditorGUI` only as a last resort.
- A separate `PropertyTree` over a view object inside an editor is rarely needed; prefer a facade.
- Writes through `ValueEntry.SmartValue` are recorded by Odin (undo, dirty). Writes that bypass it (raw `EditorGUI` on the target, changing the object directly) need `Property.MarkSerializationRootDirty()` or `Undo.RecordObject` + `EditorUtility.SetDirty`. Same for other Unity objects you touch.
- Native Unity subclasses and types whose editor you can't change: `NativeWrapperOdinEditor`.

## EDIVE components first
EDIVE attributes extend or replace Odin ones and fix several Odin limits. Before applying an Odin gotcha below, check whether the Enhanced version already solves it: read its attribute class in `OdinExtensions/Attributes`.
- Groups: `EnhancedBoxGroup`/`EnhancedFoldoutGroup` (spacing, colour, header members via `ShowInGroupHeader`).
- Lists and tables: `EnhancedTableList` (title bar, custom add/remove, paging, column widths), `CompactList`, `InlineList`, `ListItemSelector`, `EnhancedDictionary`.
- Objects: `EnhancedInlineEditor`, `EnhancedInlineProperty`, `EnhancedTypeSelector` (also closed generics), `EnhancedObjectDrawer`, `EnhancedValueDropdown`.
- Info and checks: `EnhancedInfoBox`, `RequiredIf`, `EnhancedValidate`. Display: `ShowAsString`, `IconLabelText`, `GUIBackgroundColor`, `PasswordField`.
- GUI helpers in code: `OdinExtensionUtils` (e.g. `ToolbarIconButton` with tooltip), `EditorIconsUtility`.
- Options change; read the attribute class for what it supports.
- `[Tooltip]`/`[PropertyTooltip]` automatically get an (i) icon next to the label (`TooltipIconAttributeDrawer`), so skip tooltips on self-explanatory fields.
- Dropdowns: `EnhancedValueDropdown`. Expensive getter: `DontReloadOnInit = true`.
- Polymorphic: `EnhancedTypeSelector`. For types you don't own add it with a processor on list elements (`property.Parent?.ChildResolver is ICollectionResolver`). Different types are just different types, no custom row drawers or conversions.
- Buttons: `InlineIconButton` next to a field (icon can be `"$Member"`), `IconButton`. Icons: `FontAwesomeEditorIcons`.
- List title bar: `ListDrawerSettings(OnTitleBarGUI = nameof(X))` + `OdinExtensionUtils.ToolbarIconButton(FontAwesomeEditorIcons.X, "Tooltip")`. Before writing a GUI helper, search `OdinExtensions` (`OdinExtensionUtils`, `EditorIconsUtility`) for one.
- Tables: `EnhancedTableList` only when rows have the same shape.
- Validation: `[EnhancedValidate(nameof(Method))]`, `Method(SelfValidationResult result)` or `Method(T value, SelfValidationResult result)`; a string name when the method is editor-only in a runtime class. Prefer it over `ISelfValidator`, which is clunky; most checks are about one field anyway.

## Gotchas
- `[ShowInInspector]` getter-only property draws disabled, children too. Lists: use a field, refresh it with `[OnStateUpdate(nameof(Refresh))]`. Display-only text: `[EnableGUI]` + `[DisplayAsString]`.
- Order follows the code, but fields draw before properties and methods. Arrange the code first; use `[PropertyOrder]` only where that isn't enough (negative values move up, equal values keep code order).
- Spacing around groups: use `SpaceBefore`/`SpaceAfter` on Enhanced groups. `PropertySpace` inside a plain `HorizontalGroup` spaces only that member and breaks the row's alignment; wrap the row in a `VerticalGroup(PaddingTop = x)` instead.
- Ideally group only related values in one row; don't squeeze unrelated fields together.
- Rows that pair a name with a value (locale + text, locale + table): use the name as the label (`[LabelText("$Name")]`), not a `HorizontalGroup` with a fixed-width name column. Odin lists shrink the label width by their padding, so the value then lines up with the rows outside the list.
- Sibling sections should use the same group type: a `TitleGroup` next to `EnhancedBoxGroup`s is 4px wider on both sides.
- Odin containers (lists, boxes) put their value column about 3px left of plain rows Unity draws at the same level. That's Odin's own behaviour, not worth working around per inspector.
- Unity `[TextArea]` under Odin is at least 2 lines tall with the label above. Odin's `[MultiLineProperty(lines)]` keeps the label beside it with a fixed height. One line: plain string field.
- Resolvers: highest `[ResolverPriority]` wins. Odin's localization module hides `LocalizedReference` children with its own resolver.
- Asset lookups in dropdowns: filter by name in `AssetDatabase.FindAssets` before loading, and pass search folders (`new[] {"Assets"}` or narrower) unless read-only package assets are wanted. Check `HasProperty` before reading material textures, `mainTexture` logs errors otherwise.

## Writing drawers
- Generic drawer with a constraint (`where T : Base`) covers subclasses, a concrete `OdinValueDrawer<Base>` only the exact type. Constrain tightly, `CanDraw*` filters are slower.
- Create value/action resolvers in `Initialize`, read them in `DrawPropertyLayout`, always `ValueResolver.DrawErrors(...)`. Literal-or-member strings: `ValueResolver.GetForString`. A failed resolve returns `default` (a `Color` becomes transparent), check `HasError`.
- Foldouts use `Property.State.Expanded` (persisted), not a drawer field. Hide/disable with `[OnStateUpdate]` or `ShowIf/EnableIf`. Setting `State.Enabled = true` can't re-enable a member inside a disabled parent; use `[EnableGUI]`/`GUIAlwaysEnabled` for that.
- Groups hide themselves when all children are hidden.
- Clamping on edit is fine (`MinValue`, `PropertyRange`). Other fixes in a drawer only run while inspected: report those with `EnhancedValidate`.
- Assigning `ValueEntry.SmartValue = EditorGUI.XField(...)` every frame is the normal Odin pattern; it only records a change when the value differs. Use `BeginChangeCheck`/`EndChangeCheck` when the change has side effects (callbacks, writing other objects).
- Inside a drawer prefer one `GetControlRect` split with Odin's `Rect` extensions (`AlignRight`, `SubXMax`, `SplitVertical`) or layout groups, whichever is simpler. Layout groups can misbehave with expanding heights.

## More gotchas
- Prefer Odin attributes and Sirenix helpers (`SirenixEditorGUI.DrawSolidRect`, `WarningMessageBox`) over custom drawers and raw `EditorGUI`. When replacing a drawer keep every feature it had (popups, pickers).
- Each `InlineIconButton` adds width; several overflow. Put secondary actions in a popup.
- Read-only `ShowInInspector` members draw disabled, buttons too: use `GUIAlwaysEnabled` / `[EnableGUI]`.
- Odin's `[TypeRegistryItem]` doesn't apply to closed generics; use `EnhancedTypeSelector(UseGenericRegistryItems = true)`.
- New tool windows are separate classes; extract shared parts (e.g. `EditorIconGrid`) instead of extending classes made for something else. Script icons: built-in Unity icons via `MonoImporter.SetIcon`.
- Inspector warnings only when relevant (conditional on the module or setting in use).
- Hide members for missing or conditional features; an action that is just unavailable right now stays visible and disabled.
- `EnhancedBoxGroup`/`EnhancedFoldoutGroup` header buttons are centred with FlexibleSpace; keep that when touching header layout and check it with a zoomed capture.
- Measure alignment instead of eyeballing: the `odin_layout` MCP tool (EDIVE UnityMCP) lists each row's value column, right edge, label width and indent, and groups rows by value column. Run it after a layout change, alongside the capture. How it works, for other windows: a probe `OdinDrawer` (`[DrawerPriority(0, 0, 2)]`) records `GUIHelper.BetterLabelWidth`, `EditorGUI.indentLevel` and the `GUIHelper.BeginLayoutMeasuring()`/`EndLayoutMeasuring()` rect on Repaint. `InspectorProperty.LastDrawnValueRect` covers the whole row including label and buttons. Confirm a suspected offset on the captured pixels.
- Odin foldouts can also be toggled for captures with `SendEvent` plus a repaint loop; Odin remembers foldout state.

## Processors and resolvers
- Attribute and property processors run once, when the tree is set up. Child properties don't exist yet inside them. To re-run after a value change call `$property.RefreshSetup()` (e.g. `[OnValueChanged("@$property.RefreshSetup()")]`) on the closest common parent, groups are properties too.
- Deterministic processors (same input, same output): mark them `[OdinCacheableProcessor]` so Odin caches the result.
- Attributes on a collection cascade to its elements unless the attribute class has `[DontApplyToListElements]`.
- Forwarding attributes to a child member moves their resolver context too. A forwarded `ValueDropdown("Getter")` then resolves on the child type, rewrite the string or don't forward it.
- `OdinPropertyProcessor`: `propertyInfos.AddValue<T>(name, getter, setter, attributes)` adds a virtual member. `[HideInInspector]` removes the InspectorProperty completely; to hide but keep it reachable use `[HideIf("@true")]`.
- `@` strings are plain C#: no lambdas, LINQ or extension methods. Named values: `$property`, `$value`, list callbacks also `$index`. A bad string's error lists the available names. Useful: `"@$property.ParentValueProperty.NiceName"`, `CustomAddFunction = "@new MyType()"`, or a static method taking `$property`.
- `[Button]` methods can take an `InspectorProperty` parameter (with `DisplayParameters = false`), `property.SerializationRoot` is the asset.

## Drawing
- Wrap a drawer: value drawer with higher `[DrawerPriority]` + `CallNextDrawer`. Draw Unity's own PropertyDrawer: `EditorGUILayout.PropertyField(Property.Tree.GetUnityPropertyForPath(Property.UnityPropertyPath), label, true)`.
- `[ShowDrawerChain]` shows which drawers run and in what order. First thing to check when an attribute does nothing.
- Unity `[Min]` (and other attributes with Unity property drawers) take over the field and break Odin attributes on it. Use `[MinValue]`, `[MaxValue]`, `[PropertyRange]`.
- A type with its own custom editor (NetworkBehaviour, many packages) is not drawn by Odin. Empty `[CustomEditor(typeof(T), true)] class : OdinEditor {}` takes it back. Odin Preferences > Editor Types shows who draws what.
- `[InlineEditor]` is for Unity objects only, plain classes use `[InlineProperty]`/`[HideLabel]`/`[HideReferenceObjectPicker]`. `InlineEditor` and `ValueDropdown` don't combine (check `EnhancedInlineEditor`/`EnhancedValueDropdown` first).
- `TableList` rows can't be Unity objects and it ignores most `ListDrawerSettings`; use `EnhancedTableList` for title bar, add/remove and paging.
- Rects aren't known before layout. To frame or tint a property wrap it: `PushColor`, `SirenixEditorGUI.BeginBox()`, `PopColor`, `CallNextDrawer`, `EndBox`.
- `[OnValueChanged]` fires on every undo/redo (`InvokeOnUndoRedo = false` to stop) and only for inspector edits. List element edits need `IncludeChildren = true`. Use it for simple reactions, control the write with a processor property when it matters.
- `[Delayed]` commits text on Enter. `EditorUtility.Open*Panel` from a button: run it in `EditorApplication.delayCall`.
- TabGroup tab order comes from the lowest `PropertyOrder` of each tab's members.
- Own `PropertyTree`: wrap the drawn value in a holder object (a list as target becomes multi-target) and `Dispose()` the tree, Odin logs leaks otherwise. In UI Toolkit host it in an `IMGUIContainer`.
- Slow inspectors: profile first. Usual causes are heavy `OnInspectorGUI` code, huge lists with many `ShowIf`, or a Validator profile that includes scene asset dependencies (turn that off on the profile used while working).

## Serialization and setup
- Avoid the Odin serializer (`SerializedMonoBehaviour`/`SerializedScriptableObject`, `[OdinSerialize]`): it's heavy and may be removed. Use Unity serialization with `[SerializeReference]`.
- Where it's still used: it doesn't run constructors, new fields come in as default. Initialize in `[OnDeserialized]`. Odin serialization on prefabs is unsupported. `[Serializable]` types are serialized by Unity, not Odin. `[SerializeReference]` doesn't support structs.
- Odin installed outside `Assets/Plugins/Sirenix` (e.g. `LicensedPlugins`): a plain package import updates known files in place by GUID, but new files land in `Assets/Plugins/Sirenix`. That includes Validator's `*_tmp` DLLs, which `EnsureCorrectOdinVersion` only swaps in at the real install path, so Validator silently stays old. Either copy the package files to the real path yourself (`_tmp` files under their final names) or move the stray files there before Unity reloads.
- After updating Odin also update its modules (Preferences > Modules) and keep Validator on the same version. Compile errors in module code after an update usually mean stale modules.
- Keep the `ODIN_INSPECTOR*` defines. Public repos can ship only `Sirenix.OdinInspector.Attributes.dll`.

## Learning more
- Odin tutorials (drawers, processors, resolvers, property states, attribute expressions): https://odininspector.com/tutorials
- Odin API reference: https://odininspector.com/documentation and attribute overview: https://odininspector.com/attributes (also Tools > Odin > Inspector > Attribute Overview in Unity).
- Odin Serializer source (open source): https://github.com/TeamSirenix/odin-serializer
- Unofficial Odin FAQ: https://github.com/Schwapo/Unofficial-Odin-FAQ
- Small Odin extensions worth reading as examples: https://github.com/Schwapo (Odin-Resolved-Parameters-Overview lists all named values per attribute; ApplyToDictionary, ColoredFoldoutGroup, PageGroup, AddGroupAttribute, MemberOrder, IconButtonGroup, ObjectPreview).
- Odin community tools: https://odininspector.com/community-tools and https://github.com/My-Key (OdinHelpBox, CompactTabGroup, ShaderPropertyIDAttribute, DataDrivenEnum).
