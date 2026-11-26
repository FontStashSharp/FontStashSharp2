namespace FontStashSharp2.Tests;

public sealed class GlyphCacheTests
{
	[Fact]
	public void PreloadGlyphs_RasterizesUniqueGlyphsOnDemand()
	{
		var atlas = new RecordingGlyphAtlas();
		using var fontSystem = TestFonts.CreateFontSystem(atlas);
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(32);

		Assert.Equal(0, font.GlyphCount);

		var added = font.PreloadGlyphs(atlas, "Hello");

		// H, e, l, o (the second 'l' reuses the first).
		Assert.Equal(4, added);
		Assert.Equal(4, font.GlyphCount);
		Assert.Equal(4, atlas.Glyphs.Count);
	}

	[Fact]
	public void PreloadGlyphs_SecondCall_AddsNothing()
	{
		var atlas = new RecordingGlyphAtlas();
		using var fontSystem = TestFonts.CreateFontSystem(atlas);
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(32);

		var first = font.PreloadGlyphs(atlas, "Hello");
		var second = font.PreloadGlyphs(atlas, "Hello");

		Assert.Equal(4, first);
		Assert.Equal(0, second);
		Assert.Equal(4, atlas.Glyphs.Count);
	}

	[Fact]
	public void PreloadGlyphs_Whitespace_DoesNotCreateAtlasEntries()
	{
		var atlas = new RecordingGlyphAtlas();
		using var fontSystem = TestFonts.CreateFontSystem(atlas);
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(32);

		var added = font.PreloadGlyphs(atlas, "   ");

		Assert.Equal(0, added);
		Assert.Equal(0, font.GlyphCount);
		Assert.Empty(atlas.Glyphs);
	}

	[Fact]
	public void PreloadGlyphs_SkipsLineBreaks()
	{
		var atlas = new RecordingGlyphAtlas();
		using var fontSystem = TestFonts.CreateFontSystem(atlas);
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(32);

		var added = font.PreloadGlyphs(atlas, "a\r\nb");

		Assert.Equal(2, added);
		Assert.Equal(2, atlas.Glyphs.Count);
	}

	[Fact]
	public void PreloadGlyphs_ProducesCoveragePixels()
	{
		var atlas = new RecordingGlyphAtlas();
		using var fontSystem = TestFonts.CreateFontSystem(atlas);
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(48);

		font.PreloadGlyphs(atlas, "H");

		var glyph = Assert.Single(atlas.Glyphs);
		Assert.True(glyph.Width > 0);
		Assert.True(glyph.Height > 0);
		Assert.Equal(glyph.Width * glyph.Height, glyph.Alpha.Length);
		Assert.Contains(glyph.Alpha, value => value > 0);
		Assert.Contains(glyph.Alpha, value => value == 255);
	}

	[Fact]
	public void PreloadGlyphs_EmptyText_ReturnsZero()
	{
		var atlas = new RecordingGlyphAtlas();
		using var fontSystem = TestFonts.CreateFontSystem(atlas);
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(32);

		Assert.Equal(0, font.PreloadGlyphs(atlas, string.Empty));
		Assert.Equal(0, font.PreloadGlyphs(atlas, ReadOnlySpan<char>.Empty));
	}

	[Fact]
	public void GetFont_ReturnsTheSameCachedInstance()
	{
		var atlas = new RecordingGlyphAtlas();
		using var fontSystem = TestFonts.CreateFontSystem(atlas);
		fontSystem.AddFont(TestFonts.Data);

		var first = fontSystem.GetFont(32);
		var second = fontSystem.GetFont(32);

		Assert.Same(first, second);
		first.PreloadGlyphs(atlas, "Hi");
		Assert.Equal(0, second.PreloadGlyphs(atlas, "Hi"));
		Assert.Equal(2, atlas.Glyphs.Count);
	}

}
