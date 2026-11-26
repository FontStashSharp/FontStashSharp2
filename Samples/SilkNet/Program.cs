using SilkNetSample;

var frames = 0;

for (var i = 0; i < args.Length; i++)
{
	switch (args[i])
	{
		case "--help":
		case "-h":
		case "/?":
			PrintUsage();
			return;
		case "--frames":
			if (i + 1 >= args.Length || !int.TryParse(args[++i], out frames) || frames < 1)
			{
				Console.Error.WriteLine("--frames requires a positive integer.");
				Environment.ExitCode = 1;
				return;
			}

			break;
		default:
			Console.Error.WriteLine($"Unknown argument: {args[i]}");
			PrintUsage();
			Environment.ExitCode = 1;
			return;
	}
}

using var sample = new SilkSample(frames);
sample.Run();

static void PrintUsage()
{
	Console.WriteLine("FontStashSharp2 platform-agnostic Silk.NET sample");
	Console.WriteLine();
	Console.WriteLine("Usage: SilkNetSample [options]");
	Console.WriteLine();
	Console.WriteLine("  --frames <n>  Render n frames, then exit");
	Console.WriteLine("  --help        Show this help");
}
