// Author: František Holubec
// Created: 02.12.2025

using System;
using System.Collections;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class MonoBehaviourExtensions
    {
        public static Coroutine InvokeAfterTime(this MonoBehaviour self, Action action, float time)
        {
            return action == null ? throw new ArgumentNullException() : self.StartCoroutine(ExecuteAfterTime());
            IEnumerator ExecuteAfterTime()
            {
                yield return new WaitForSeconds(time);
                action.Invoke();
            }
        }

        public static Coroutine InvokeAfterTimeRealtime(this MonoBehaviour self, Action action, float time)
        {
            return action == null ? throw new ArgumentNullException() : self.StartCoroutine(ExecuteAfterTimeRealtime());
            IEnumerator ExecuteAfterTimeRealtime()
            {
                yield return new WaitForSecondsRealtime(time);
                action.Invoke();
            }
        }

        public static Coroutine InvokeNextFrame(this MonoBehaviour self, Action action) => InvokeInFrames(self, action, 1);
        public static Coroutine InvokeInFrames(this MonoBehaviour self, Action action, int frames)
        {
            return action == null ? throw new ArgumentNullException() : self.StartCoroutine(ExecuteAfterFrames());
            IEnumerator ExecuteAfterFrames()
            {
                for (var i = 0; i < frames; i++)
                    yield return null;
                action.Invoke();
            }
        }
        
        public static Coroutine InvokeOnEndOfFrame(this MonoBehaviour self, Action action)
        {
            return action == null ? throw new ArgumentNullException() : self.StartCoroutine(ExecuteOnEndOfFrame());
            IEnumerator ExecuteOnEndOfFrame()
            {
                yield return new WaitForEndOfFrame();
                action.Invoke();
            }
        }

        public static Coroutine InvokeWhen(this MonoBehaviour self, Func<bool> predicate, Action action)
        {
            return action == null ? throw new ArgumentNullException() : self.StartCoroutine(ExecuteOnPredicate());
            IEnumerator ExecuteOnPredicate()
            {
                yield return new WaitUntil(predicate);
                action.Invoke();
            }
        }
    }
}
