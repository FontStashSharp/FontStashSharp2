using System.Numerics;
using FontStashSharp2;

namespace FontStashSharp2.PlatformAgnostic.Tests;

internal static class TestFont
{
	private static readonly Lazy<byte[]> LazyData = new(
		() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fonts", "ABeeZee-Regular.ttf")),
		LazyThreadSafetyMode.ExecutionAndPublication);

	public static byte[] Data => LazyData.Value;

	public static FontSystem CreateFontSystem(FontSystemSettings? settings = null)
	{
		var fontSystem = new FontSystem(settings);
		fontSystem.AddFont(Data);
		return fontSystem;
	}
}

internal sealed class FakeTexture
{
	public FakeTexture(int width, int height)
	{
		Width = width;
		Height = height;
		Pixels = new byte[checked(width * height * 4)];
	}

	public int Width { get; }

	public int Height { get; }

	public byte[] Pixels { get; }

	public bool Destroyed { get; set; }
}

internal sealed class FakeTextureManager : ITexture2DManager
{
	public List<FakeTexture> Textures { get; } = new();

	public int CreateTextureCalls { get; private set; }

	public int SetTextureDataCalls { get; private set; }

	public int GetTextureSizeCalls { get; private set; }

	public int DestroyTextureCalls { get; private set; }

	public object CreateTexture(int width, int height)
	{
		CreateTextureCalls++;
		var texture = new FakeTexture(width, height);
		Textures.Add(texture);
		return texture;
	}

	public Point GetTextureSize(object texture)
	{
		GetTextureSizeCalls++;
		var fake = (FakeTexture)texture;
		return new Point(fake.Width, fake.Height);
	}

	public void SetTextureData(object texture, Rectangle bounds, ReadOnlySpan<byte> data)
	{
		SetTextureDataCalls++;
		var fake = (FakeTexture)texture;

		Assert.True(bounds.X >= 0);
		Assert.True(bounds.Y >= 0);
		Assert.True(bounds.Right <= fake.Width);
		Assert.True(bounds.Bottom <= fake.Height);

		var expectedLength = checked(bounds.Width * bounds.Height * 4);
		Assert.True(data.Length >= expectedLength);

		for (var row = 0; row < bounds.Height; row++)
		{
			var source = data.Slice(row * bounds.Width * 4, bounds.Width * 4);
			var destinationOffset = ((bounds.Y + row) * fake.Width + bounds.X) * 4;
			source.CopyTo(fake.Pixels.AsSpan(destinationOffset, bounds.Width * 4));
		}
	}

	public void DestroyTexture(object texture)
	{
		DestroyTextureCalls++;
		((FakeTexture)texture).Destroyed = true;
	}
}

internal sealed record DrawCall(
	object Texture,
	Vector2 Position,
	Rectangle? SourceRectangle,
	Color Color,
	float Rotation,
	Vector2 Scale,
	SpriteEffects Effects,
	float LayerDepth);

internal sealed class RecordingRenderer : IFontStashRenderer
{
	public RecordingRenderer(ITexture2DManager textureManager)
	{
		TextureManager = textureManager;
	}

	public ITexture2DManager TextureManager { get; }

	public List<DrawCall> Calls { get; } = new();

	public void DrawText(
		object texture,
		Vector2 position,
		Rectangle? sourceRectangle,
		Color color,
		float rotation,
		Vector2 scale,
		SpriteEffects effects,
		float layerDepth)
	{
		Calls.Add(new DrawCall(texture, position, sourceRectangle, color, rotation, scale, effects, layerDepth));
	}
}

internal sealed record Quad(
	object Texture,
	VertexPositionColorTexture TopLeft,
	VertexPositionColorTexture TopRight,
	VertexPositionColorTexture BottomLeft,
	VertexPositionColorTexture BottomRight);

internal sealed class RecordingVertexRenderer : IFontStashRenderer2
{
	public RecordingVertexRenderer(ITexture2DManager textureManager)
	{
		TextureManager = textureManager;
	}

	public ITexture2DManager TextureManager { get; }

	public List<Quad> Quads { get; } = new();

	public void DrawQuad(
		object texture,
		ref VertexPositionColorTexture topLeft,
		ref VertexPositionColorTexture topRight,
		ref VertexPositionColorTexture bottomLeft,
		ref VertexPositionColorTexture bottomRight)
	{
		Quads.Add(new Quad(texture, topLeft, topRight, bottomLeft, bottomRight));
	}
}
