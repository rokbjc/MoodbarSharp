namespace MoodbarSharp;

/// <summary>
/// Configuration for <see cref="MoodbarGenerator"/>.
/// </summary>
public sealed class MoodbarOptions
{
  /// <summary>Number of output columns (each column = 3 bytes RGB).</summary>
  public int Width { get; set; } = 1_000;

  /// <summary>Number of spectrum bands used by the builder (matches MoodbarPipeline::kBands in the original code).</summary>
  public int Bands { get; set; } = 128;

  /// <summary>FFT size (power of two).</summary>
  public int FftSize { get; set; } = 2_048;

  /// <summary>Hop size in samples (per channel) between consecutive FFT frames.</summary>
  public int HopSize { get; set; } = 512;

  /// <summary>Whether to downmix to mono before analysis.</summary>
  public bool DownmixToMono { get; set; } = true;
}
