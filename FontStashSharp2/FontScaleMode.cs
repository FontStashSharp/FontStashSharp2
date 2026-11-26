namespace FontStashSharp2;

/// <summary>
/// Selects how font units are converted to pixels.
/// </summary>
public enum FontScaleMode
{
	/// <summary>
	/// Maps the font's ascent-to-descent pixel height to <see cref="DynamicSpriteFont.FontSize"/>. This
	/// is the default.
	/// </summary>
	ScaleForPixelHeight = 0,

	/// <summary>
	/// Maps the font's em square to <see cref="DynamicSpriteFont.FontSize"/>.
	/// </summary>
	ScaleForMappingEmToPixels = 1,
}
