using System;
using System.Collections.Generic;

namespace EDIVE.Tweening
{
    public class TweenTargetCollection
    {
        public Dictionary<UnityEngine.Object, Type> Targets { get; } = new();

        public void Add(UnityEngine.Object target, Type targetType)
        {
            Targets[target] = targetType;
        }
    }
}
