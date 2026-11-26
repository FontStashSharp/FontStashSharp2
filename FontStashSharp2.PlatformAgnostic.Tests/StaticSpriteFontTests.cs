using System.Numerics;
using FontStashSharp2;

namespace FontStashSharp2.PlatformAgnostic.Tests;

public sealed class StaticSpriteFontTests
{
	private static string FontPath(string fileName = "static-test.fnt")
	{
		return Path.Combine(AppContext.BaseDirectory, "Fonts", "StaticTest", fileName);
	}

	private static byte[] GetPagePixel(FakeTextureManager manager, int x, int y)
	{
		var texture = manager.Textures[0];
		var offset = ((y * texture.Width) + x) * 4;
		return texture.Pixels[offset..(offset + 4)];
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
		Assert.Equal(new Vector2(5f, 20f), font.MeasureString(" "));
		Assert.Equal(new Vector2(5f, 20f), font.MeasureString("\t"));
		Assert.Equal(new Vector2(6f, 20f), font.MeasureString("\u4E00"));
		Assert.Equal(Vector2.Zero, font.MeasureString(string.Empty));
	}

	[Fact]
	public void XmlAndBinaryDescriptors_Load()
	{
		foreach (var fileName in new[] { "static-test-xml.fnt", "static-test-binary.fnt" })
		{
			using var font = new StaticSpriteFont(FontPath(fileName));

			Assert.Equal("Static Test", font.Name);
			Assert.Equal(20f, font.LineSpacing);
			Assert.Equal(new Vector2(18f, 20f), font.MeasureString("AB"));
		}
	}

	[Fact]
	public void DrawText_UploadsPagesAndPositionsGlyphs()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var font = new StaticSpriteFont(FontPath());
		font.DrawText(renderer, "AB", new Vector2(100f, 200f), Color.White);

		Assert.Equal(2, renderer.Calls.Count);
		Assert.Single(manager.Textures);

		var first = renderer.Calls[0];
		Assert.Same(manager.Textures[0], first.Texture);
		Assert.Equal(new Rectangle(0, 0, 8, 10), first.SourceRectangle);
		Assert.Equal(new Vector2(101f, 202f), first.Position);

		var second = renderer.Calls[1];
		Assert.Same(manager.Textures[0], second.Texture);
		Assert.Equal(new Rectangle(8, 0, 8, 10), second.SourceRectangle);
		Assert.Equal(new Vector2(109f, 201f), second.Position);

		Assert.Equal(new byte[] { 255, 0, 0, 255 }, manager.Textures[0].Pixels[..4]);
		var greenOffset = ((0 * 16) + 8) * 4;
		Assert.Equal(
			new byte[] { 0, 255, 0, 255 },
			manager.Textures[0].Pixels[greenOffset..(greenOffset + 4)]);

		renderer.Calls.Clear();
		font.DrawText(renderer, "C", Vector2.Zero, Color.White);

		Assert.Single(renderer.Calls);
		Assert.Equal(2, manager.Textures.Count);
		Assert.Same(manager.Textures[1], renderer.Calls[0].Texture);
		Assert.Equal(new Rectangle(0, 0, 6, 8), renderer.Calls[0].SourceRectangle);
		Assert.Equal(new Vector2(2f, 3f), renderer.Calls[0].Position);
		Assert.Equal(new byte[] { 0, 0, 255, 255 }, manager.Textures[1].Pixels[..4]);
	}

	[Fact]
	public void MissingGlyph_UsesNotDefCharacter()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var font = new StaticSpriteFont(FontPath());
		font.DrawText(renderer, "\u4E00B", Vector2.Zero, Color.White);

		Assert.Equal(2, renderer.Calls.Count);
		Assert.Equal(new Rectangle(0, 0, 4, 4), renderer.Calls[0].SourceRectangle);
		Assert.Equal(new Vector2(6f, 1f), renderer.Calls[1].Position);
	}

	[Fact]
	public void DrawText_AppliesScaleEffectsAndSpacing()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var font = new StaticSpriteFont(FontPath());
		var effectColor = new Color(255, 0, 0);

		font.DrawText(
			renderer,
			"A",
			new Vector2(10f, 20f),
			Color.White,
			0f,
			Vector2.Zero,
			new Vector2(2f, 3f),
			SpriteEffects.None,
			0f,
			effect: TextEffectOptions.Shadow(2, effectColor));

		Assert.Equal(2, renderer.Calls.Count);
		Assert.Equal(new Vector2(16f, 32f), renderer.Calls[0].Position);
		Assert.Equal(effectColor, renderer.Calls[0].Color);
		Assert.Equal(new Vector2(12f, 26f), renderer.Calls[1].Position);
		Assert.Equal(Color.White, renderer.Calls[1].Color);
		Assert.Equal(new Vector2(2f, 3f), renderer.Calls[1].Scale);

		renderer.Calls.Clear();
		font.DrawText(
			renderer,
			"AB",
			Vector2.Zero,
			Color.White,
			0f,
			Vector2.Zero,
			new Vector2(3f, 1f),
			SpriteEffects.None,
			0f,
			characterSpacing: 5f);

		Assert.Equal(2, renderer.Calls.Count);
		Assert.Equal(42f, renderer.Calls[1].Position.X);
	}

	[Fact]
	public void SpriteEffects_MirrorTheWholeBlock()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var font = new StaticSpriteFont(FontPath());
		font.DrawText(
			renderer,
			"AB",
			Vector2.Zero,
			Color.White,
			0f,
			Vector2.Zero,
			Vector2.One,
			SpriteEffects.FlipHorizontally,
			0f);

		Assert.Equal(2, renderer.Calls.Count);
		Assert.Equal(9f, renderer.Calls[0].Position.X);
		Assert.Equal(1f, renderer.Calls[1].Position.X);

		renderer.Calls.Clear();
		font.DrawText(
			renderer,
			"A\nB",
			Vector2.Zero,
			Color.White,
			0f,
			Vector2.Zero,
			Vector2.One,
			SpriteEffects.FlipVertically,
			0f);

		Assert.Equal(2, renderer.Calls.Count);
		Assert.Equal(28f, renderer.Calls[0].Position.Y);
		Assert.Equal(9f, renderer.Calls[1].Position.Y);
	}

	[Fact]
	public void LineSpacing_ScalesWithDrawScale()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var font = new StaticSpriteFont(FontPath());
		font.DrawText(
			renderer,
			"A\nB",
			Vector2.Zero,
			Color.White,
			0f,
			Vector2.Zero,
			new Vector2(1f, 2f),
			SpriteEffects.None,
			0f,
			lineSpacing: 7f);

		Assert.Equal(2, renderer.Calls.Count);
		Assert.Equal(56f, renderer.Calls[1].Position.Y);
	}

	[Fact]
	public void AlphaMode_PremultipliesPageImagesByDefault()
	{
		var manager = new FakeTextureManager();
		using var font = new StaticSpriteFont(FontPath());

		font.PreloadTextures(manager);

		Assert.Equal(GlyphAlphaMode.Premultiplied, font.AlphaMode);
		Assert.Equal(new byte[] { 128, 128, 128, 128 }, GetPagePixel(manager, 15, 15));
	}

	[Fact]
	public void AlphaMode_NonPremultiplied_PreservesStraightAlpha()
	{
		var manager = new FakeTextureManager();
		using var font = new StaticSpriteFont(FontPath(), GlyphAlphaMode.NonPremultiplied);

		font.PreloadTextures(manager);

		Assert.Equal(GlyphAlphaMode.NonPremultiplied, font.AlphaMode);
		Assert.Equal(new byte[] { 255, 255, 255, 128 }, GetPagePixel(manager, 15, 15));
	}

	[Fact]
	public void AlphaMode_NoAntialiasing_ThresholdsCoverage()
	{
		var manager = new FakeTextureManager();
		using var font = new StaticSpriteFont(FontPath(), GlyphAlphaMode.NoAntialiasing);

		font.PreloadTextures(manager);

		Assert.Equal(GlyphAlphaMode.NoAntialiasing, font.AlphaMode);
		Assert.Equal(new byte[] { 255, 255, 255, 255 }, GetPagePixel(manager, 15, 15));
	}

	[Fact]
	public void AlphaMode_RejectsInvalidValues()
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new StaticSpriteFont(FontPath(), (GlyphAlphaMode)99));
	}

	[Fact]
	public void PreloadTextures_UploadsEveryPage()
	{
		var manager = new FakeTextureManager();
		using var font = new StaticSpriteFont(FontPath());

		var created = font.PreloadTextures(manager);

		Assert.Equal(2, created);
		Assert.Equal(2, manager.Textures.Count);
		Assert.Equal(0, font.PreloadTextures(manager));
		Assert.Equal(2, manager.Textures.Count);
	}

	[Fact]
	public void DrawText_DrawsDecorationFromWhiteTexture()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var font = new StaticSpriteFont(FontPath());
		font.DrawText(
			renderer,
			"A",
			Vector2.Zero,
			Color.White,
			decoration: TextDecorationOptions.Underline(2f, new Color(255, 0, 0)));

		Assert.Equal(2, renderer.Calls.Count);

		var decoration = renderer.Calls[^1];
		Assert.Null(decoration.SourceRectangle);
		Assert.Equal(new Color(255, 0, 0), decoration.Color);
		Assert.Equal(new Vector2(0f, 17.5f), decoration.Position);
		Assert.Equal(new Vector2(10f, 2f), decoration.Scale);
		Assert.Same(manager.Textures[1], decoration.Texture);
		Assert.Equal(1, ((FakeTexture)decoration.Texture).Width);
		Assert.Equal(1, ((FakeTexture)decoration.Texture).Height);
	}

	[Fact]
	public void Strikethrough_UsesBmFontBaseline()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var font = new StaticSpriteFont(FontPath());
		font.DrawText(
			renderer,
			"A",
			Vector2.Zero,
			Color.White,
			decoration: TextDecorationOptions.Strikethrough(1f));

		Assert.Equal(8.25f, renderer.Calls[^1].Position.Y);
	}

	[Fact]
	public void DifferentTextureManagers_Throw()
	{
		using var font = new StaticSpriteFont(FontPath());
		font.DrawText(new RecordingRenderer(new FakeTextureManager()), "A", Vector2.Zero, Color.White);

		var exception = Assert.Throws<InvalidOperationException>(
			() => font.DrawText(new RecordingRenderer(new FakeTextureManager()), "B", Vector2.Zero, Color.White));

		Assert.Contains("different ITexture2DManager", exception.Message);
	}

	[Fact]
	public void Dispose_DestroysCreatedTextures()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		var font = new StaticSpriteFont(FontPath());
		font.DrawText(
			renderer,
			"A",
			Vector2.Zero,
			Color.White,
			decoration: TextDecorationOptions.Underline(1f));

		Assert.Equal(2, manager.Textures.Count);
		Assert.Equal(0, manager.DestroyTextureCalls);

		font.Dispose();

		Assert.Equal(2, manager.DestroyTextureCalls);
		Assert.All(manager.Textures, texture => Assert.True(texture.Destroyed));
		Assert.Throws<ObjectDisposedException>(() => font.MeasureString("A"));

		font.Dispose();
		Assert.Equal(2, manager.DestroyTextureCalls);
	}
}
