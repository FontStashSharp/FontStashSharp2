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
