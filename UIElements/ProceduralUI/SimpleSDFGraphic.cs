// Author: Michal Petr
// Created: 22.09.2026

using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UIElements.ProceduralUI
{
    public class SimpleSDFGraphic : SignedDistanceFieldGraphic
    {
        [PropertySpace]
        [PropertyOrder(10)]
        [SerializeField]
        private CornerRoundness _Roundness;

        public CornerRoundness Roundness
        {
            get => _Roundness;
            set { _Roundness = value; SetVerticesDirty(); }
        }

        public Vector4 ResolveRoundness(float width, float height) => _Roundness.Resolve(width, height);

        protected override Vector4 GetRoundness() => ResolveRoundness(rectTransform.rect.width, rectTransform.rect.height);
    }
}
