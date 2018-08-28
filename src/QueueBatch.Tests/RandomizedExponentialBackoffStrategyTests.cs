using System;
using NUnit.Framework;
using QueueBatch.Impl;

namespace QueueBatch.Tests
{
    public class RandomizedExponentialBackoffStrategyTests
    {
        [Test]
        public void Constructor_IfMinimumIntervalIsNegative_Throws()
        {
            var minimumInterval = TimeSpan.FromTicks(-1);
            var maximumInterval = TimeSpan.Zero;

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateProductUnderTest(minimumInterval, maximumInterval));

            Assert.AreEqual("minimumInterval", ex.ParamName);

        }

        [Test]
        public void Constructor_IfMaximumIntervalIsNegative_Throws()
        {
            var minimumInterval = TimeSpan.Zero;
            var maximumInterval = TimeSpan.FromTicks(-1);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateProductUnderTest(minimumInterval, maximumInterval));

            Assert.AreEqual("maximumInterval", ex.ParamName);
        }

        [Test]
        public void Constructor_IfMinimumIntervalIsGreaterThanMaximumInterval_Throws()
        {
            var minimumInterval = TimeSpan.FromMilliseconds(2);
            var maximumInterval = TimeSpan.FromMilliseconds(1);

            var ex = Assert.Throws<ArgumentException>(
                () => CreateProductUnderTest(minimumInterval, maximumInterval));

            Assert.AreEqual("minimumInterval", ex.ParamName);
        }

        [Test]
        public void GetNextDelay_WhenFirstExecutionSucceeded_ReturnsMinimumInterval()
        {
            var minimumInterval = TimeSpan.FromMilliseconds(123);
            var maximumInterval = TimeSpan.FromSeconds(4);
            var product = CreateProductUnderTest(minimumInterval, maximumInterval);

            var nextDelay = product.GetNextDelay(executionSucceeded: true);

            Assert.AreEqual(minimumInterval, nextDelay);
        }

        [Test]
        public void GetNextDelay_WhenFirstExecutionFailed_ReturnsMinimumInterval()
        {
            var minimumInterval = TimeSpan.FromMilliseconds(123);
            var maximumInterval = TimeSpan.FromSeconds(4);
            var product = CreateProductUnderTest(minimumInterval, maximumInterval);

            var nextDelay = product.GetNextDelay(executionSucceeded: false);

            Assert.AreEqual(minimumInterval, nextDelay);
        }

        [Test]
        public void GetNextDelay_WhenSecondExecutionFailedAgain_ReturnsApproximatelyDoubleMinimumInterval()
        {
            var minimumInterval = TimeSpan.FromMilliseconds(123);
            var maximumInterval = TimeSpan.FromSeconds(4);

            var product = CreateProductUnderTest(minimumInterval, maximumInterval);
            product.GetNextDelay(executionSucceeded: false);

            var nextDelay = product.GetNextDelay(executionSucceeded: false);

            AssertInRandomizationRange(minimumInterval, 1, nextDelay);
        }

        [Test]
        public void GetNextDelay_WhenThirdExecutionFailedAgain_ReturnsApproximatelyQuadrupleMinimumInterval()
        {
            var minimumInterval = TimeSpan.FromMilliseconds(123);
            var maximumInterval = TimeSpan.FromSeconds(4);
            var product = CreateProductUnderTest(minimumInterval, maximumInterval);
            product.GetNextDelay(executionSucceeded: false);
            product.GetNextDelay(executionSucceeded: false);

            var nextDelay = product.GetNextDelay(executionSucceeded: false);

            AssertInRandomizationRange(minimumInterval, 2, nextDelay);
        }

        [Test]
        public void GetNextDelay_WhenExecutionSuccededAfterPreviouslyFailing_ReturnsMinimumInterval()
        {
            var minimumInterval = TimeSpan.FromMilliseconds(123);
            var maximumInterval = TimeSpan.FromSeconds(4);
            var product = CreateProductUnderTest(minimumInterval, maximumInterval);
            product.GetNextDelay(executionSucceeded: false);

            var nextDelay = product.GetNextDelay(executionSucceeded: true);

            Assert.AreEqual(minimumInterval, nextDelay);
        }

        [Test]
        public void GetNextDelay_WhenExecutionFailedAfterPreviouslySucceeding_ReturnsRoughlyDoubleMinimumInterval()
        {
            var minimumInterval = TimeSpan.FromMilliseconds(123);
            var maximumInterval = TimeSpan.FromSeconds(4);
            var product = CreateProductUnderTest(minimumInterval, maximumInterval);
            product.GetNextDelay(executionSucceeded: true);

            var nextDelay = product.GetNextDelay(executionSucceeded: false);

            AssertInRandomizationRange(minimumInterval, 1, nextDelay);
        }

        [Test]
        public void GetNextDelay_WhenExecutionFailedAgainEnoughTimes_ReturnsMaximumInternal()
        {
            var minimumInterval = TimeSpan.FromMilliseconds(123);
            var maximumInterval = TimeSpan.FromSeconds(4);
            var product = CreateProductUnderTest(minimumInterval, maximumInterval);

            const double randomizationMinimum = 1 - RandomizedExponentialBackoffStrategy.RandomizationFactor;
            var minimumDeltaInterval = new TimeSpan((long)(minimumInterval.Ticks * randomizationMinimum));
            // minimumBackoffInterval = minimumInterval + minimumDeltaInterval * 2 ^ (deltaIteration - 1)
            // when is minimumBackOffInterval first >= maximumInterval?
            // maximumInterval <= minimumInterval + minimumDeltaInterval * 2 ^ (deltaIteration - 1)
            // minimumInterval + minimumDeltaInterval * 2 ^ (deltaIteration - 1) >= maximumInterval
            // minimumDeltaInterval * 2 ^ (deltaIteration - 1) >= maximumInterval - minimumInterval
            // 2 ^ (deltaIteration - 1) >= (maximumInterval - minimumInterval) / minimumDeltaInterval
            // deltaIteration - 1 >= log2(maximumInterval - minimumInterval) / minimumDeltaInterval
            // deltaIteration >= (log2(maximumInterval - minimumInterval) / minimumDeltaInterval) + 1
            var deltaIterationsNeededForMaximumInterval = (int)Math.Ceiling(Math.Log(
                (maximumInterval - minimumInterval).Ticks / minimumDeltaInterval.Ticks, 2)) + 1;

            // Add one for initial minimumInterval interation (before deltaIterations start).
            var iterationsNeededForMaximumInterval = deltaIterationsNeededForMaximumInterval + 1;

            for (var iteration = 0; iteration < iterationsNeededForMaximumInterval - 1; iteration++)
            {
                product.GetNextDelay(executionSucceeded: false);
            }

            // Act
            var nextDelay = product.GetNextDelay(executionSucceeded: false);

            // Assert
            Assert.AreEqual(maximumInterval, nextDelay);
        }

        static RandomizedExponentialBackoffStrategy CreateProductUnderTest(TimeSpan minimumInterval,
            TimeSpan maximumInterval)
        {
            return new RandomizedExponentialBackoffStrategy(minimumInterval, maximumInterval, minimumInterval);
        }

        static void AssertInRandomizationRange(TimeSpan minimumInterval, int retryCount, TimeSpan actual)
        {
            var lowerBoundary = minimumInterval.Ticks * (1 - RandomizedExponentialBackoffStrategy.RandomizationFactor) *
                    (Math.Pow(2, retryCount - 1) + 1);
            var higherBoundary = minimumInterval.Ticks * (1 + RandomizedExponentialBackoffStrategy.RandomizationFactor) *
                    (Math.Pow(2, retryCount - 1) + 1);

            Assert.LessOrEqual(lowerBoundary, actual.Ticks);
            Assert.LessOrEqual(actual.Ticks, higherBoundary);
        }
    }
}