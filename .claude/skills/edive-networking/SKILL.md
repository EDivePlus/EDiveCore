---
name: edive-networking
description: Multiplayer and streaming in EDIVE apps. Use when working with PurrNet (SyncVars, RPCs, transports, relay, join paths), networked props, XR grab and interaction, voice, LiveKit streaming/displays, or when testing host/client setups.
---

# EDIVE networking

## PurrNet
- Patterns are in `EDIVE/CLAUDE.md` (simple prop, companion, owner state). Server-only methods get PurrNet attributes or at least clear names.
- A SyncVar setter fires `onChanged` locally too: drive client events from `onChanged`, not RPC + SyncVar. On the host `ObserversRpc` also runs on the server; guard echo paths.
- Don't publish SyncVars in despawn/unbind paths (owner already gone). Publish on bind.
- Project uses PurrNet "Unsafe" rules (ownership ignored, anyone can despawn, players stay in scenes). Check this before calling something an RPC/ownership bug.
- Missing `PURRNET` versionDefine in an asmdef silently turns components into missing scripts. Check versionDefines first.
- Use the latest PurrNet beta. Report third-party bugs (plain ASCII, repro, versions); PurrNet fixes come fast, so prefer an upstream fix over a workaround. Remove any bypass once fixed and retest. After an update diff NetworkTransform settings and test pose drift.
- `SyncVar<RenderTexture>` works through `RenderTextureSerializer`, late joiners included.

## Transports and sessions
- One `LocalTransport` in `CompositeTransport` for both offline mode and the host's own client.
- `DirectJoinServerListAdapter` must be on the network manager prefab for IP joins.
- The relay is the team's self-hosted PurrLay fork (URL in the MasterNetworkManager prefab).
- A user Stop cancels pending connect/host starts. Logout in a session ends the session (with a confirmation).
- Dedicated servers: credentials only from local config, never in ServiceHubSettings. Don't retry obvious rejections. With Lobby/Servers off a server keeps running locally (LAN discovery).
- Presence is always on; its instance id comes from the stable device id.

## Testing
- Test with a ParrelSync clone (see `unity-mcp-workflow` skill): confirm data, not just connection (avatar transforms, SyncVar/RPC both ways).
- Cover every join path: server list, direct UDP, relay, join code, direct IP, LAN discovery, kick reconnect, cancel while connecting. Edge cases: host lost (pause host editor), reconnect countdown, bad address/code.

## XR interaction
- Check where the rig has a `PhysicsRaycaster` and which layers it hits. Where it hits an interactable's layer, a collider in front of a canvas steals the clicks, so grabbable panels get edge colliders instead of one covering collider. Grab interactables with an explicit collider list need new colliders added.
- World UI interaction works on the trigger (read `activateInput` from the hovering interactor), mouse clicks/hover like UI on desktop (even with the XR Device Simulator), and per-interactor hover/select for hands, mouse and touch.
- Poke: click on press-down with a re-press block, not on release.
- Prefer OpenXR, minimal Meta SDK dependencies.

## Streaming (LiveKit)
- Self-hosted LiveKit carries media only; ServiceHub does control (presence, commands, roles, displays). Displays register at runtime from the app SDK.
- In-app sharing is session-only, no audio (voice chat covers it); anyone in the session may watch. Roles apply on the web only.
- LiveKit Unity SDK comes from the team fork (`v2.1.0-fork.N`, branch develop). Update the packages-lock hash if a tag moves.
- Game audio tracks: disable native AEC/NS/AGC. RenderTexture video arrives upside down on the web (FlipMode). Detect lost streams from LiveKit signals, not frame timers. Own view on a display uses the local picture.
- Voice is UniVoice in-session only.

## Learning more
- PurrNet docs: https://purrnet.gitbook.io/docs · source: https://github.com/PurrNet/PurrNet
- XR Interaction Toolkit 3: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/index.html
- LiveKit: https://docs.livekit.io · Unity SDK upstream: https://github.com/livekit/client-sdk-unity
- ParrelSync: https://github.com/VeriorPies/ParrelSync
