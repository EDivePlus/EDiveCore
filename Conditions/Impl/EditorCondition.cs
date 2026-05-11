// Author: František Holubec
// Created: 07.05.2026

using System;

namespace EDIVE.Conditions.Impl
{
    [Serializable]
    public class EditorCondition : ABoolCondition
    {
        protected override bool GetValue() =>
#if UNITY_EDITOR
            true;
#else
            false;
#endif
    }
}
