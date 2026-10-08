// Author: František Holubec
// Created: 06.10.2026

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.Utils.DrivenValues
{
    public class DrivenValuesCollection
    {
        public Dictionary<Object, HashSet<string>> Targets { get; } = new();

        public void Add(Object target, string member = null)
        {
            if (target == null)
                return;

            if (!Targets.TryGetValue(target, out var members))
                Targets[target] = members = new HashSet<string>();

            if (!string.IsNullOrEmpty(member))
                members.Add(member);
        }
    }
}
#endif
