using System;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    // Own values for one material slot without touching the shared material.
    // Play mode swaps in a copy. Edit mode writes a property block, a copy would get saved into the scene.
    // Blocks cannot set keywords or render state, so those only work in play mode.
    //
    // Typical use:
    //   if (MaterialSlotOverride.Ensure(ref _override, _Renderer, _Index))
    //       _override.SetFloat(ID, value);
    //   ...
    //   MaterialSlotOverride.Release(ref _override); // OnDisable
    public sealed class MaterialSlotOverride : IDisposable
    {
        public const string DEFAULT_SUFFIX = " (instance)";

        private readonly string _suffix;
        private MaterialPropertyBlock _block;

        public Renderer Renderer { get; }
        public int Index { get; }
        public Material Source { get; private set; }
        public Material Instance { get; private set; }

        public bool IsActive => Instance != null || _block != null;

        // Still applied to that slot. False once the slot got another material.
        public bool IsCurrent => IsActive && Renderer.TryGetSharedMaterial(Index, out var slot)
                                          && slot != null && (slot == Instance || slot == Source);

        public MaterialSlotOverride(Renderer renderer, int index, string suffix = DEFAULT_SUFFIX)
        {
            Renderer = renderer;
            Index = index;
            _suffix = suffix;
        }

        // knownSource is used when the slot holds a copy saved by mistake.
        public bool Apply(Material knownSource = null)
        {
            Restore();
            if (!Renderer.TryGetSharedMaterial(Index, out var slot))
                return false;

            Source = slot != null && !slot.name.EndsWith(_suffix) ? slot : knownSource;
            if (Source == null)
                return false;

            if (!Application.isPlaying)
            {
                Renderer.SetSharedMaterial(Index, Source);
                _block = new MaterialPropertyBlock();
                return true;
            }

            Instance = new Material(Source)
            {
                name = Source.name + _suffix,
                hideFlags = HideFlags.DontSave
            };
            Renderer.SetSharedMaterial(Index, Instance);
            return true;
        }

        public void Restore()
        {
            if (_block != null)
            {
                Renderer.ClearPropertyBlock(Index);
                _block = null;
            }

            if (Instance == null)
                return;

            if (Renderer != null && Renderer.TryGetSharedMaterial(Index, out var slot) && slot == Instance)
                Renderer.SetSharedMaterial(Index, Source);

            Instance.SafeDestroy();
            Instance = null;
        }

        public void Dispose() => Restore();

        public bool Targets(Renderer renderer, int index) => Renderer == renderer && Index == index;

        // Null counts as off.
        public static implicit operator bool(MaterialSlotOverride slot) => slot != null && slot.IsActive;

        // Keeps a live override for this slot or builds a new one. False and null when the slot has no material.
        public static bool Ensure(ref MaterialSlotOverride slot, Renderer renderer, int index,
            string suffix = DEFAULT_SUFFIX, Material knownSource = null)
        {
            if (slot != null && slot.Targets(renderer, index) && slot.IsCurrent)
                return true;

            slot?.Restore();
            slot = new MaterialSlotOverride(renderer, index, suffix);
            if (slot.Apply(knownSource))
                return true;

            slot = null;
            return false;
        }

        public static void Release(ref MaterialSlotOverride slot)
        {
            slot?.Restore();
            slot = null;
        }

        // Drops every block value. Copy values stay.
        public void ClearBlock()
        {
            if (_block == null)
                return;

            _block.Clear();
            Renderer.ClearPropertyBlock(Index);
        }

        public void SetFloat(int id, float value)
        {
            if (Instance != null)
                Instance.SetFloat(id, value);
            else if (BeginBlock())
            {
                _block.SetFloat(id, value);
                Renderer.SetPropertyBlock(_block, Index);
            }
        }

        public void SetInt(int id, int value)
        {
            if (Instance != null)
                Instance.SetInteger(id, value);
            else if (BeginBlock())
            {
                _block.SetInteger(id, value);
                Renderer.SetPropertyBlock(_block, Index);
            }
        }

        // Converts to linear, unlike SetVector.
        public void SetColor(int id, Color value)
        {
            if (Instance != null)
                Instance.SetColor(id, value);
            else if (BeginBlock())
            {
                _block.SetColor(id, value);
                Renderer.SetPropertyBlock(_block, Index);
            }
        }

        public void SetVector(int id, Vector4 value)
        {
            if (Instance != null)
                Instance.SetVector(id, value);
            else if (BeginBlock())
            {
                _block.SetVector(id, value);
                Renderer.SetPropertyBlock(_block, Index);
            }
        }

        public void SetMatrix(int id, Matrix4x4 value)
        {
            if (Instance != null)
                Instance.SetMatrix(id, value);
            else if (BeginBlock())
            {
                _block.SetMatrix(id, value);
                Renderer.SetPropertyBlock(_block, Index);
            }
        }

        // Block skips null. It cannot unset a texture.
        public void SetTexture(int id, Texture value)
        {
            if (Instance != null)
                Instance.SetTexture(id, value);
            else if (value != null && BeginBlock())
            {
                _block.SetTexture(id, value);
                Renderer.SetPropertyBlock(_block, Index);
            }
        }

        // Reads first, so other writers of the same block keep their values.
        private bool BeginBlock()
        {
            if (_block == null || Renderer == null)
                return false;

            Renderer.GetPropertyBlock(_block, Index);
            return true;
        }
    }
}
