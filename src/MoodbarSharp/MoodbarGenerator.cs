using System.Numerics;
using System.Runtime.InteropServices;

namespace MoodbarSharp;

/// <summary>
/// Generates moodbar RGB data from PCM audio by computing a magnitude spectrum for successive frames,
/// then feeding it into <see cref="MoodbarBuilder"/>.
/// </summary>
/// <remarks>
/// The original project uses a GStreamer element (fastspectrum). This class provides a pure .NET equivalent.
/// Exact output parity is not guaranteed without matching the original FFT/windowing implementation.
/// </remarks>
public sealed class MoodbarGenerator
{
  private readonly MoodbarOptions _opt;
  private readonly double[] _window;
  private readonly Complex[] _fftBuf;
  private readonly double[] _magnitudes;

  /// <summary>
  /// Creates a new generator with the given options, or default options if <paramref name="options"/> is <see langword="null"/>.
  /// </summary>
  /// <param name="options">Processing options. Pass <see langword="null"/> to use defaults.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown if <see cref="MoodbarOptions.Width"/>, <see cref="MoodbarOptions.Bands"/>,
  /// <see cref="MoodbarOptions.FftSize"/>, or <see cref="MoodbarOptions.HopSize"/> is invalid.
  /// </exception>
  public MoodbarGenerator(MoodbarOptions? options = null)
  {
    _opt = options ?? new MoodbarOptions();
    if (_opt.Width <= 0) throw new ArgumentOutOfRangeException(nameof(_opt.Width));
    if (_opt.Bands <= 0) throw new ArgumentOutOfRangeException(nameof(_opt.Bands));
    if (_opt.FftSize <= 0 || (_opt.FftSize & (_opt.FftSize - 1)) != 0) throw new ArgumentOutOfRangeException(nameof(_opt.FftSize));
    if (_opt.HopSize <= 0) throw new ArgumentOutOfRangeException(nameof(_opt.HopSize));

    // Precompute Hann window: 0.5 - 0.5 * cos(2πn/(N-1))
    _window = new double[_opt.FftSize];
    for (int i = 0; i < _window.Length; i++)
      _window[i] = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / (_window.Length - 1));

    _fftBuf = new Complex[_opt.FftSize];
    _magnitudes = new double[_opt.Bands];
  }

  /// <summary>
  /// Generates moodbar bytes from interleaved float PCM in [-1, 1].
  /// </summary>
  public byte[] GenerateFromFloatPcm(ReadOnlySpan<float> interleaved, int sampleRate, int channels)
  {
    if (channels <= 0) throw new ArgumentOutOfRangeException(nameof(channels));
    if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
    if (interleaved.Length % channels != 0) throw new ArgumentException("Interleaved length must be divisible by channels.", nameof(interleaved));

    var builder = new MoodbarBuilder();
    builder.Init(_opt.Bands, sampleRate);

    int frames = interleaved.Length / channels;
    int pos = 0;

    while (pos + _opt.FftSize <= frames)
    {
      // Fill FFT buffer (mono or first channel)
      if (_opt.DownmixToMono && channels > 1)
      {
        for (int i = 0; i < _opt.FftSize; i++)
        {
          double sum = 0;
          int baseIdx = (pos + i) * channels;
          for (int c = 0; c < channels; c++)
            sum += interleaved[baseIdx + c];
          _fftBuf[i] = new Complex((sum / channels) * _window[i], 0);
        }
      }
      else
      {
        for (int i = 0; i < _opt.FftSize; i++)
          _fftBuf[i] = new Complex(interleaved[(pos + i) * channels] * _window[i], 0);
      }

      Fft.Forward(_fftBuf);

      // Build linear bands over [0, Nyquist]
      Array.Clear(_magnitudes, 0, _magnitudes.Length);

      int nyquistBins = _opt.FftSize / 2;
      for (int b = 0; b < _opt.Bands; b++)
      {
        int start = (int)((long)b * nyquistBins / _opt.Bands);
        int end = (int)((long)(b + 1) * nyquistBins / _opt.Bands);
        if (end <= start) end = start + 1;
        if (end > nyquistBins) end = nyquistBins;

        double acc = 0;
        for (int k = start; k < end; k++)
          acc += _fftBuf[k].Magnitude;
        _magnitudes[b] = acc;
      }

      builder.AddFrame(_magnitudes);

      pos += _opt.HopSize;
    }

    return builder.Finish(_opt.Width);
  }
}
