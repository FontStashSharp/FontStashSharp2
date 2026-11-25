using BenchmarkSample;

BenchmarkOptions options;
try
{
	options = BenchmarkOptions.Parse(args);
}
catch (ArgumentException exception)
{
	Console.Error.WriteLine(exception.Message);
	Console.Error.WriteLine();
	BenchmarkOptions.PrintUsage();
	Environment.ExitCode = 1;
	return;
}

if (options.ShowHelp)
{
	BenchmarkOptions.PrintUsage();
	return;
}

using var game = new BenchmarkGame(options);
game.Run();
