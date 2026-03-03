namespace MoodbarSharp;

internal static class Util
{
  public static byte ToByteClamped(double v)
  {
    return v switch
    {
      <= 0 => 0,
      >= 255 => 255,
      _ => (byte)v
    };
  }
}
