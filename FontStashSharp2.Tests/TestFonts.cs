using FontStashSharp2.Internal;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FontStashSharp2.Tests;

internal static class TestFonts
{
	private static readonly Lazy<byte[]> LazyData = new(
		() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fonts", "ABeeZee-Regular.ttf")),
		LazyThreadSafetyMode.ExecutionAndPublication);

	public static byte[] Data => LazyData.Value;

	public static FontSystem CreateFontSystem(FontSystemSettings? settings = null)
	{
		return new FontSystem(settings);
	}

	public static FontSystem CreateFontSystem(IGlyphAtlas atlas, FontSystemSettings? settings = null)
	{
		return FontSystem.CreateWithAtlas(atlas, settings);
	}
}

internal sealed record RecordedGlyph(int Width, int Height, byte[] Alpha);

internal sealed class RecordingGlyphAtlas : IGlyphAtlas
{
	public List<RecordedGlyph> Glyphs { get; } = new();

	public int TextureCount => 1;

	public void ValidateDevice(GraphicsDevice graphicsDevice)
	{
	}

	public GlyphPlacement Add(GraphicsDevice? graphicsDevice, ReadOnlySpan<byte> alphaBitmap, int width, int height)
	{
		var alpha = alphaBitmap[..(width * height)].ToArray();
		var index = Glyphs.Count;
		Glyphs.Add(new RecordedGlyph(width, height, alpha));

		var bounds = new Rectangle((index % 16) * 64, (index / 16) * 64, width, height);
		return new GlyphPlacement(0, bounds);
	}
}
