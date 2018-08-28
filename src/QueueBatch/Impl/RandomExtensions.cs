using System;

namespace QueueBatch.Impl
{
    static class RandomExtensions
    {
        public static double Next(this Random random, double minValue, double maxValue)
        {
            return (maxValue - minValue) * random.NextDouble() + minValue;
        }
    }
}