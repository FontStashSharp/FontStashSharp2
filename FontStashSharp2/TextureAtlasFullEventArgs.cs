namespace FontStashSharp2;

/// <summary>
/// Data for <see cref="FontSystem.TextureAtlasFull"/>.
/// </summary>
public sealed class TextureAtlasFullEventArgs : EventArgs
{
	internal TextureAtlasFullEventArgs(
		int currentTextureCount,
		int maxTextureCount,
		int pageWidth,
		int pageHeight,
		int glyphWidth,
		int glyphHeight,
		int slotWidth,
		int slotHeight)
	{
		CurrentTextureCount = currentTextureCount;
		MaxTextureCount = maxTextureCount;
		PageWidth = pageWidth;
		PageHeight = pageHeight;
		GlyphWidth = glyphWidth;
		GlyphHeight = glyphHeight;
		SlotWidth = slotWidth;
		SlotHeight = slotHeight;
	}

	/// <summary>
	/// Gets the number of atlas pages that existed before the new page is created.
	/// </summary>
	public int CurrentTextureCount { get; }

	/// <summary>
	/// Gets the configured maximum number of atlas pages.
	/// </summary>
	public int MaxTextureCount { get; }

	/// <summary>
	/// Gets the width of an atlas page, in pixels.
	/// </summary>
	public int PageWidth { get; }

	/// <summary>
	/// Gets the height of an atlas page, in pixels.
	/// </summary>
	public int PageHeight { get; }

	/// <summary>
	/// Gets the width of the glyph that did not fit, in pixels.
	/// </summary>
	public int GlyphWidth { get; }

	/// <summary>
	/// Gets the height of the glyph that did not fit, in pixels.
	/// </summary>
	public int GlyphHeight { get; }

	/// <summary>
	/// Gets the padded width reserved for the glyph, in pixels.
	/// </summary>
	public int SlotWidth { get; }

	/// <summary>
	/// Gets the padded height reserved for the glyph, in pixels.
	/// </summary>
	public int SlotHeight { get; }
}
