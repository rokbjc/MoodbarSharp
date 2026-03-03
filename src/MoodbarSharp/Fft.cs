using System.Numerics;

namespace MoodbarSharp;

/// <summary>
/// Minimal in-place radix-2 Cooley–Tukey FFT.
/// </summary>
internal static class Fft
{
  public static void Forward(Complex[] buffer)
  {
    if (buffer is null) throw new ArgumentNullException(nameof(buffer));
    int n = buffer.Length;
    if (n == 0 || (n & (n - 1)) != 0)
      throw new ArgumentException("FFT length must be a power of two.", nameof(buffer));

    BitReversePermutation(buffer);

    for (int len = 2; len <= n; len <<= 1)
    {
      double ang = -2.0 * Math.PI / len;
      Complex wlen = new(Math.Cos(ang), Math.Sin(ang));

      for (int i = 0; i < n; i += len)
      {
        var w = Complex.One;
        int half = len >> 1;

        for (int j = 0; j < half; j++)
        {
          var u = buffer[i + j];
          var v = buffer[i + j + half] * w;
          buffer[i + j] = u + v;
          buffer[i + j + half] = u - v;
          w *= wlen;
        }
      }
    }
  }

  private static void BitReversePermutation(Complex[] a)
  {
    int n = a.Length;
    int j = 0;
    for (int i = 1; i < n; i++)
    {
      int bit = n >> 1;
      for (; (j & bit) != 0; bit >>= 1) j ^= bit;
      j ^= bit;

      if (i < j)
      {
        (a[i], a[j]) = (a[j], a[i]);
      }
    }
  }
}
