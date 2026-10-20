// Author: Michal Petr
// Created: 20.10.2026

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EDIVE.XRTools.ActionWheel
{
    public readonly struct ActionWheelContext
    {
        public InteractorHandedness Hand { get; }
        public Transform Anchor { get; }

        public ActionWheelContext(InteractorHandedness hand, Transform anchor)
        {
            Hand = hand;
            Anchor = anchor;
        }
    }
}
