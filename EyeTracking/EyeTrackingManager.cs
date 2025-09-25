using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.External.Promises;
using R3;
using UnityEngine;

namespace EDIVE.EyeTracking
{
    public class EyeTrackingManager : ALoadableServiceBehaviour<EyeTrackingManager>
    {
        [SerializeField]
        private List<AEyeTrackingModule> _EyeTrackingModules = new();
        
        public AEyeTrackingModule ActiveModule { get; private set; }
        
        public Observable<EyeGazeFrame> RawEyeGazeStream => _rawEyeGazeStream;
        public Observable<EyeGazeFrame> FrameEyeGazeStream => _rawEyeGazeStream.ThrottleLastFrame(1, UnityFrameProvider.Update);
        
        private bool IsTracking => _startTrackingPromise != null && _startTrackingPromise.State == BasePromise.PromiseState.Fulfilled;
        
        private Promise<bool> _startTrackingPromise;
        private readonly HashSet<object> _requesters = new();
        private readonly Subject<EyeGazeFrame> _rawEyeGazeStream = new();
        private IDisposable _eyeGazeSubscription;
        
        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            ActiveModule = _EyeTrackingModules.FirstOrDefault(m => m.IsAvailable);
            if (ActiveModule == null)
            {
                Debug.LogWarning("[EyeTrackingManager] No eye tracking module is available.");
                return;
            }
            Debug.Log($"[EyeTrackingManager] ActiveModule: {ActiveModule.GetType().Name}");
            try
            {
                await ActiveModule.Initialize();
            }
            catch (Exception e)
            {
                ActiveModule = null;
                Debug.LogException(e);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (ActiveModule != null )
                ActiveModule.Terminate();
        }
        
        public UniTask<bool> AwaitStartTracking(object requester)
        {
            var tcs = new UniTaskCompletionSource<bool>();
            StartTracking(requester, success => tcs.TrySetResult(success));
            return tcs.Task;
        }
        
        public void StartTracking(object requester, Action<bool> callback)
        {
            if (requester == null)
                return;

            _startTrackingPromise ??= new Promise<bool>();
            _requesters.Add(requester);
        
            if (ActiveModule == null)
                return;

            _startTrackingPromise.Then(callback);
            if (ActiveModule.IsTracking)
                return;
            
            if (_requesters.Count == 1)
            {
                ActiveModule.StartTracking(OnTrackingStarted);
            }
        }
        
        private void OnTrackingStarted(bool success)
        {
            _startTrackingPromise?.Dispatch(success);
            _eyeGazeSubscription = ActiveModule.EyeGazeStream.Subscribe(data =>
            {
                _rawEyeGazeStream.OnNext(data);
            });
        }
    
        public void StopTracking(object requester)
        {
            if (requester == null)
                return;

            _requesters.Remove(requester);

            if (_requesters.Count == 0)
            {
                if (ActiveModule != null) 
                    ActiveModule.StopTracking();
                
                _eyeGazeSubscription?.Dispose();
                _startTrackingPromise?.RemoveAllListeners();
                _startTrackingPromise = null;
            }
        }
    }
}