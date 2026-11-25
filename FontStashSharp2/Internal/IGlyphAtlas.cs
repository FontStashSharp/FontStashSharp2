using Microsoft.Xna.Framework;

namespace FontStashSharp2.Internal;

/// <summary>
/// A sink for rasterized glyph coverage bitmaps. Implemented by <see cref="TextureAtlas"/>; replaced
/// with a recording implementation in tests so glyph caching can be verified without a graphics device.
/// </summary>
internal interface IGlyphAtlas
{
	int TextureCount { get; }

	/// <summary>
	/// Uploads an 8-bit coverage bitmap and returns its location in the atlas.
	/// </summary>
	GlyphPlacement Add(ReadOnlySpan<byte> alphaBitmap, int width, int height);
}
