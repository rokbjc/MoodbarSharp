/*
  Port of MoodbarBuilder from Clementine / exaile moodbar project.

  Original copyright:
    Copyright 2014, David Sansome <me@davidsansome.com>
  License:
    GNU GPL v3 (see COPYING in source distribution)

  Notes:
    - This class matches the logic of src/moodbar/moodbarbuilder.cpp/.h
    - Input to AddFrame is a magnitude spectrum (non-negative), one value per band.
*/

namespace MoodbarSharp;

/// <summary>
/// Accumulates FFT magnitude frames and converts them to normalised RGB moodbar data
/// by mapping spectrum energy across 24 Bark-scale critical bands into low, mid, and high colour channels.
/// </summary>
/// <remarks>
/// Port of <c>MoodbarBuilder</c> from the <a href="https://github.com/exaile/moodbar">exaile/moodbar</a> project
/// by David Sansome. Call <see cref="Init"/> once, then <see cref="AddFrame"/> per spectrum frame,
/// then <see cref="Finish"/> to retrieve the result.
/// </remarks>
public sealed class MoodbarBuilder
{
  private static readonly int[] _barkBands =
  [
      100, 200, 300, 400, 510, 630, 770, 920, 1_080, 1_270, 1_480, 1_720,
        2_000, 2_320, 2_700, 3_150, 3_700, 4_400, 5_300, 6_400, 7_700, 9_500, 12_000, 15_500
  ];

  private readonly List<int> _barkBandTable = [];
  private int _bands;
  private int _rateHz;

  private record struct Rgb(double R, double G, double B)
  {
    public enum Member
    {
      R,
      G,
      B
    }
    public double Get(Member member) => member switch
    {
      Member.R => this.R,
      Member.G => this.G,
      _ => this.B
    };

    public void Set(Member member, double value)
    {
      switch (member)
      {
        case Member.R: this.R = value; break;
        case Member.G: this.G = value; break;
        default: this.B = value; break;
      }
    }


  }

  private readonly List<Rgb> _frames = [];

  /// <summary>
  /// Initialises the builder for the given number of spectrum bands and sample rate.
  /// Must be called before <see cref="AddFrame"/>.
  /// </summary>
  public void Init(int bands, int rateHz)
  {
    if (bands <= 0) throw new ArgumentOutOfRangeException(nameof(bands));
    if (rateHz <= 0) throw new ArgumentOutOfRangeException(nameof(rateHz));

    _bands = bands;
    _rateHz = rateHz;

    _barkBandTable.Clear();
    _barkBandTable.Capacity = bands + 1;

    int barkBand = 0;
    for (int i = 0; i < bands + 1; ++i)
    {
      if (barkBand < _barkBands.Length - 1 && BandFrequency(i) >= _barkBands[barkBand])
      {
        barkBand++;
      }

      _barkBandTable.Add(barkBand);
    }
  }

  /// <summary>
  /// Matches integer math in original C++:
  /// </summary>
  /// <returns>((rate_hz_ / 2) * band + rate_hz_ / 4) / bands_</returns>
  private int BandFrequency(int band)
  {
    int rateHalf = _rateHz / 2;
    int rateQuarter = _rateHz / 4;
    return (rateHalf * band + rateQuarter) / _bands;
  }

  /// <summary>
  /// Processes one frame of spectrum magnitudes, one non-negative value per band.
  /// </summary>
  public void AddFrame(ReadOnlySpan<double> magnitudes)
  {
    if (_bands <= 0 || _rateHz <= 0)
      throw new InvalidOperationException("Call Init(bands, rateHz) before AddFrame.");

    if (magnitudes.Length > _barkBandTable.Count)
      return; // matches original early-return behavior

    // Calculate total magnitudes for different bark bands.
    Span<double> bands = stackalloc double[_barkBands.Length];
    for (int i = 0; i < bands.Length; i++) bands[i] = 0.0;

    for (int i = 0; i < magnitudes.Length; ++i)
    {
      int bark = _barkBandTable[i];
      if ((uint)bark < (uint)bands.Length)
      {
        bands[bark] += magnitudes[i];
      }
    }

    // Divide bark bands into thirds and compute total amplitudes.
    double r = 0, g = 0, b = 0;
    for (int i = 0; i < bands.Length; ++i)
    {
      double v = bands[i];
      double vv = v * v;

      int idx = (i * 3) / bands.Length; // 0..2
      switch (idx)
      {
        case 0:
          r += vv;
          break;
        case 1:
          g += vv;
          break;
        default:
          b += vv;
          break;
      }
    }

    _frames.Add(new Rgb(Math.Sqrt(r), Math.Sqrt(g), Math.Sqrt(b)));
  }

  /// <summary>
  /// Normalizes all accumulated frames and returns the moodbar as <paramref name="width"/> × 3 RGB bytes.
  /// </summary>
  public byte[] Finish(int width)
  {
    if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

    byte[] ret = new byte[width * 3];
    if (_frames.Count == 0) return ret;

    Normalize(_frames);

    int outIdx = 0;
    for (int i = 0; i < width; ++i)
    {
      int start = i * _frames.Count / width;
      int end = (i + 1) * _frames.Count / width;
      if (start == end) end = start + 1;
      if (end > _frames.Count) end = _frames.Count;

      double rr = 0, gg = 0, bb = 0;
      for (int j = start; j < end; j++)
      {
        var f = _frames[j];
        rr += f.R * 255.0;
        gg += f.G * 255.0;
        bb += f.B * 255.0;
      }

      int n = end - start;
      ret[outIdx++] = Util.ToByteClamped(rr / n);
      ret[outIdx++] = Util.ToByteClamped(gg / n);
      ret[outIdx++] = Util.ToByteClamped(bb / n);
    }

    return ret;
  }

  private static void Normalize(List<Rgb> vals)
  {
    Normalize(vals, Rgb.Member.R);
    Normalize(vals, Rgb.Member.G);
    Normalize(vals, Rgb.Member.B);
  }

  // Port of MoodbarBuilder::Normalize
  private static void Normalize(List<Rgb> vals, Rgb.Member member)
  {
    double mini = vals[0].Get(member);
    double maxi = mini;

    for (int i = 1; i < vals.Count; i++)
    {
      double value = vals[i].Get(member);
      if (value > maxi) maxi = value;
      else if (value < mini) mini = value;
    }

    double avg = 0;
    int t = 0;
    foreach (var var in vals)
    {
      double value = var.Get(member);
      if (Math.Abs(value - mini) < float.Epsilon || Math.Abs(value - maxi) < float.Epsilon) continue;
      avg += value / vals.Count;
      t++;
    }

    if (t == 0) avg = 0;

    double tu = 0, tb = 0, avgu = 0, avgb = 0;
    foreach (var var in vals)
    {
      double value = var.Get(member);
      if (Math.Abs(value - mini) < float.Epsilon || Math.Abs(value - maxi) < float.Epsilon) continue;
      if (value > avg)
      {
        avgu += value;
        tu++;
      }
      else
      {
        avgb += value;
        tb++;
      }
    }

    if (tu != 0) avgu /= tu; else avgu = avg;
    if (tb != 0) avgb /= tb; else avgb = avg;

    tu = 0;
    tb = 0;
    double avguu = 0, avgbb = 0;
    foreach (var val in vals)
    {
      double value = val.Get(member);
      if (Math.Abs(value - mini) > float.Epsilon && Math.Abs(value - maxi) > float.Epsilon)
      {
        if (value > avgu)
        {
          avguu += value;
          tu++;
        }
        else if (value < avgb)
        {
          avgbb += value;
          tb++;
        }
      }
    }

    if (tu != 0) avguu /= tu;
    else avguu = avgu;
    if (tb != 0) avgbb /= tb;
    else avgbb = avgb;

    mini = Math.Max(avg + (avgb - avg) * 2, avgbb);
    maxi = Math.Min(avg + (avgu - avg) * 2, avguu);

    double delta = maxi - mini;
    if (delta == 0) delta = 1;

    for (int i = 0; i < vals.Count; i++)
    {
      var rgb = vals[i];
      double value = rgb.Get(member);

      double norm = double.IsFinite(value)
          ? Math.Clamp((value - mini) / delta, 0.0, 1.0)
          : 0.0;

      rgb.Set(member, norm);
      vals[i] = rgb;
    }
  }
}
