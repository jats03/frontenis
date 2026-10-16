using Frontenis.Infrastructure.Time;
using Xunit;

namespace Frontenis.Tests;

public class MadridClockTests
{
    private sealed class FixedTime : TimeProvider
    {
        private readonly DateTimeOffset _utc;
        public FixedTime(DateTimeOffset utc) { _utc = utc; }
        public override DateTimeOffset GetUtcNow() => _utc;
    }

    [Theory]
    [InlineData(2026, 10, 16, 17, 0, 19)]  // horario de verano (UTC+2)
    [InlineData(2026, 12, 1, 18, 0, 19)]   // horario de invierno (UTC+1)
    public void Converts_utc_to_madrid_local_time(int y, int m, int d, int hourUtc, int minute, int expectedHour)
    {
        var clock = new MadridClock(new FixedTime(new DateTimeOffset(y, m, d, hourUtc, minute, 0, TimeSpan.Zero)));
        Assert.Equal(new DateTime(y, m, d, expectedHour, minute, 0), clock.Now);
        Assert.Equal(DateTimeKind.Unspecified, clock.Now.Kind);
    }
}
