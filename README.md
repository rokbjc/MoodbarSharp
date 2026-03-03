# MoodbarSharp

[![CI](https://github.com/rokbjc/MoodbarSharp/actions/workflows/ci.yml/badge.svg)](https://github.com/rokbjc/MoodbarSharp/actions/workflows/ci.yml)

Pure .NET moodbar generator — a port of `MoodbarBuilder` from the [exaile/moodbar](https://github.com/exaile/moodbar) project by [David Sansome](https://github.com/davidsansome). Licensed under GPLv3 (see [`COPYING`](COPYING)).

## What this library does

- Computes a simple FFT magnitude spectrum from PCM audio
- Maps spectrum energy to 3 Bark-band regions (low/mid/high)
- Normalizes each channel using the same heuristic as the original C++ implementation
- Outputs `width * 3` bytes: RGB triplets per column

## Usage

```csharp
using MoodbarSharp;

var gen = new MoodbarGenerator(new MoodbarOptions
{
    Width = 1000,
    Bands = 128,
    FftSize = 2048,
    HopSize = 512,
});

byte[] rgb = gen.GenerateFromFloatPcm(pcmInterleavedFloats, sampleRate: 44100, channels: 2);
// rgb.Length == 3000 for Width=1000
```

## Sample project

See [`samples/MoodbarSample`](samples/MoodbarSample) for a CLI that reads any audio file via NAudio and writes a `.mood` file and a JPEG:

```bash
dotnet run --project samples/MoodbarSample -- path/to/song.mp3
```

## Notes

- Input must be decoded to PCM elsewhere (e.g. NAudio, FFmpeg). This library intentionally does not include decoding.
- Exact byte-for-byte parity with the original GStreamer pipeline is not guaranteed.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

GPLv3 — see [COPYING](COPYING).