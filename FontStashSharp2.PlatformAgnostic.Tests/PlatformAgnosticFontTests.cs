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

	private static void AssertVertexInBounds(VertexPositionColorTexture vertex)
	{
		Assert.InRange(vertex.TextureCoordinate.X, 0f, 1f);
		Assert.InRange(vertex.TextureCoordinate.Y, 0f, 1f);
	}
}
