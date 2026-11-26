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
		Assert.Equal(2f, settings.SupersamplingFactor);
		Assert.True(settings.UseKerning);
		Assert.Equal(FontScaleMode.ScaleForPixelHeight, settings.ScaleMode);
		Assert.Equal(GlyphAlphaMode.Premultiplied, settings.AlphaMode);
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

	[Theory]
	[InlineData(0f)]
	[InlineData(0.5f)]
	[InlineData(-1f)]
	public void SupersamplingFactor_RejectsValuesBelowOne(float value)
	{
		var settings = new FontSystemSettings();

		Assert.Throws<ArgumentOutOfRangeException>(() => settings.SupersamplingFactor = value);
	}

	[Theory]
	[InlineData(float.NaN)]
	[InlineData(float.PositiveInfinity)]
	[InlineData(float.NegativeInfinity)]
	public void SupersamplingFactor_RejectsNonFiniteValues(float value)
	{
		var settings = new FontSystemSettings();

		Assert.Throws<ArgumentOutOfRangeException>(() => settings.SupersamplingFactor = value);
	}

	[Fact]
	public void ScaleMode_RejectsUndefinedValues()
	{
		var settings = new FontSystemSettings();

		Assert.Throws<ArgumentOutOfRangeException>(() => settings.ScaleMode = (FontScaleMode)99);
	}

	[Fact]
	public void AlphaMode_RejectsUndefinedValues()
	{
		var settings = new FontSystemSettings();

		Assert.Throws<ArgumentOutOfRangeException>(() => settings.AlphaMode = (GlyphAlphaMode)99);
	}

	[Fact]
	public void Settings_AreCopiedByFontSystem()
	{
		var settings = new FontSystemSettings
		{
			GlyphPadding = 3,
			SupersamplingFactor = 1.5f,
			ScaleMode = FontScaleMode.ScaleForMappingEmToPixels,
			AlphaMode = GlyphAlphaMode.NoAntialiasing,
		};
		var atlas = new RecordingGlyphAtlas();
		using var fontSystem = TestFonts.CreateFontSystem(atlas, settings);

		settings.GlyphPadding = 9;
		settings.SupersamplingFactor = 3f;
		settings.ScaleMode = FontScaleMode.ScaleForPixelHeight;
		settings.AlphaMode = GlyphAlphaMode.NonPremultiplied;

		// The system holds its own validated copy; the public settings object can be reused safely.
		Assert.Equal(3, fontSystem.Settings.GlyphPadding);
		Assert.Equal(1.5f, fontSystem.Settings.SupersamplingFactor);
		Assert.Equal(FontScaleMode.ScaleForMappingEmToPixels, fontSystem.Settings.ScaleMode);
		Assert.Equal(GlyphAlphaMode.NoAntialiasing, fontSystem.Settings.AlphaMode);
		Assert.Equal(9, settings.GlyphPadding);
		Assert.Equal(3f, settings.SupersamplingFactor);
		Assert.Equal(FontScaleMode.ScaleForPixelHeight, settings.ScaleMode);
		Assert.Equal(GlyphAlphaMode.NonPremultiplied, settings.AlphaMode);
	}
}
