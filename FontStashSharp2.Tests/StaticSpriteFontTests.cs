using Microsoft.Xna.Framework;

namespace FontStashSharp2.Tests;

public sealed class StaticSpriteFontTests
{
	private static string FontPath(string fileName = "static-test.fnt")
	{
		return Path.Combine(AppContext.BaseDirectory, "Fonts", "StaticTest", fileName);
	}

	[Fact]
	public void TextDescriptor_LoadsMetricsAndMeasures()
	{
		using var font = new StaticSpriteFont(FontPath());

		Assert.Equal("Static Test", font.Name);
		Assert.Equal(16f, font.FontSize);
		Assert.Equal(20f, font.LineSpacing);
		Assert.Equal(15f, font.Ascent);
		Assert.Equal(-5f, font.Descent);

		Assert.Equal(new Vector2(18f, 20f), font.MeasureString("AB"));
		Assert.Equal(new Vector2(10f, 40f), font.MeasureString("A\nB"));
		Assert.Equal(new Vector2(20f, 20f), font.MeasureString("CA"));
	}

	[Fact]
	public void XmlAndBinaryDescriptors_Load()
	{
		foreach (var fileName in new[] { "static-test-xml.fnt", "static-test-binary.fnt" })
		{
			using var font = new StaticSpriteFont(FontPath(fileName));

			Assert.Equal("Static Test", font.Name);
			Assert.Equal(new Vector2(18f, 20f), font.MeasureString("AB"));
		}
	}

	[Fact]
	public void LineSpacing_CanBeChanged()
	{
		using var font = new StaticSpriteFont(FontPath());

		font.LineSpacing = 30f;

		Assert.Equal(30f, font.LineSpacing);
		Assert.Equal(new Vector2(10f, 60f), font.MeasureString("A\nB"));
		Assert.Throws<ArgumentOutOfRangeException>(() => font.LineSpacing = -1f);
		Assert.Throws<ArgumentOutOfRangeException>(() => font.LineSpacing = float.NaN);
	}

	[Fact]
	public void Dispose_WithoutDrawing_IsIdempotent()
	{
		var font = new StaticSpriteFont(FontPath());

		font.Dispose();
		font.Dispose();

		Assert.Throws<ObjectDisposedException>(() => font.MeasureString("A"));
	}
}
