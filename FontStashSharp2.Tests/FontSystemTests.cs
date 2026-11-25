using Microsoft.Xna.Framework;

namespace FontStashSharp2.Tests;

public sealed class FontSystemTests
{
	[Fact]
	public void MeasurementOnlySystem_HasNoTextures()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);

		Assert.Equal(0, fontSystem.TextureCount);
		Assert.Throws<InvalidOperationException>(() => fontSystem.GetTexture(0));
	}

	[Fact]
	public void GetFont_WithoutFonts_Throws()
	{
		using var fontSystem = TestFonts.CreateFontSystem();

		Assert.Throws<InvalidOperationException>(() => fontSystem.GetFont(16));
	}

	[Theory]
	[InlineData(0f)]
	[InlineData(-1f)]
	[InlineData(float.NaN)]
	[InlineData(float.PositiveInfinity)]
	[InlineData(float.NegativeInfinity)]
	public void GetFont_InvalidSize_Throws(float size)
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);

		Assert.Throws<ArgumentOutOfRangeException>(() => fontSystem.GetFont(size));
	}

	[Fact]
	public void GetFont_ExposesMetadata()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);

		var font = fontSystem.GetFont(28);

		Assert.Equal(28f, font.FontSize);
		Assert.True(font.Ascent > 0f);
		Assert.True(font.Descent < 0f);
		Assert.True(font.LineSpacing > 0f);
		Assert.Equal(0, font.GlyphCount);
	}

	[Fact]
	public void AddFont_InvalidatesCachedFonts()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var before = fontSystem.GetFont(20);

		fontSystem.AddFont(TestFonts.Data);

		var after = fontSystem.GetFont(20);
		Assert.NotSame(before, after);
	}

	[Fact]
	public void FontSources_AfterDispose_Throws()
	{
		var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		fontSystem.Dispose();

		Assert.Throws<ObjectDisposedException>(() => _ = fontSystem.FontSources);
		Assert.Throws<ObjectDisposedException>(() => fontSystem.GetFont(16));
		Assert.Throws<ObjectDisposedException>(() => fontSystem.AddFont(TestFonts.Data));
		Assert.Throws<ObjectDisposedException>(() => _ = fontSystem.TextureCount);
	}

	[Fact]
	public void Dispose_IsIdempotent()
	{
		var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.Dispose();
		fontSystem.Dispose();
	}

	[Fact]
	public void MeasureString_NullText_Throws()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(16);

		Assert.Throws<ArgumentNullException>(() => font.MeasureString((string)null!));
		Assert.Throws<ArgumentNullException>(() => font.MeasureString((System.Text.StringBuilder)null!));
		Assert.Throws<ArgumentNullException>(() => font.PreloadGlyphs((string)null!));
	}

	[Fact]
	public void DrawString_WithNullBatch_Throws()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(16);

		Microsoft.Xna.Framework.Graphics.SpriteBatch? spriteBatch = null;

		Assert.Throws<ArgumentNullException>(
			() => SpriteBatchExtensions.DrawString(spriteBatch!, font, "text", Vector2.Zero, Color.White));
	}
}
