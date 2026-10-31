---
name: edive-git
description: Git conventions for EDIVE and the projects that include it as a submodule. Use whenever committing, squashing, rebasing, branching or pushing in an EDIVE project.
---

# Git in EDIVE projects

## Commits
- Never commit .blend/.blend1 files, local test settings (localhost URLs, test credentials in ServiceHubSettings and similar assets) or secrets. Scan committed assets for secrets before committing.

## Submodule
- EDIVE is a separate repo inside each project. Commit inside EDIVE first, then the parent pointer. Push EDIVE first, otherwise CI fails with "bad object".
- Never leave the submodule on a detached HEAD. Feature branches: one branch per repo with matching names.
- Rewriting a feature that spans both repos: EDIVE first, then the parent, and remap the parent's submodule pointers. Only rewrite commits that aren't pushed.

## Conflicts
- Scene or prefab conflicts: use a Unity-aware merge (UnityYAMLMerge / Smart Merge, or Rider's Unity merge). If that fails, take the upstream file and re-apply your own changes on top.
