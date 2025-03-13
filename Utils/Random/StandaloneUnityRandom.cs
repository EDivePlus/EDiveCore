using System;

namespace EDIVE.Extensions.Random
{
    public class StandaloneUnityRandom : IRandom
    {
        private UnityEngine.Random.State? _localState;
        private UnityEngine.Random.State _cachedGlobalState;

        public StandaloneUnityRandom(int? seed = null)
        {
            if (seed.HasValue) InitState(seed.Value);
        }
        
        public void InitState(int seed)
        {
            _cachedGlobalState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed);
            RestoreGlobalState();
        }

        public float Next()
        {
            return NextFloat();
        }

        public bool NextBool()
        {
            return NextInt(0, 2) == 1;
        }

        public int NextInt()
        {
            PrepareLocalState();
            var value = UnityEngine.Random.Range(0, int.MaxValue);
            RestoreGlobalState();
            return value;
        }

        public int NextInt(int max)
        {
            PrepareLocalState();
            var value = UnityEngine.Random.Range(0, max);
            RestoreGlobalState();
            return value;
        }

        public int NextInt(int min, int max)
        {
            PrepareLocalState();
            var value = UnityEngine.Random.Range(min, max);
            RestoreGlobalState();
            return value;
        }

        public float NextFloat()
        {
            PrepareLocalState();
            var value = UnityEngine.Random.Range(0f, 1f - float.Epsilon);
            RestoreGlobalState();
            return value;
        }

        public float NextFloat(float max)
        {
            PrepareLocalState();
            var value = UnityEngine.Random.Range(0f, max - float.Epsilon);
            RestoreGlobalState();
            return value;
        }

        public float NextFloat(float min, float max)
        {
            PrepareLocalState();
            var value = UnityEngine.Random.Range(min, max - float.Epsilon);
            RestoreGlobalState();
            return value;
        }
        
        private void PrepareLocalState()
        {
            _cachedGlobalState = UnityEngine.Random.state;
            if (!_localState.HasValue)
            {
                UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
                _localState = UnityEngine.Random.state;
            }
            UnityEngine.Random.state = _localState.Value;
        }

        private void RestoreGlobalState()
        {
            _localState = UnityEngine.Random.state;
            UnityEngine.Random.state = _cachedGlobalState;
        }
    }
}
