// This file isn't generated, but this comment is necessary to exclude it from StyleCop analysis.
// <auto-generated/>

using Xunit;

namespace HdrHistogram.UnitTests
{
    
    internal sealed class ShortHistogramEncodingTests : HistogramEncodingTestBase
    {
        internal override HistogramBase Create(long highestTrackableValue, int numberOfSignificantDigits)
        {
            //return new ShortHistogram(highestTrackableValue, numberOfSignificantDigits);
            return HistogramFactory.With16BitBucketSize()
                .WithValuesUpTo(highestTrackableValue)
                .WithPrecisionOf(numberOfSignificantDigits)
                .Create();
        }

        internal override void LoadFullRange(IRecorder source)
        {
            for (long i = 0L; i < DefaultHighestTrackableValue; i += 1000L)
            {
                source.RecordValue(i);
            }
            source.RecordValue(DefaultHighestTrackableValue);
        }
    }
}   