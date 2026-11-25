using System.Globalization;
using FontStashSharp2;

namespace MeasureTextSample;

/// <summary>
/// A console sample showing that parsing, fallback, measurement, kerning, and line layout work
/// without a graphics device. Only rasterization needs one.
/// </summary>
internal static class Program
{
	private static void Main(string[] args)
	{
		var fontSize = args.Length > 0 && float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
			? parsed
			: 32f;
		var text = args.Length > 1 ? args[1] : "FontStashSharp2 measures text without a GraphicsDevice.";

		var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "ABeeZee-Regular.ttf");
		if (!File.Exists(fontPath))
		{
			Console.Error.WriteLine($"Font not found: {fontPath}");
			return;
		}

		using var fontSystem = new FontSystem();
		var source = fontSystem.AddFont(File.ReadAllBytes(fontPath));
		var font = fontSystem.GetFont(fontSize);

		Console.WriteLine($"Font:           {source.Name}");
		Console.WriteLine($"Font units:     ascent {source.Ascent}, descent {source.Descent}, line gap {source.LineGap}");
		Console.WriteLine(string.Create(
			CultureInfo.InvariantCulture,
			$"At {fontSize:0.##}px:   ascent {font.Ascent:0.00}, descent {font.Descent:0.00}, line spacing {font.LineSpacing:0.00}"));
		Console.WriteLine();

		WriteMeasurement(font, "Hello, world!");
		WriteMeasurement(font, "AV AV AV - kerning pair");
		WriteMeasurement(font, "Two\nlines");
		WriteMeasurement(font, "Trailing newline\n");
		WriteMeasurement(font, "Missing glyph: \U0010FFFF");
		Console.WriteLine();

		var sentence = "Center this string";
		var sentenceSize = font.MeasureString(sentence);
		Console.WriteLine($"Text:           \"{sentence}\"");
		Console.WriteLine(string.Create(
			CultureInfo.InvariantCulture,
			$"Size:           {sentenceSize.X:0.00} x {sentenceSize.Y:0.00}"));
		Console.WriteLine(string.Create(
			CultureInfo.InvariantCulture,
			$"In a 640x360 view: position = ({320f - (sentenceSize.X / 2f):0.00}, {180f - (sentenceSize.Y / 2f):0.00})"));
		Console.WriteLine();

		Console.WriteLine($"Cached metrics: {font.CachedGlyphCount}");
		Console.WriteLine($"Rasterized:     {font.GlyphCount} (measurement does not touch an atlas)");
		Console.WriteLine($"Atlas pages:    {fontSystem.TextureCount}");
	}

	private static void WriteMeasurement(DynamicSpriteFont font, string text)
	{
		var size = font.MeasureString(text);
		var escaped = text.Replace("\r", "\\r").Replace("\n", "\\n", StringComparison.Ordinal);
		Console.WriteLine(string.Create(
			CultureInfo.InvariantCulture,
			$"  \"{escaped}\" -> {size.X,8:0.00} x {size.Y,7:0.00} px"));
	}
}
