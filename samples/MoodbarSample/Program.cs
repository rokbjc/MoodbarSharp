using MoodbarSharp;
using NAudio.Wave;
using SkiaSharp;

if (args.Length != 1)
{
  Console.Error.WriteLine("Usage: MoodbarSample <audio-file>");
  return 1;
}

string inputPath = args[0];
if (!File.Exists(inputPath))
{
  Console.Error.WriteLine($"File not found: {inputPath}");
  return 1;
}

// Decode audio to interleaved float PCM
float[] samples;
int sampleRate;
int channels;

using (var reader = new AudioFileReader(inputPath))
{
  sampleRate = reader.WaveFormat.SampleRate;
  channels = reader.WaveFormat.Channels;

  var allSamples = new List<float>();
  float[] buffer = new float[4_096 * channels];
  int read;
  while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
  {
    for (int i = 0; i < read; i++)
      allSamples.Add(buffer[i]);
  }
  samples = allSamples.ToArray();
}

// Generate moodbar
var gen = new MoodbarGenerator(new MoodbarOptions { Width = 1_000 });
byte[] rgb = gen.GenerateFromFloatPcm(samples, sampleRate, channels);

// Write .mood file (raw RGB bytes)
string moodPath = Path.ChangeExtension(inputPath, ".mood");
File.WriteAllBytes(moodPath, rgb);

// Render 1000x32 JPEG — each column is a vertical stripe of its RGB colour
const int imageWidth = 1_000;
const int imageHeight = 32;

string jpgPath = Path.ChangeExtension(inputPath, ".jpg");
using (var bitmap = new SKBitmap(imageWidth, imageHeight))
{
  for (int x = 0; x < imageWidth; x++)
  {
    var color = new SKColor(rgb[x * 3], rgb[x * 3 + 1], rgb[x * 3 + 2]);
    for (int y = 0; y < imageHeight; y++)
      bitmap.SetPixel(x, y, color);
  }

  using var image = SKImage.FromBitmap(bitmap);
  using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
  using var stream = File.OpenWrite(jpgPath);
  data.SaveTo(stream);
}

Console.WriteLine($"Written {moodPath} and {jpgPath}");
return 0;
