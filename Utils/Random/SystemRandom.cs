namespace EDIVE.Extensions.Random
{
    public class SystemRandom : IRandom
    {
        private readonly System.Random _randomInstance;

        public SystemRandom(System.Random randomInstance) { _randomInstance = randomInstance; }
        public SystemRandom(int seed) { _randomInstance = new System.Random(seed); }
        public SystemRandom(int? seed = null) { _randomInstance = seed.HasValue ? new System.Random(seed.Value) : new System.Random(); }
        public SystemRandom() { _randomInstance = new System.Random(); }

        public float Next() { return NextFloat(); }

        public bool NextBool() { return NextInt(0, 2) == 1; }

        public int NextInt() { return _randomInstance.Next(); }
        public int NextInt(int max) { return _randomInstance.Next(max); }
        public int NextInt(int min, int max) { return _randomInstance.Next(min, max); }

        public float NextFloat() { return (float) _randomInstance.NextDouble(); }
        public float NextFloat(float max) { return (float) (_randomInstance.NextDouble() * max); }
        public float NextFloat(float min, float max) { return min + (float) _randomInstance.NextDouble() * (max - min); }
    }
}
