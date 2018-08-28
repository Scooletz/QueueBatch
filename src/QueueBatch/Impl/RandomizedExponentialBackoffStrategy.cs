using System;

namespace QueueBatch.Impl
{
    class RandomizedExponentialBackoffStrategy
    {
        public const double RandomizationFactor = 0.2;
        readonly TimeSpan deltaBackoff;
        readonly TimeSpan maximumInterval;
        readonly TimeSpan minimumInterval;
        readonly Random random;

        uint backoffExponent;
        TimeSpan currentInterval;

        public RandomizedExponentialBackoffStrategy(TimeSpan minimumInterval, TimeSpan maximumInterval)
            : this(minimumInterval, maximumInterval, minimumInterval)
        {
        }

        public RandomizedExponentialBackoffStrategy(TimeSpan minimumInterval, TimeSpan maximumInterval,
            TimeSpan deltaBackoff)
        {
            if (minimumInterval.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumInterval), "The TimeSpan must not be negative.");

            if (maximumInterval.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(maximumInterval), "The TimeSpan must not be negative.");

            if (minimumInterval.Ticks > maximumInterval.Ticks)
                throw new ArgumentException("The minimumInterval must not be greater than the maximumInterval.",
                    nameof(minimumInterval));

            this.minimumInterval = minimumInterval;
            this.maximumInterval = maximumInterval;
            this.deltaBackoff = deltaBackoff;
            random = new Random();
        }

        public TimeSpan GetNextDelay(bool executionSucceeded)
        {
            if (executionSucceeded)
            {
                currentInterval = minimumInterval;
                backoffExponent = 1;
            }
            else if (currentInterval != maximumInterval)
            {
                var backoffInterval = minimumInterval;

                if (backoffExponent > 0)
                {
                    var incrementMsec = random.Next(1.0 - RandomizationFactor, 1.0 + RandomizationFactor) *
                                        Math.Pow(2.0, backoffExponent - 1) *
                                        deltaBackoff.TotalMilliseconds;
                    backoffInterval += TimeSpan.FromMilliseconds(incrementMsec);
                }

                if (backoffInterval < maximumInterval)
                {
                    currentInterval = backoffInterval;
                    backoffExponent++;
                }
                else
                {
                    currentInterval = maximumInterval;
                }
            }

            // else do nothing and keep current interval equal to max
            return currentInterval;
        }
    }
}