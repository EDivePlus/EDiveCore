// Author: František Holubec
// Created: 23.03.2026

using System.Collections;
using System.Collections.Generic;

namespace EDIVE.BuildTool
{
    public interface IBuildCallback
    {
        int Priority { get; }
        string CallbackName { get; }
        
        private sealed class PriorityRelationalComparer : IComparer<IBuildCallback>
        {
            public int Compare(IBuildCallback x, IBuildCallback y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (y is null) return 1;
                if (x is null) return -1;
                return x.Priority.CompareTo(y.Priority);
            }
        }

        public static IComparer<IBuildCallback> PriorityComparer { get; } = new PriorityRelationalComparer();
    }
    
    public interface IStateCaptureBuildCallback : IBuildCallback
    {
        IEnumerator OnStateCapture(BuildContext context);
    }
    
    public interface IPreprocessBuildCallback : IBuildCallback
    {
        IEnumerator OnPreprocess(BuildContext context);
    }
    
    public interface IPostprocessBuildCallback : IBuildCallback
    {
        IEnumerator OnPostprocess(BuildContext context);
    }
    
    public interface IStateRestoreBuildCallback : IBuildCallback
    {
        IEnumerator OnStateRestore(BuildContext context);
    }
}
