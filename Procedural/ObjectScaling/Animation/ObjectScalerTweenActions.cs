// Author: František Holubec
// Created: 19.10.2026

using System;
using DG.Tweening;
using EDIVE.Tweening.ObjectActions;
using UnityEngine;

namespace EDIVE.Procedural.ObjectScaling.Animation
{
    [Serializable]
    public class ObjectScalerSizeTweenAction : ATweenObjectAction<ObjectScaler>
    {
        [SerializeField]
        private Vector3 _EndValue = Vector3.one;

        protected override Tween GetTween(ObjectScaler target) => DOTween.To(() => target.Size, value => target.Size = value, _EndValue, _Duration).SetTarget(target);
    }
}
