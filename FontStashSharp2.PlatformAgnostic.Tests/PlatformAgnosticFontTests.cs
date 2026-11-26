using System.Numerics;
using FontStashSharp2;

namespace FontStashSharp2.PlatformAgnostic.Tests;

public sealed class PlatformAgnosticFontTests
{
	[Fact]
	public void Measurement_DoesNotCreateTextures()
	{
		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});

		var font = fontSystem.GetFont(24f);
		var size = font.MeasureString("Hello");

		Assert.True(size.X > 0f);
		Assert.True(size.Y > 0f);
		Assert.Equal(0, font.GlyphCount);
		Assert.Equal(0, fontSystem.TextureCount);
	}

	[Fact]
	public void DrawText_ThroughRenderer_CreatesAtlasAndUploadsRgba()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using (var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		}))
		{
			var font = fontSystem.GetFont(24f);

			font.DrawText(renderer, "A", new Vector2(10f, 20f), Color.White);

			Assert.NotEmpty(manager.Textures);
			Assert.True(manager.SetTextureDataCalls > 0);
			Assert.NotEmpty(renderer.Calls);
			Assert.True(font.GlyphCount > 0);
			Assert.True(fontSystem.TextureCount > 0);
			Assert.Same(manager.Textures[0], fontSystem.GetTexture(0));

			var call = renderer.Calls[0];
			Assert.Same(manager.Textures[0], call.Texture);
			Assert.NotNull(call.SourceRectangle);
			Assert.True(call.SourceRectangle!.Value.Width > 0);
			Assert.True(call.SourceRectangle.Value.Height > 0);

			var opaquePixels = manager.Textures[0].Pixels
				.Chunk(4)
				.Where(pixel => pixel[3] > 0)
				.Take(16)
				.ToArray();

			Assert.NotEmpty(opaquePixels);
			Assert.All(opaquePixels, pixel =>
			{
				Assert.Equal(pixel[3], pixel[0]);
				Assert.Equal(pixel[3], pixel[1]);
				Assert.Equal(pixel[3], pixel[2]);
			});
		}

		Assert.All(manager.Textures, texture => Assert.True(texture.Destroyed));
		Assert.Equal(manager.Textures.Count, manager.DestroyTextureCalls);
	}

	[Fact]
	public void DrawText_ThroughVertexRenderer_EmitsQuads()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingVertexRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});

		var font = fontSystem.GetFont(24f);
		font.DrawText(renderer, "A", new Vector2(10f, 20f), Color.White);

		Assert.NotEmpty(renderer.Quads);
		Assert.True(manager.GetTextureSizeCalls > 0);

		var quad = renderer.Quads[0];
		Assert.Same(manager.Textures[0], quad.Texture);
		Assert.Equal(0f, quad.TopLeft.Position.Z);

		AssertVertexInBounds(quad.TopLeft);
		AssertVertexInBounds(quad.TopRight);
		AssertVertexInBounds(quad.BottomLeft);
		AssertVertexInBounds(quad.BottomRight);
	}

	[Fact]
	public void DrawText_WithDifferentTextureManagers_Throws()
	{
		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});

		var font = fontSystem.GetFont(24f);
		font.DrawText(new RecordingRenderer(new FakeTextureManager()), "A", Vector2.Zero, Color.White);

		var exception = Assert.Throws<InvalidOperationException>(
			() => font.DrawText(new RecordingRenderer(new FakeTextureManager()), "B", Vector2.Zero, Color.White));

		Assert.Contains("different ITexture2DManager", exception.Message);
	}

	[Fact]
	public void PreloadGlyphs_WithTextureManager_UploadsGlyphs()
	{
		var manager = new FakeTextureManager();

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});

		var font = fontSystem.GetFont(24f);
		var added = font.PreloadGlyphs(manager, "AB");

		Assert.True(added > 0);
		Assert.True(font.GlyphCount > 0);
		Assert.NotEmpty(manager.Textures);
		Assert.True(manager.SetTextureDataCalls > 0);
	}

	[Fact]
	public void AlphaMode_NonPremultiplied_StoresWhiteRgbAndCoverageAlpha()
	{
		var texture = DrawGlyph(GlyphAlphaMode.NonPremultiplied);
		var pixels = texture.Pixels;
		var visiblePixels = 0;

		for (var i = 0; i < pixels.Length; i += 4)
		{
			var alpha = pixels[i + 3];
			if (alpha == 0)
			{
				continue;
			}

			visiblePixels++;
			Assert.Equal(255, pixels[i]);
			Assert.Equal(255, pixels[i + 1]);
			Assert.Equal(255, pixels[i + 2]);
		}

		Assert.True(visiblePixels > 0);
	}

	[Fact]
	public void AlphaMode_NoAntialiasing_StoresOnlyTransparentOrOpaquePixels()
	{
		var texture = DrawGlyph(GlyphAlphaMode.NoAntialiasing);
		var pixels = texture.Pixels;
		var opaquePixels = 0;

		for (var i = 0; i < pixels.Length; i += 4)
		{
			var red = pixels[i];
			var green = pixels[i + 1];
			var blue = pixels[i + 2];
			var alpha = pixels[i + 3];

			var transparent = red == 0 && green == 0 && blue == 0 && alpha == 0;
			var opaque = red == 255 && green == 255 && blue == 255 && alpha == 255;
			Assert.True(transparent || opaque, $"Unexpected RGBA pixel: {red},{green},{blue},{alpha}.");

			if (opaque)
			{
				opaquePixels++;
			}
		}

		Assert.True(opaquePixels > 0);
	}

	[Fact]
	public void ShadowEffect_DrawsOffsetShadowBeforeText()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		var effectColor = new Color(255, 0, 0);

		font.DrawText(
			renderer,
			"A",
			new Vector2(20f, 30f),
			Color.White,
			effect: TextEffectOptions.Shadow(2, effectColor));

		Assert.Equal(2, renderer.Calls.Count);
		var shadow = renderer.Calls[0];
		var main = renderer.Calls[1];

		Assert.Equal(main.Position.X + 2f, shadow.Position.X);
		Assert.Equal(main.Position.Y + 2f, shadow.Position.Y);
		Assert.Equal(effectColor, shadow.Color);
		Assert.Equal(Color.White, main.Color);
		Assert.Equal(shadow.SourceRectangle, main.SourceRectangle);
		Assert.Same(shadow.Texture, main.Texture);
	}

	[Fact]
	public void EffectColor_DefaultsToWhite()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		font.DrawText(
			renderer,
			"A",
			Vector2.Zero,
			Color.White,
			effect: TextEffectOptions.Shadow(2));

		Assert.Equal(2, renderer.Calls.Count);
		Assert.Equal(Color.White, renderer.Calls[0].Color);
	}

	[Fact]
	public void StrokeEffect_DrawsEightOffsetsBeforeText()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		var effectColor = new Color(255, 0, 0);

		font.DrawText(
			renderer,
			"A",
			new Vector2(20f, 30f),
			Color.White,
			effect: TextEffectOptions.Stroke(1, effectColor));

		Assert.Equal(9, renderer.Calls.Count);
		var main = renderer.Calls[^1];

		for (var i = 0; i < renderer.Calls.Count - 1; i++)
		{
			var effect = renderer.Calls[i];
			Assert.Equal(effectColor, effect.Color);
			Assert.InRange(Math.Abs(effect.Position.X - main.Position.X), 0f, 1f);
			Assert.InRange(Math.Abs(effect.Position.Y - main.Position.Y), 0f, 1f);
			Assert.NotEqual(main.Position, effect.Position);
		}
	}

	[Fact]
	public void EffectAmountZero_DisablesEffect()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		font.DrawText(
			renderer,
			"A",
			Vector2.Zero,
			Color.White,
			effect: TextEffectOptions.Shadow(0));

		Assert.Single(renderer.Calls);
	}

	[Fact]
	public void Effect_RejectsInvalidValues()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		Assert.Throws<ArgumentOutOfRangeException>(() => TextEffectOptions.Shadow(-1));
		Assert.Throws<ArgumentOutOfRangeException>(() => new TextEffectOptions((TextEffect)99));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => font.DrawText(renderer, "A", Vector2.Zero, Color.White, effect: default(TextEffectOptions)));
	}

	[Fact]
	public void CharacterSpacing_OffsetsSubsequentGlyphs()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		font.DrawText(
			renderer,
			"AB",
			new Vector2(10f, 20f),
			Color.White,
			0f,
			Vector2.Zero,
			new Vector2(3f, 1f),
			SpriteEffects.None,
			0f);
		var baseline = renderer.Calls.ToArray();
		renderer.Calls.Clear();

		font.DrawText(
			renderer,
			"AB",
			new Vector2(10f, 20f),
			Color.White,
			0f,
			Vector2.Zero,
			new Vector2(3f, 1f),
			SpriteEffects.None,
			0f,
			characterSpacing: 5f);

		Assert.Equal(baseline.Length, renderer.Calls.Count);
		Assert.Equal(baseline[0].Position, renderer.Calls[0].Position);
		Assert.Equal(baseline[1].Position.X + 15f, renderer.Calls[1].Position.X);
		Assert.Equal(baseline[1].Position.Y, renderer.Calls[1].Position.Y);
	}

	[Fact]
	public void LineSpacing_OffsetsSecondLine()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		font.DrawText(
			renderer,
			"A\nB",
			new Vector2(10f, 20f),
			Color.White,
			0f,
			Vector2.Zero,
			new Vector2(1f, 3f),
			SpriteEffects.None,
			0f);
		var baseline = renderer.Calls.ToArray();
		renderer.Calls.Clear();

		font.DrawText(
			renderer,
			"A\nB",
			new Vector2(10f, 20f),
			Color.White,
			0f,
			Vector2.Zero,
			new Vector2(1f, 3f),
			SpriteEffects.None,
			0f,
			lineSpacing: 7f);

		Assert.Equal(baseline.Length, renderer.Calls.Count);
		Assert.Equal(baseline[0].Position, renderer.Calls[0].Position);
		Assert.Equal(baseline[1].Position.X, renderer.Calls[1].Position.X);
		Assert.Equal(baseline[1].Position.Y + 21f, renderer.Calls[1].Position.Y);
	}

	[Fact]
	public void Underline_DrawsLineBelowBaselineAfterText()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		font.DrawText(
			renderer,
			"AB",
			new Vector2(10f, 20f),
			Color.White,
			decoration: TextDecorationOptions.Underline(2f));

		Assert.Equal(3, renderer.Calls.Count);

		var decoration = renderer.Calls[^1];
		Assert.Null(decoration.SourceRectangle);
		Assert.Equal(1, ((FakeTexture)decoration.Texture).Width);
		Assert.Equal(1, ((FakeTexture)decoration.Texture).Height);
		Assert.Equal(Color.White, decoration.Color);
		Assert.Equal(10f, decoration.Position.X);
		Assert.Equal(
			20f + font.Ascent - (font.Descent * 0.5f),
			decoration.Position.Y,
			3);
		Assert.Equal(font.MeasureString("AB").X, decoration.Scale.X, 3);
		Assert.Equal(2f, decoration.Scale.Y);
	}

	[Fact]
	public void Strikethrough_DrawsLineThroughText()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		font.DrawText(
			renderer,
			"AB",
			new Vector2(10f, 20f),
			Color.White,
			decoration: TextDecorationOptions.Strikethrough(3f));

		Assert.Equal(3, renderer.Calls.Count);

		var decoration = renderer.Calls[^1];
		Assert.Null(decoration.SourceRectangle);
		Assert.Equal(
			20f + font.Ascent - (font.Ascent * 0.45f),
			decoration.Position.Y,
			3);
		Assert.Equal(3f, decoration.Scale.Y);
	}

	[Fact]
	public void DecorationWidthAndOffset_ScaleWithDrawScale()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		font.DrawText(
			renderer,
			"AB",
			new Vector2(10f, 20f),
			Color.White,
			0f,
			Vector2.Zero,
			new Vector2(2f, 3f),
			SpriteEffects.None,
			0f,
			decoration: TextDecorationOptions.Underline(1.5f));

		Assert.Equal(3, renderer.Calls.Count);

		var decoration = renderer.Calls[^1];
		Assert.Equal(10f, decoration.Position.X);
		Assert.Equal(
			20f + ((font.Ascent - (font.Descent * 0.5f)) * 3f),
			decoration.Position.Y,
			3);
		Assert.Equal(font.MeasureString("AB").X * 2f, decoration.Scale.X, 3);
		Assert.Equal(4.5f, decoration.Scale.Y);
	}

	[Fact]
	public void DecorationColor_OverridesOrInheritsMainTextColor()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		font.DrawText(
			renderer,
			"AB",
			Vector2.Zero,
			Color.White,
			decoration: TextDecorationOptions.Underline(2f, new Color(255, 0, 0)));

		Assert.Equal(3, renderer.Calls.Count);
		Assert.Equal(Color.White, renderer.Calls[0].Color);
		Assert.Equal(new Color(255, 0, 0), renderer.Calls[^1].Color);

		renderer.Calls.Clear();

		var mainColor = new Color(0, 128, 255);
		font.DrawText(
			renderer,
			"AB",
			Vector2.Zero,
			mainColor,
			decoration: TextDecorationOptions.Strikethrough(2f));

		Assert.Equal(3, renderer.Calls.Count);
		Assert.Equal(mainColor, renderer.Calls[0].Color);
		Assert.Equal(mainColor, renderer.Calls[^1].Color);
	}

	[Fact]
	public void Decoration_DrawsOnceAfterEffectPassesUsingMainColor()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);
		var effectColor = new Color(255, 0, 0);

		font.DrawText(
			renderer,
			"AB",
			new Vector2(10f, 20f),
			Color.White,
			effect: TextEffectOptions.Shadow(2, effectColor),
			decoration: TextDecorationOptions.Underline(1f));

		Assert.Equal(5, renderer.Calls.Count);
		Assert.Equal(effectColor, renderer.Calls[0].Color);
		Assert.Equal(effectColor, renderer.Calls[1].Color);

		var decoration = renderer.Calls[^1];
		Assert.Null(decoration.SourceRectangle);
		Assert.Equal(Color.White, decoration.Color);
	}

	[Fact]
	public void DecorationNone_DrawsNoDecoration()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		font.DrawText(
			renderer,
			"AB",
			Vector2.Zero,
			Color.White,
			decoration: new TextDecorationOptions(TextDecoration.None, 5f));

		Assert.Equal(2, renderer.Calls.Count);
		Assert.All(renderer.Calls, call => Assert.NotNull(call.SourceRectangle));
	}

	[Fact]
	public void Decoration_RejectsInvalidValues()
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => TextDecorationOptions.Underline(-1f));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => TextDecorationOptions.Strikethrough(float.NaN));
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new TextDecorationOptions((TextDecoration)99));

		Assert.Equal(1f, TextDecorationOptions.Underline().DecorationWidth);
		Assert.Equal(1f, TextDecorationOptions.Strikethrough().DecorationWidth);
	}

	[Fact]
	public void ClearTextures_DisposesDecorationWhiteTexture()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			TextureWidth = 128,
			TextureHeight = 128,
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		font.DrawText(
			renderer,
			"AB",
			Vector2.Zero,
			Color.White,
			decoration: TextDecorationOptions.Underline(1f));

		var textureCount = manager.Textures.Count;
		Assert.Equal(fontSystem.TextureCount + 1, textureCount);

		fontSystem.ClearTextures();

		Assert.Equal(textureCount, manager.DestroyTextureCalls);
		Assert.All(manager.Textures, texture => Assert.True(texture.Destroyed));
	}

	[Fact]
	public void Decoration_ThroughVertexRenderer_EmitsQuadAfterText()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingVertexRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		font.DrawText(
			renderer,
			"AB",
			new Vector2(10f, 20f),
			Color.White,
			decoration: TextDecorationOptions.Underline(2f));

		Assert.Equal(3, renderer.Quads.Count);

		var decoration = renderer.Quads[^1];
		Assert.Equal(1, ((FakeTexture)decoration.Texture).Width);
		Assert.Equal(0f, decoration.TopLeft.Position.Z);
		Assert.Equal(
			font.MeasureString("AB").X,
			decoration.TopRight.Position.X - decoration.TopLeft.Position.X,
			3);
		Assert.Equal(
			2f,
			decoration.BottomLeft.Position.Y - decoration.TopLeft.Position.Y,
			3);
	}

	[Fact]
	public void TextureAtlasFull_FiresOnlyWhenANewPageIsCreated()
	{
		var manager = new FakeTextureManager();
		var events = new List<TextureAtlasFullEventArgs>();

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			TextureWidth = 48,
			TextureHeight = 48,
			MaxTextureCount = 32,
			GlyphPadding = 1,
			SupersamplingFactor = 1f,
			UseKerning = false,
		});
		fontSystem.TextureAtlasFull += (_, e) => events.Add(e);

		var font = fontSystem.GetFont(24f);
		font.PreloadGlyphs(manager, "Hi");

		Assert.Empty(events);

		font.PreloadGlyphs(manager, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");

		Assert.NotEmpty(events);
		Assert.True(fontSystem.TextureCount > 1);
		Assert.All(events, e =>
		{
			Assert.InRange(e.CurrentTextureCount, 1, int.MaxValue);
			Assert.Equal(32, e.MaxTextureCount);
			Assert.Equal(48, e.PageWidth);
			Assert.Equal(48, e.PageHeight);
			Assert.True(e.GlyphWidth > 0);
			Assert.True(e.GlyphHeight > 0);
			Assert.True(e.SlotWidth >= e.GlyphWidth);
			Assert.True(e.SlotHeight >= e.GlyphHeight);
		});
	}

	[Fact]
	public void ClearTextures_RemovesPagesAndInvalidatesGlyphCache()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			TextureWidth = 128,
			TextureHeight = 128,
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);

		font.DrawText(renderer, "Hello", Vector2.Zero, Color.White);

		var pageCount = fontSystem.TextureCount;
		Assert.True(pageCount > 0);
		Assert.True(font.GlyphCount > 0);
		Assert.Equal(0, manager.DestroyTextureCalls);

		fontSystem.ClearTextures();

		Assert.Equal(0, fontSystem.TextureCount);
		Assert.Equal(0, font.GlyphCount);
		Assert.Equal(pageCount, manager.DestroyTextureCalls);
		Assert.All(manager.Textures, texture => Assert.True(texture.Destroyed));
		Assert.Throws<InvalidOperationException>(() => fontSystem.GetTexture(0));

		font.DrawText(renderer, "Hello", Vector2.Zero, Color.White);

		Assert.True(fontSystem.TextureCount > 0);
		Assert.True(font.GlyphCount > 0);
		Assert.True(manager.CreateTextureCalls > pageCount);
	}

	[Fact]
	public void ClearTextures_LazilyInvalidatesFontsRemovedFromInternalCache()
	{
		var manager = new FakeTextureManager();
		var renderer = new RecordingRenderer(manager);

		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			TextureWidth = 128,
			TextureHeight = 128,
			SupersamplingFactor = 1f,
		});
		var font = fontSystem.GetFont(24f);
		font.DrawText(renderer, "Hello", Vector2.Zero, Color.White);
		Assert.True(font.GlyphCount > 0);

		// AddFont clears FontSystem._fonts; this font is now only held by the caller.
		fontSystem.AddFont(TestFont.Data);
		fontSystem.ClearTextures();

		Assert.Equal(0, font.GlyphCount);

		font.DrawText(renderer, "Hello", Vector2.Zero, Color.White);
		Assert.True(font.GlyphCount > 0);
	}

	private static FakeTexture DrawGlyph(GlyphAlphaMode alphaMode)
	{
		var manager = new FakeTextureManager();
		using var fontSystem = TestFont.CreateFontSystem(new FontSystemSettings
		{
			SupersamplingFactor = 1f,
			AlphaMode = alphaMode,
		});
		var font = fontSystem.GetFont(24f);
		font.DrawText(new RecordingRenderer(manager), "A", new Vector2(10f, 10f), Color.White);

		Assert.NotEmpty(manager.Textures);
		return manager.Textures[0];
	}

	private static void AssertVertexInBounds(VertexPositionColorTexture vertex)
	{
		Assert.InRange(vertex.TextureCoordinate.X, 0f, 1f);
		Assert.InRange(vertex.TextureCoordinate.Y, 0f, 1f);
	}
}
