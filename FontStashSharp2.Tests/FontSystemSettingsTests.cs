namespace FontStashSharp2.Tests;

public sealed class FontSystemSettingsTests
{
	[Fact]
	public void Defaults_AreSensible()
	{
		var settings = new FontSystemSettings();

		Assert.Equal(1024, settings.TextureWidth);
		Assert.Equal(1024, settings.TextureHeight);
		Assert.Equal(8, settings.MaxTextureCount);
		Assert.Equal(1, settings.GlyphPadding);
		Assert.True(settings.UseKerning);
		Assert.True(settings.PremultiplyAlpha);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void TextureWidth_RejectsNonPositive(int value)
	{
		var settings = new FontSystemSettings();

		Assert.Throws<ArgumentOutOfRangeException>(() => settings.TextureWidth = value);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void TextureHeight_RejectsNonPositive(int value)
	{
		var settings = new FontSystemSettings();

		Assert.Throws<ArgumentOutOfRangeException>(() => settings.TextureHeight = value);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void MaxTextureCount_RejectsNonPositive(int value)
	{
		var settings = new FontSystemSettings();

		Assert.Throws<ArgumentOutOfRangeException>(() => settings.MaxTextureCount = value);
	}

	[Fact]
	public void GlyphPadding_RejectsNegative()
	{
		var settings = new FontSystemSettings();

		Assert.Throws<ArgumentOutOfRangeException>(() => settings.GlyphPadding = -1);
	}

	[Fact]
	public void Settings_AreCopiedByFontSystem()
	{
		var settings = new FontSystemSettings { GlyphPadding = 3 };
		var atlas = new RecordingGlyphAtlas();
		using var fontSystem = TestFonts.CreateFontSystem(atlas, settings);

		settings.GlyphPadding = 9;

		// The system holds its own validated copy; the public settings object can be reused safely.
		Assert.Equal(3, fontSystem.Settings.GlyphPadding);
		Assert.Equal(9, settings.GlyphPadding);
	}
}
