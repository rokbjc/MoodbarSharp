using System;
using System.Numerics;
using FluentAssertions;
using MoodbarSharp;
using Xunit;

namespace MoodbarSharp.Tests;

public class FftTests
{
  [Fact]
  public void Forward_NullBuffer_Throws()
  {
    var act = () => Fft.Forward(null!);
    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void Forward_NonPowerOfTwo_Throws()
  {
    var buf = new Complex[3];
    var act = () => Fft.Forward(buf);
    act.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void Forward_DcSignal_AllEnergyInBinZero()
  {
    // A constant real signal has all its energy at DC (bin 0).
    // After a forward FFT of length N with no normalization, bin 0 magnitude == N.
    const int n = 8;
    var buf = new Complex[n];
    for (int i = 0; i < n; i++) buf[i] = new Complex(1, 0);

    Fft.Forward(buf);

    buf[0].Magnitude.Should().BeApproximately(n, precision: 1e-9);
    for (int i = 1; i < n; i++)
      buf[i].Magnitude.Should().BeApproximately(0, precision: 1e-9);
  }

  [Fact]
  public void Forward_NyquistSignal_AllEnergyInNyquistBin()
  {
    // Alternating +1/-1 is a real signal at the Nyquist frequency.
    // After FFT, bin N/2 should have magnitude == N; all others ≈ 0.
    const int n = 8;
    var buf = new Complex[n];
    for (int i = 0; i < n; i++) buf[i] = new Complex(i % 2 == 0 ? 1 : -1, 0);

    Fft.Forward(buf);

    buf[n / 2].Magnitude.Should().BeApproximately(n, precision: 1e-9);
    for (int i = 0; i < n; i++)
    {
      if (i == n / 2) continue;
      buf[i].Magnitude.Should().BeApproximately(0, precision: 1e-9);
    }
  }
}
