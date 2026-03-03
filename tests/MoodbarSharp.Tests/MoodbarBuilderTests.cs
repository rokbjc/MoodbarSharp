using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace MoodbarSharp.Tests;

public class MoodbarBuilderTests
{
  [Fact]
  public void Init_InvalidBands_Throws()
  {
    var builder = new MoodbarBuilder();
    Action act = () => builder.Init(0, 44_100);
    act.Should().Throw<ArgumentOutOfRangeException>();
  }

  [Fact]
  public void Init_InvalidRate_Throws()
  {
    var builder = new MoodbarBuilder();
    Action act = () => builder.Init(128, 0);
    act.Should().Throw<ArgumentOutOfRangeException>();
  }

  [Fact]
  public void AddFrame_BeforeInit_Throws()
  {
    var builder = new MoodbarBuilder();
    double[] mags = new double[128];
    Action act = () => builder.AddFrame(mags);
    act.Should().Throw<InvalidOperationException>();
  }

  [Fact]
  public void Finish_NoFrames_ReturnsAllZeros()
  {
    var builder = new MoodbarBuilder();
    builder.Init(128, 44_100);

    byte[] result = builder.Finish(100);

    result.Should().HaveCount(300);
    result.Should().OnlyContain(b => b == 0);
  }

  [Fact]
  public void Finish_ReturnsWidthTimes3Bytes()
  {
    var builder = new MoodbarBuilder();
    builder.Init(128, 44_100);
    double[] mags = Enumerable.Range(1, 128).Select(i => (double)i).ToArray();
    builder.AddFrame(mags);

    byte[] result = builder.Finish(1_000);

    result.Should().HaveCount(3_000);
  }

  [Fact]
  public void Finish_NonSilentInput_ProducesNonZeroOutput()
  {
    var builder = new MoodbarBuilder();
    builder.Init(128, 44_100);
    double[] mags = Enumerable.Range(1, 128).Select(i => (double)i).ToArray();
    for (int i = 0; i < 10; i++) builder.AddFrame(mags);

    byte[] result = builder.Finish(100);

    result.Should().Contain(b => b > 0);
  }
}
