using SupersamplingSample;

string? screenshotPath = null;

for (var i = 0; i < args.Length; i++)
{
	switch (args[i])
	{
		case "--help":
		case "-h":
		case "/?":
			PrintUsage();
			return;
		case "--screenshot":
			if (i + 1 >= args.Length)
			{
				Console.Error.WriteLine("--screenshot requires a file path.");
				Environment.ExitCode = 1;
				return;
			}

			screenshotPath = Path.GetFullPath(args[++i]);
			break;
		default:
			if (args[i].StartsWith("--screenshot=", StringComparison.OrdinalIgnoreCase))
			{
				screenshotPath = Path.GetFullPath(args[i]["--screenshot=".Length..]);
			}
			else
			{
				Console.Error.WriteLine($"Unknown argument: {args[i]}");
				PrintUsage();
				Environment.ExitCode = 1;
				return;
			}

			break;
	}
}

using var game = new SupersamplingGame(screenshotPath);
game.Run();

static void PrintUsage()
{
	Console.WriteLine("FontStashSharp2 supersampling comparison");
	Console.WriteLine();
	Console.WriteLine("Usage: SupersamplingSample [options]");
	Console.WriteLine();
	Console.WriteLine("  --screenshot <path>  Save the comparison screen as a PNG and exit");
	Console.WriteLine("  --help               Show this help");
}
