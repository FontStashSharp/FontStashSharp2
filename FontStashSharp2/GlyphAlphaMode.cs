namespace FontStashSharp2;

/// <summary>
/// Controls how rasterized glyph coverage is converted into RGBA atlas pixels.
/// </summary>
public enum GlyphAlphaMode
{
	/// <summary>
	/// Stores premultiplied RGBA. A coverage value <c>a</c> becomes <c>(a, a, a, a)</c>. Use with a
	/// premultiplied alpha blend state.
	/// </summary>
	Premultiplied = 0,

	/// <summary>
	/// Stores straight alpha. A coverage value <c>a</c> becomes <c>(255, 255, 255, a)</c>. Use with a
	/// standard source-alpha blend state.
	/// </summary>
	NonPremultiplied = 1,

	/// <summary>
	/// Disables antialiasing in the atlas. Non-zero coverage becomes <c>(255, 255, 255, 255)</c>;
	/// zero coverage stays <c>(0, 0, 0, 0)</c>.
	/// </summary>
	NoAntialiasing = 2,
}
