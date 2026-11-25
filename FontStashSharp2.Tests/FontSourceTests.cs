using System.Text;
using Microsoft.Xna.Framework;

namespace FontStashSharp2.Tests;

public sealed class FontSourceTests
{
	[Fact]
	public void AddFont_ParsesMetricsAndName()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		var source = fontSystem.AddFont(TestFonts.Data);

		Assert.Equal(0, source.Index);
		Assert.Equal("ABeeZee Regular", source.Name);
		Assert.True(source.Ascent > 0);
		Assert.True(source.Descent < 0);
		Assert.True(source.LineGap >= 0);
	}

	[Fact]
	public void AddFont_UsesProvidedName()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		var source = fontSystem.AddFont(TestFonts.Data, name: "My Font");

		Assert.Equal("My Font", source.Name);
	}

	[Fact]
	public void AddFont_FromStream()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		using var stream = new MemoryStream(TestFonts.Data, writable: false);
		var source = fontSystem.AddFont(stream);

		Assert.True(source.ContainsGlyph('A'));
	}

	[Fact]
	public void AddFont_FromFile()
	{
		var path = Path.Combine(AppContext.BaseDirectory, "Fonts", "ABeeZee-Regular.ttf");
		using var fontSystem = TestFonts.CreateFontSystem();
		var source = fontSystem.AddFont(path);

		Assert.True(source.ContainsGlyph('A'));
	}

	[Fact]
	public void ContainsGlyph_IsTrueForPrintableAscii()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		var source = fontSystem.AddFont(TestFonts.Data);

		for (var codepoint = 32; codepoint <= 126; codepoint++)
		{
			Assert.True(source.ContainsGlyph(codepoint), $"Missing glyph for U+{codepoint:X4} '{(char)codepoint}'.");
			Assert.NotEqual(0, source.GetGlyphIndex(codepoint));
		}
	}

	[Fact]
	public void ContainsGlyph_IsFalseForUnmappedCodePoint()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		var source = fontSystem.AddFont(TestFonts.Data);

		Assert.False(source.ContainsGlyph(0x10FFFF));
		Assert.Equal(0, source.GetGlyphIndex(0x10FFFF));
	}

	[Fact]
	public void AddFont_InvalidData_Throws()
	{
		using var fontSystem = TestFonts.CreateFontSystem();

		Assert.Throws<InvalidDataException>(() => fontSystem.AddFont(new byte[] { 1, 2, 3, 4 }));
	}

	[Fact]
	public void AddFont_FontIndexOutOfRange_Throws()
	{
		using var fontSystem = TestFonts.CreateFontSystem();

		Assert.Throws<ArgumentOutOfRangeException>(() => fontSystem.AddFont(TestFonts.Data, fontIndex: 1));
	}

	[Fact]
	public void AddFont_NullData_Throws()
	{
		using var fontSystem = TestFonts.CreateFontSystem();

		Assert.Throws<ArgumentNullException>(() => fontSystem.AddFont((byte[])null!));
	}

	[Fact]
	public void FontSources_PreserveFallbackOrder()
	{
		using var fontSystem = TestFonts.CreateFontSystem();
		var first = fontSystem.AddFont(TestFonts.Data, name: "First");
		var second = fontSystem.AddFont(TestFonts.Data, name: "Second");

		Assert.Equal(2, fontSystem.FontSources.Count);
		Assert.Same(first, fontSystem.FontSources[0]);
		Assert.Same(second, fontSystem.FontSources[1]);
		Assert.Equal(1, second.Index);
	}
}
