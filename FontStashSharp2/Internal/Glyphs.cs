using Microsoft.Xna.Framework;

namespace FontStashSharp2.Internal;

internal readonly struct GlyphMetrics
{
	public GlyphMetrics(int codepoint, int sourceIndex, int glyphIndex, float advance)
	{
		Codepoint = codepoint;
		SourceIndex = sourceIndex;
		GlyphIndex = glyphIndex;
		Advance = advance;
	}

	public int Codepoint { get; }

	public int SourceIndex { get; }

	public int GlyphIndex { get; }

	public float Advance { get; }
}

internal readonly struct RenderedGlyph
{
	public RenderedGlyph(
		GlyphMetrics metrics,
		int textureIndex,
		Rectangle sourceRectangle,
		float offsetX,
		float offsetY)
	{
		Metrics = metrics;
		TextureIndex = textureIndex;
		SourceRectangle = sourceRectangle;
		OffsetX = offsetX;
		OffsetY = offsetY;
	}

	public GlyphMetrics Metrics { get; }

	/// <summary>Atlas page index, or -1 when the glyph has no pixels (for example a space).</summary>
	public int TextureIndex { get; }

	public Rectangle SourceRectangle { get; }

	/// <summary>Horizontal bitmap offset from the pen position, in logical pixels.</summary>
	public float OffsetX { get; }

	/// <summary>Vertical bitmap offset from the baseline, in logical pixels (negative is above the baseline).</summary>
	public float OffsetY { get; }

	public bool HasTexture => TextureIndex >= 0;
}

internal readonly record struct GlyphPlacement(int TextureIndex, Rectangle Bounds);
