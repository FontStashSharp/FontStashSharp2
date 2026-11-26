namespace FontStashSharp2;

/// <summary>
/// A line decoration applied when drawing text.
/// </summary>
public enum TextDecoration
{
	/// <summary>No decoration.</summary>
	None = 0,

	/// <summary>Draw a line below each text line.</summary>
	Underline = 1,

	/// <summary>Draw a line through each text line.</summary>
	Strikethrough = 2,
}
