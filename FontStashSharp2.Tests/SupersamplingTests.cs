namespace FontStashSharp2.Tests;

public sealed class SupersamplingTests
{
	[Fact]
	public void RasterizesGlyphsAtHigherResolution()
	{
		var normalAtlas = new RecordingGlyphAtlas();
		using var normalSystem = TestFonts.CreateFontSystem(
			normalAtlas,
			new FontSystemSettings { SupersamplingFactor = 1f });
		normalSystem.AddFont(TestFonts.Data);
		normalSystem.GetFont(32).PreloadGlyphs("H");

		var supersampledAtlas = new RecordingGlyphAtlas();
		using var supersampledSystem = TestFonts.CreateFontSystem(
			supersampledAtlas,
			new FontSystemSettings { SupersamplingFactor = 2f });
		supersampledSystem.AddFont(TestFonts.Data);
		supersampledSystem.GetFont(32).PreloadGlyphs("H");

		var normal = Assert.Single(normalAtlas.Glyphs);
		var supersampled = Assert.Single(supersampledAtlas.Glyphs);

		Assert.InRange(supersampled.Width, (normal.Width * 2) - 2, (normal.Width * 2) + 2);
		Assert.InRange(supersampled.Height, (normal.Height * 2) - 2, (normal.Height * 2) + 2);
		Assert.True(supersampled.Width > normal.Width);
		Assert.True(supersampled.Height > normal.Height);
	}

	[Fact]
	public void MeasurementIsUnaffectedBySupersampling()
	{
		using var normalSystem = TestFonts.CreateFontSystem(
			new RecordingGlyphAtlas(),
			new FontSystemSettings { SupersamplingFactor = 1f });
		normalSystem.AddFont(TestFonts.Data);
		var normal = normalSystem.GetFont(24);

		using var supersampledSystem = TestFonts.CreateFontSystem(
			new RecordingGlyphAtlas(),
			new FontSystemSettings { SupersamplingFactor = 2f });
		supersampledSystem.AddFont(TestFonts.Data);
		var supersampled = supersampledSystem.GetFont(24);

		var normalSize = normal.MeasureString("Hello\nWorld");
		var supersampledSize = supersampled.MeasureString("Hello\nWorld");

		Assert.Equal(normalSize.X, supersampledSize.X, 3);
		Assert.Equal(normalSize.Y, supersampledSize.Y, 3);
		Assert.Equal(normal.Ascent, supersampled.Ascent, 3);
		Assert.Equal(normal.LineSpacing, supersampled.LineSpacing, 3);
	}

	[Fact]
	public void FontExposesConfiguredFactor()
	{
		using var fontSystem = TestFonts.CreateFontSystem(
			new RecordingGlyphAtlas(),
			new FontSystemSettings { SupersamplingFactor = 1.5f });
		fontSystem.AddFont(TestFonts.Data);

		var font = fontSystem.GetFont(20);

		Assert.Equal(1.5f, font.SupersamplingFactor);
		Assert.Equal(1.5f, fontSystem.Settings.SupersamplingFactor);
	}

	[Fact]
	public void PreloadingCountsUniqueGlyphsIndependentOfFactor()
	{
		var atlas = new RecordingGlyphAtlas();
		using var fontSystem = TestFonts.CreateFontSystem(
			atlas,
			new FontSystemSettings { SupersamplingFactor = 3f });
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(24);

		var added = font.PreloadGlyphs("Hello");

		Assert.Equal(4, added);
		Assert.Equal(4, font.GlyphCount);
		Assert.Equal(4, atlas.Glyphs.Count);
	}
}
