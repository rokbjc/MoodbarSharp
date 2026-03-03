using System;
using System.Security.Cryptography;
using FluentAssertions;
using Xunit;

namespace MoodbarSharp.Tests;

public class MoodbarGeneratorTests
{
  [Fact]
  public void GenerateFromFloatPcm_SineWave440Hz_MatchesKnownOutput()
  {
    // Deterministic 440 Hz sine wave, 1 second, 44100 Hz mono.
    // This is a golden/regression test: if any refactoring changes the
    // output bytes, this hash will no longer match and the test will fail.
    const int sampleRate = 44_100;
    float[] samples = new float[sampleRate];
    for (int i = 0; i < samples.Length; i++)
      samples[i] = (float)Math.Sin(Math.Tau * 440.0 * i / sampleRate);

    var gen = new MoodbarGenerator(new MoodbarOptions { Width = 1_000 });
    byte[] result = gen.GenerateFromFloatPcm(samples, sampleRate, channels: 1);

    result.Should().HaveCount(3_000);

    // Golden hash — to regenerate: Convert.ToHexString(SHA256.HashData(result))
    string hash = Convert.ToHexString(SHA256.HashData(result));
    hash.Should().Be("E15992A4FF3FA0F4A5BC60ADF5892165376D67AC46915E9FFD3B109F4828BCE7");
  }
}
