// Based on https://github.com/strangeioc/strangeioc/tree/master/StrangeIoC/scripts/strange/extensions/signal
using System;
using System.Linq;
using UnityEngine;

namespace EDIVE.External.Signals
{
    public interface ISignal
    {
        void RemoveAllListeners();
    }
    
    public class Signal : ISignal
    {
        public event Action Listener;
        public event Action OnceListener;
        
        public void AddListener(Action callback)
        {
            Listener = AddUnique(Listener, callback);
        }

        public void AddOnceListener(Action callback)
        {
            OnceListener = AddUnique(OnceListener, callback);
        }

        public void RemoveListener(Action callback)
        {
            if (Listener != null)
                Listener -= callback;
        }

        public void RemoveOnceListener(Action callback)
        {
            if (OnceListener != null)
                OnceListener -= callback;
        }
        
        public void Dispatch()
        {
            Listener?.Invoke();
            OnceListener?.Invoke();
            OnceListener = null;
        }
        
        public void DispatchSafe()
        {
            if (Listener != null)
                InvokeSafe(Listener);
            if (OnceListener != null)
                InvokeSafe(OnceListener);
            OnceListener = null;
        }

        private void InvokeSafe(Action listeners)
        {
            var invocationList = listeners.GetInvocationList();
            foreach (var invocation in invocationList)
            {
                try
                {
                    if (invocation is Action action) action.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
        
        protected Action AddUnique(Action listeners, Action callback)
        {
            if (listeners == null || !listeners.GetInvocationList().Contains(callback)) 
                listeners += callback;
            return listeners;
        }

        public void RemoveAllListeners()
        {
            Listener = null;
            OnceListener = null;
        }
    }
    
    public class Signal<T> : ISignal
    {
        public event Action<T> Listener;
        public event Action<T> OnceListener;

        public void AddListener(Action<T> callback)
        {
            Listener = AddUnique(Listener, callback);
        }

        public void AddOnceListener(Action<T> callback)
        {
            OnceListener = AddUnique(OnceListener, callback);
        }
        
        public void RemoveListener(Action<T> callback)
        {
            if (Listener != null)
                Listener -= callback;
        }

        public void RemoveOnceListener(Action<T> callback)
        {
            if (OnceListener != null)
                OnceListener -= callback;
        }

        public void Dispatch(T t1)
        {
            Listener?.Invoke(t1);
            OnceListener?.Invoke(t1);
            OnceListener = null;
        }
        
        public void DispatchSafe(T t1)
        {
            if (Listener != null)
                InvokeSafe(Listener, t1);
            if (OnceListener != null)
                InvokeSafe(OnceListener, t1);
            OnceListener = null;
        }

        private void InvokeSafe(Action<T> listeners, T t1)
        {
            var invocationList = listeners.GetInvocationList();
            foreach (var invocation in invocationList)
            {
                try
                {
                    if (invocation is Action<T> action) action.Invoke(t1);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
        
        protected Action<T> AddUnique(Action<T> listeners, Action<T> callback)
        {
            if (listeners == null || !listeners.GetInvocationList().Contains(callback)) 
                listeners += callback;
            return listeners;
        }

        public void RemoveAllListeners()
        {
            Listener = null;
            OnceListener = null;
        }
    }

    /// Base concrete form for a Signal with two parameters
    public class Signal<T, T2> : ISignal
    {
        public event Action<T, T2> Listener;
        public event Action<T, T2> OnceListener;

        public void AddListener(Action<T, T2> callback)
        {
            Listener = AddUnique(Listener, callback);
        }

        public void AddOnceListener(Action<T, T2> callback)
        {
            OnceListener = AddUnique(OnceListener, callback);
        }

        public void RemoveListener(Action<T, T2> callback)
        {
            if (Listener != null)
                Listener -= callback;
        }

        public void RemoveOnceListener(Action<T, T2> callback)
        {
            if (OnceListener != null)
                OnceListener -= callback;
        }

        public void Dispatch(T t1, T2 t2)
        {
            Listener?.Invoke(t1, t2);
            OnceListener?.Invoke(t1, t2);
            OnceListener = null;
        }
        
        public void DispatchSafe(T t1, T2 t2)
        {
            if (Listener != null)
                InvokeSafe(Listener, t1, t2);
            if (OnceListener != null)
                InvokeSafe(OnceListener, t1, t2);
            OnceListener = null;
        }

        private void InvokeSafe(Action<T, T2> listeners, T t1, T2 t2)
        {
            var invocationList = listeners.GetInvocationList();
            foreach (var invocation in invocationList)
            {
                try
                {
                    if (invocation is Action<T, T2> action) action.Invoke(t1, t2);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private Action<T, T2> AddUnique(Action<T, T2> listeners, Action<T, T2> callback)
        {
            if (listeners == null || !listeners.GetInvocationList().Contains(callback)) 
                listeners += callback;
            return listeners;
        }

        public void RemoveAllListeners()
        {
            Listener = null;
            OnceListener = null;
        }
    }

    /// Base concrete form for a Signal with three parameters
    public class Signal<T, T2, T3> :  ISignal
    {
        public event Action<T, T2, T3> Listener;
        public event Action<T, T2, T3> OnceListener;

        public void AddListener(Action<T, T2, T3> callback)
        {
            Listener = AddUnique(Listener, callback);
        }

        public void AddOnceListener(Action<T, T2, T3> callback)
        {
            OnceListener = AddUnique(OnceListener, callback);
        }

        public void RemoveListener(Action<T, T2, T3> callback)
        {
            if (Listener != null)
                Listener -= callback;
        }

        public void RemoveOnceListener(Action<T, T2, T3> callback)
        {
            if (OnceListener != null)
                OnceListener -= callback;
        }

        public void Dispatch(T t1, T2 t2, T3 t3)
        {
            Listener?.Invoke(t1, t2, t3);
            OnceListener?.Invoke(t1, t2, t3);
            OnceListener = null;
        }
        
        public void DispatchSafe(T t1, T2 t2, T3 t3)
        {
            if (Listener != null)
                InvokeSafe(Listener, t1, t2, t3);
            if (OnceListener != null)
                InvokeSafe(OnceListener, t1, t2, t3);
            OnceListener = null;
        }

        private void InvokeSafe(Action<T, T2, T3> listeners, T t1, T2 t2, T3 t3)
        {
            var invocationList = listeners.GetInvocationList();
            foreach (var invocation in invocationList)
            {
                try
                {
                    if (invocation is Action<T, T2, T3> action) action.Invoke(t1, t2, t3);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private Action<T, T2, T3> AddUnique(Action<T, T2, T3> listeners, Action<T, T2, T3> callback)
        {
            if (listeners == null || !listeners.GetInvocationList().Contains(callback)) 
                listeners += callback;
            return listeners;
        }

        public void RemoveAllListeners()
        {
            Listener = null;
            OnceListener = null;
        }
    }

    /// Base concrete form for a Signal with four parameters
    public class Signal<T, T2, T3, T4> : ISignal
    {
        public event Action<T, T2, T3, T4> Listener;
        public event Action<T, T2, T3, T4> OnceListener;

        public void AddListener(Action<T, T2, T3, T4> callback)
        {
            Listener = AddUnique(Listener, callback);
        }

        public void AddOnceListener(Action<T, T2, T3, T4> callback)
        {
            OnceListener = AddUnique(OnceListener, callback);
        }

        public void RemoveListener(Action<T, T2, T3, T4> callback)
        {
            if (Listener != null)
                Listener -= callback;
        }

        public void RemoveOnceListener(Action<T, T2, T3, T4> callback)
        {
            if (OnceListener != null)
                OnceListener -= callback;
        }

        public void Dispatch(T t1, T2 t2, T3 t3, T4 t4)
        {
            Listener?.Invoke(t1, t2, t3, t4);
            OnceListener?.Invoke(t1, t2, t3, t4);
            OnceListener = null;
        }
        
        public void DispatchSafe(T t1, T2 t2, T3 t3, T4 t4)
        {
            if (Listener != null)
                InvokeSafe(Listener, t1, t2, t3, t4);
            if (OnceListener != null)
                InvokeSafe(OnceListener, t1, t2, t3, t4);
            OnceListener = null;
        }

        private void InvokeSafe(Action<T, T2, T3, T4> listeners, T t1, T2 t2, T3 t3, T4 t4)
        {
            var invocationList = listeners.GetInvocationList();
            foreach (var invocation in invocationList)
            {
                try
                {
                    if (invocation is Action<T, T2, T3, T4> action) action.Invoke(t1, t2, t3, t4);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private Action<T, T2, T3, T4> AddUnique(Action<T, T2, T3, T4> listeners, Action<T, T2, T3, T4> callback)
        {
            if (listeners == null || !listeners.GetInvocationList().Contains(callback)) 
                listeners += callback;
            return listeners;
        }

        public void RemoveAllListeners()
        {
            Listener = null;
            OnceListener = null;
        }
    }
}
