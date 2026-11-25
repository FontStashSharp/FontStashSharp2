using System.Text;
using Microsoft.Xna.Framework;

namespace FontStashSharp2.Tests;

public sealed class MeasurementTests
{
	[Fact]
	public void MeasureString_Empty_ReturnsZero()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(32);

		Assert.Equal(Vector2.Zero, font.MeasureString(string.Empty));
		Assert.Equal(Vector2.Zero, font.MeasureString(string.Empty.AsSpan()));
		Assert.Equal(Vector2.Zero, font.MeasureString(new StringBuilder()));
	}

	[Fact]
	public void MeasureString_ReturnsAdvancesAndLineHeight()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(32);

		var size = font.MeasureString("Hello");

		Assert.True(size.X > 0f);
		Assert.Equal(font.LineSpacing, size.Y, 3);
	}

	[Fact]
	public void MeasureString_AppliesKerning()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(32);

		var a = font.MeasureString("A").X;
		var v = font.MeasureString("V").X;
		var av = font.MeasureString("AV").X;

		Assert.True(av < a + v, "The font's A/V kerning pair should reduce the measured width.");
	}

	[Fact]
	public void MeasureString_WithoutKerning_DoesNotApplyPairs()
	{
		using var fontSystem = TestFonts.CreateFontSystem(new FontSystemSettings { UseKerning = false });
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(32);

		var a = font.MeasureString("A").X;
		var v = font.MeasureString("V").X;
		var av = font.MeasureString("AV").X;

		Assert.Equal(a + v, av, 3);
	}

	[Fact]
	public void MeasureString_MultiLine_UsesWidestLineAndLineCount()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(24);

		var shortLine = font.MeasureString("abc").X;
		var longLine = font.MeasureString("abcdef").X;
		var size = font.MeasureString("abc\nabcdef");

		Assert.Equal(longLine, size.X, 3);
		Assert.True(shortLine < longLine);
		Assert.Equal(font.LineSpacing * 2f, size.Y, 3);
	}

	[Fact]
	public void MeasureString_TrailingNewline_AddsAnEmptyLine()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(24);

		var size = font.MeasureString("abc\n");

		Assert.Equal(font.LineSpacing * 2f, size.Y, 3);
	}

	[Fact]
	public void MeasureString_CRLF_CountsAsSingleLineBreak()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(24);

		var lf = font.MeasureString("a\nb");
		var crlf = font.MeasureString("a\r\nb");

		Assert.Equal(lf.X, crlf.X, 3);
		Assert.Equal(lf.Y, crlf.Y, 3);
	}

	[Fact]
	public void MeasureString_Tab_AdvancesLikeSpace()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(24);

		Assert.Equal(font.MeasureString(" ").X, font.MeasureString("\t").X, 3);
	}

	[Fact]
	public void MeasureString_StringBuilder_MatchesString()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(30);

		const string text = "String\nBuilder";
		var fromString = font.MeasureString(text);
		var fromBuilder = font.MeasureString(new StringBuilder(text));

		Assert.Equal(fromString.X, fromBuilder.X, 3);
		Assert.Equal(fromString.Y, fromBuilder.Y, 3);
	}

	[Fact]
	public void MeasureString_ScalesLinearlyWithFontSize()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);

		var small = fontSystem.GetFont(16).MeasureString("Kerning");
		var large = fontSystem.GetFont(32).MeasureString("Kerning");

		Assert.InRange(large.X, (small.X * 2f) - 0.01f, (small.X * 2f) + 0.01f);
		Assert.InRange(large.Y, (small.Y * 2f) - 0.01f, (small.Y * 2f) + 0.01f);
	}

	[Fact]
	public void LineSpacing_DefaultsToVerticalMetrics()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(32);

		Assert.Equal(font.Ascent - font.Descent + font.LineGap, font.LineSpacing, 3);
		Assert.True(font.Descent < 0f);
		Assert.True(font.LineSpacing > font.FontSize);
	}

	[Fact]
	public void LineSpacing_CanBeOverridden()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(24);

		font.LineSpacing = 10f;

		Assert.Equal(20f, font.MeasureString("a\nb").Y, 3);
	}

	[Fact]
	public void LineSpacing_RejectsInvalidValues()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(24);

		Assert.Throws<ArgumentOutOfRangeException>(() => font.LineSpacing = -1f);
		Assert.Throws<ArgumentOutOfRangeException>(() => font.LineSpacing = float.NaN);
		Assert.Throws<ArgumentOutOfRangeException>(() => font.LineSpacing = float.PositiveInfinity);
	}

	[Fact]
	public void MeasureString_UnpairedSurrogate_DoesNotThrow()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(24);

		var size = font.MeasureString("a\uD800b");

		Assert.True(float.IsFinite(size.X));
		Assert.True(float.IsFinite(size.Y));
	}

	[Fact]
	public void MeasureString_DoesNotRasterizeGlyphs()
	{
		var atlas = new RecordingGlyphAtlas();
		using var fontSystem = TestFonts.CreateFontSystem(atlas);
		fontSystem.AddFont(TestFonts.Data);
		var font = fontSystem.GetFont(32);

		font.MeasureString("Hello");

		Assert.Equal(0, font.GlyphCount);
		Assert.Empty(atlas.Glyphs);
		Assert.True(font.CachedGlyphCount > 0);
	}
}
