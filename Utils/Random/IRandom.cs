namespace EDIVE.Extensions.Random
{
    public interface IRandom
    {
        float Next();

        /// <summary>Returns a uniformly random bool value.</summary>
        bool NextBool();

        /// <summary>Returns a uniformly random int value in the interval [0, int.MaxValue).</summary>
        int NextInt();

        /// <summary>Returns a uniformly random int value in the interval [0, max).</summary>
        int NextInt(int max);

        /// <summary>Returns a uniformly random int value in the interval [min, max).</summary>
        int NextInt(int min, int max);
        
        /// <summary>Returns a uniformly random float value in the interval [0, 1).</summary>
        float NextFloat();

        /// <summary>Returns a uniformly random float value in the interval [0, max).</summary>
        float NextFloat(float max);

        /// <summary>Returns a uniformly random float value in the interval [min, max).</summary>
        float NextFloat(float min, float max);
    }
}
