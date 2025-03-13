using static UnityEngine.Random;

namespace EDIVE.Extensions.Random
{
    public class UnityRandom : IRandom
    {
        /// <summary>
        /// Initialize the state of the Random instance with a given seed value.
        /// </summary>
        public void InitState(int seed) => UnityEngine.Random.InitState(seed);

        public float Next() { return NextFloat(); }

        public bool NextBool() { return NextInt(0, 2) == 1; }

        public int NextInt() { return Range(0, int.MaxValue); }
        public int NextInt(int max) { return Range(0, max); }
        public int NextInt(int min, int max) { return Range(min, max); }

        public float NextFloat() { return Range(0f, 1f - float.Epsilon); }
        public float NextFloat(float max) { return Range(0f, max - float.Epsilon); }
        public float NextFloat(float min, float max) { return Range(min, max - float.Epsilon); }
    }
}
