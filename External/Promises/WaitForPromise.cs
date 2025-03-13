using UnityEngine;

namespace EDIVE.External.Promises
{
    public class WaitForPromise : CustomYieldInstruction
    {
        private bool _completed;
        
        public override bool keepWaiting => !_completed;

        public WaitForPromise(IBasePromise promise)
        {
            _completed = false;
            promise.Finally(OnPromise);
        }

        private void OnPromise()
        {
            _completed = true;
        }
    }
}
