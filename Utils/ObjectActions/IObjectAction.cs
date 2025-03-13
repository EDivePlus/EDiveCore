// Author: František Holubec
// Created: 12.03.2025

using System;

namespace EDIVE.Utils.ObjectActions
{
    public interface IObjectAction
    {
        Type TargetType { get; }
        bool IsValidFor(Type targetType);
    }
}
