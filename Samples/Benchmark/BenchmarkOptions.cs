using System.Globalization;

namespace BenchmarkSample;

internal sealed class BenchmarkOptions
{
	public bool AutoExit { get; private init; }

	public bool ShowHelp { get; private init; }

	public bool DisableKerning { get; private init; }

	public int Rounds { get; private init; } = 3;

	public int Iterations { get; private init; } = 100;

	public int Texts { get; private init; } = 128;

	public int WarmupFrames { get; private init; } = 8;

	public float FontSize { get; private init; } = 18f;

	public string? ScreenshotPath { get; private init; }

	public static BenchmarkOptions Parse(string[] args)
	{
		var options = new BenchmarkOptions();

		for (var i = 0; i < args.Length; i++)
		{
			var arg = args[i];

			switch (arg)
			{
				case "--benchmark":
					options = options.With(autoExit: true);
					break;
				case "--no-kerning":
					options = options.With(disableKerning: true);
					break;
				case "--help":
				case "-h":
				case "/?":
					options = options.With(showHelp: true);
					break;
				case "--rounds":
					options = options.With(rounds: ReadPositiveInt(args, ref i, arg));
					break;
				case "--iterations":
					options = options.With(iterations: ReadPositiveInt(args, ref i, arg));
					break;
				case "--texts":
					options = options.With(texts: ReadPositiveInt(args, ref i, arg));
					break;
				case "--warmup":
					options = options.With(warmupFrames: ReadNonNegativeInt(args, ref i, arg));
					break;
				case "--font-size":
					options = options.With(fontSize: ReadPositiveFloat(args, ref i, arg));
					break;
				case "--screenshot":
					options = options.With(screenshotPath: ReadScreenshotPath(ReadValue(args, ref i, arg), arg));
					break;
				default:
					if (arg.StartsWith("--screenshot=", StringComparison.OrdinalIgnoreCase))
					{
						var value = arg["--screenshot=".Length..];
						options = options.With(screenshotPath: ReadScreenshotPath(value, arg));
					}
					else
					{
						throw new ArgumentException($"Unknown argument: {arg}");
					}

					break;
			}
		}

		return options;
	}

	public static void PrintUsage()
	{
		Console.WriteLine("FontStashSharp2 DrawString benchmark");
		Console.WriteLine();
		Console.WriteLine("Usage: BenchmarkSample [options]");
		Console.WriteLine();
		Console.WriteLine("  --benchmark          Run the benchmark, print results, and exit");
		Console.WriteLine("  --rounds <n>         Measured rounds per mode/target (default 3)");
		Console.WriteLine("  --iterations <n>     Frames per measured phase (default 100)");
		Console.WriteLine("  --texts <n>          DrawString calls per frame (default 128)");
		Console.WriteLine("  --warmup <n>         Warmup frames per mode/target (default 8)");
		Console.WriteLine("  --font-size <n>      Font size in pixels (default 18)");
		Console.WriteLine("  --no-kerning         Disable FontStashSharp2 kerning pairs");
		Console.WriteLine("  --screenshot <path>  Save the results screen as a PNG and exit");
		Console.WriteLine("  --help               Show this help");
		Console.WriteLine();
		Console.WriteLine("The benchmark pre-bakes the same TTF at the same size for both engines.");
		Console.WriteLine("Warmup and glyph rasterization are excluded from the measured phases.");
	}

	private BenchmarkOptions With(
		bool? autoExit = null,
		bool? showHelp = null,
		bool? disableKerning = null,
		int? rounds = null,
		int? iterations = null,
		int? texts = null,
		int? warmupFrames = null,
		float? fontSize = null,
		string? screenshotPath = null)
	{
		return new BenchmarkOptions
		{
			AutoExit = autoExit ?? AutoExit,
			ShowHelp = showHelp ?? ShowHelp,
			DisableKerning = disableKerning ?? DisableKerning,
			Rounds = rounds ?? Rounds,
			Iterations = iterations ?? Iterations,
			Texts = texts ?? Texts,
			WarmupFrames = warmupFrames ?? WarmupFrames,
			FontSize = fontSize ?? FontSize,
			ScreenshotPath = screenshotPath ?? ScreenshotPath,
		};
	}

	private static string ReadValue(string[] args, ref int index, string option)
	{
		if (index + 1 >= args.Length)
		{
			throw new ArgumentException($"Missing value for {option}.");
		}

		return args[++index];
	}

	private static int ReadPositiveInt(string[] args, ref int index, string option)
	{
		var value = ReadValue(args, ref index, option);
		if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
		{
			throw new ArgumentException($"{option} requires a positive integer, got '{value}'.");
		}

		return parsed;
	}

	private static int ReadNonNegativeInt(string[] args, ref int index, string option)
	{
		var value = ReadValue(args, ref index, option);
		if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
		{
			throw new ArgumentException($"{option} requires a non-negative integer, got '{value}'.");
		}

		return parsed;
	}

	private static float ReadPositiveFloat(string[] args, ref int index, string option)
	{
		var value = ReadValue(args, ref index, option);
		if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
			|| !float.IsFinite(parsed)
			|| parsed <= 0f)
		{
			throw new ArgumentException($"{option} requires a positive number, got '{value}'.");
		}

		return parsed;
	}

	private static string ReadScreenshotPath(string value, string option)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException($"{option} requires a file path.");
		}

		return Path.GetFullPath(value);
	}
}
