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
	/// <param name="device">
	/// Device or texture manager used when a new atlas page must be created. Production always supplies
	/// one; the recording test atlas ignores it.
	/// </param>
	/// <param name="alphaBitmap">8-bit coverage values, row-major.</param>
	/// <param name="width">Bitmap width in pixels.</param>
	/// <param name="height">Bitmap height in pixels.</param>
	GlyphPlacement Add(
		AtlasDevice? device,
		ReadOnlySpan<byte> alphaBitmap,
		int width,
		int height);

	/// <summary>
	/// Ensures that the atlas can be used with the supplied device or texture manager.
	/// </summary>
	/// <param name="device">The device or texture manager that will be used for drawing/uploading.</param>
	void ValidateDevice(AtlasDevice device);
}
