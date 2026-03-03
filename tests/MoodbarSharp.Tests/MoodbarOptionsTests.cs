using FluentAssertions;
using Xunit;

namespace MoodbarSharp.Tests;

public class MoodbarOptionsTests
{
  [Fact]
  public void Defaults_AreExpectedValues()
  {
    var opts = new MoodbarOptions();

    opts.Width.Should().Be(1_000);
    opts.Bands.Should().Be(128);
    opts.FftSize.Should().Be(2_048);
    opts.HopSize.Should().Be(512);
    opts.DownmixToMono.Should().BeTrue();
  }
}
