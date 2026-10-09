// Author: František Holubec
// Created: 09.10.2026

using System;

namespace EDIVE.EditorUtils.EditorPriority
{
    // Custom editor beats Unity, package and unprioritized editors for its type. Highest priority wins.
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class EditorPriorityAttribute : Attribute
    {
        public int Priority { get; }

        public EditorPriorityAttribute(int priority = 0)
        {
            Priority = priority;
        }
    }
}
